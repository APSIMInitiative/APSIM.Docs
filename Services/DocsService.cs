using System.Text.RegularExpressions;
using Markdig;

namespace APSIM.Docs.Services;

public sealed class DocsOptions
{
    public string MarkdownRoot { get; set; } = "apsim-docs-md";

    public string HtmlRoot { get; set; } = "apsim-docs-html";
}

public sealed record DocPage(
    string SlugPath,
    string Title,
    string SourcePath,
    bool IsIndexPage,
    string? Section,
    string? Summary);

public sealed record DocsNode(
    string Name,
    string SlugPath,
    DocPage? IndexPage,
    IReadOnlyList<DocsNode> Directories,
    IReadOnlyList<DocPage> Pages);

public interface IDocsService
{
    DocsNode GetTree();
    IReadOnlyList<DocsNode> GetTopLevelSections();
    DocsNode? TryGetDirectory(string? slugPath);
    DocPage? TryGetPageBySlug(string? slugPath);
    DocPage? TryResolveLegacyFilename(string? filename);
    string RenderPageHtml(DocPage page);
}

public sealed class DocsService : IDocsService
{
    private static readonly Regex FrontMatterRegex = new(
        @"\A---\s*\r?\n(?<frontmatter>.*?)(\r?\n)---\s*\r?\n",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TitleRegex = new(
        "^\\s*title\\s*:\\s*[\"']?(?<title>.*?)[\"']?\\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex HeadingRegex = new(
        @"^#\s+(?<heading>.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex LinkRegex = new(
        "href=\"(?<href>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly MarkdownPipeline markdownPipeline;
    private readonly string markdownRoot;
    private readonly object cacheLock = new();

    private DocsCache? cache;

    public DocsService(IConfiguration configuration)
    {
        var options = new DocsOptions();
        configuration.GetSection("Docs").Bind(options);

        markdownRoot = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), options.MarkdownRoot));

        markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public DocsNode GetTree() => GetOrBuildCache().Root;

    public IReadOnlyList<DocsNode> GetTopLevelSections() => GetOrBuildCache().Root.Directories;

    public DocsNode? TryGetDirectory(string? slugPath)
    {
        var normalizedPath = NormalizeSlugPath(slugPath);
        return GetOrBuildCache().DirectoriesBySlug.GetValueOrDefault(normalizedPath);
    }

    public DocPage? TryGetPageBySlug(string? slugPath)
    {
        var normalizedPath = NormalizeSlugPath(slugPath);
        return GetOrBuildCache().PagesBySlug.GetValueOrDefault(normalizedPath);
    }

    public DocPage? TryResolveLegacyFilename(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        var key = SlugifySegment(filename);
        if (!GetOrBuildCache().PagesByLegacyStem.TryGetValue(key, out var matches) || matches.Count != 1)
        {
            return null;
        }

        return matches[0];
    }

    public string RenderPageHtml(DocPage page)
    {
        if (!File.Exists(page.SourcePath))
        {
            return "<p>The selected document is unavailable.</p>";
        }

        var rawMarkdown = File.ReadAllText(page.SourcePath);
        var markdownWithoutFrontMatter = StripFrontMatter(rawMarkdown, out _);
        var html = Markdown.ToHtml(markdownWithoutFrontMatter, markdownPipeline);
        return RewriteRelativeDocLinks(html, page.SlugPath);
    }

    private DocsCache GetOrBuildCache()
    {
        lock (cacheLock)
        {
            if (cache is null)
            {
                cache = BuildCache();
            }

            return cache;
        }
    }

    private DocsCache BuildCache()
    {
        var rootBuilder = new NodeBuilder("Docs", string.Empty);
        var pagesBySlug = new Dictionary<string, DocPage>(StringComparer.OrdinalIgnoreCase);
        var directoriesBySlug = new Dictionary<string, DocsNode>(StringComparer.OrdinalIgnoreCase);
        var pagesByLegacyStem = new Dictionary<string, List<DocPage>>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(markdownRoot))
        {
            PopulateNode(rootBuilder, markdownRoot, string.Empty, pagesBySlug, pagesByLegacyStem);
        }

        var root = rootBuilder.ToNode();
        IndexDirectories(root, directoriesBySlug);

        return new DocsCache(root, pagesBySlug, directoriesBySlug, pagesByLegacyStem);
    }

    private void PopulateNode(
        NodeBuilder node,
        string directoryPath,
        string parentSlug,
        Dictionary<string, DocPage> pagesBySlug,
        Dictionary<string, List<DocPage>> pagesByLegacyStem)
    {
        var directories = Directory.EnumerateDirectories(directoryPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var childDirectory in directories)
        {
            var rawName = Path.GetFileName(childDirectory);
            var childSlugSegment = SlugifySegment(rawName);
            var childSlug = JoinSlug(parentSlug, childSlugSegment);

            var childNode = new NodeBuilder(ToDisplayName(rawName), childSlug);
            PopulateNode(childNode, childDirectory, childSlug, pagesBySlug, pagesByLegacyStem);
            node.Directories.Add(childNode);
        }

        var markdownFiles = Directory.EnumerateFiles(directoryPath, "*.md")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var markdownFile in markdownFiles)
        {
            var baseName = Path.GetFileNameWithoutExtension(markdownFile);
            var isIndex = baseName.Equals("_index", StringComparison.OrdinalIgnoreCase);
            if (!isIndex && baseName.StartsWith('_'))
            {
                continue;
            }

            var slug = isIndex
                ? parentSlug
                : JoinSlug(parentSlug, SlugifySegment(baseName));

            var content = File.ReadAllText(markdownFile);
            var contentWithoutFrontMatter = StripFrontMatter(content, out var frontMatter);
            var title = ExtractTitle(frontMatter, contentWithoutFrontMatter)
                ?? (isIndex ? node.Name : ToDisplayName(baseName));

            var section = parentSlug.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var summary = ExtractSummary(contentWithoutFrontMatter);

            var page = new DocPage(slug, title, markdownFile, isIndex, section, summary);
            pagesBySlug[slug] = page;

            if (isIndex)
            {
                node.IndexPage = page;
            }
            else
            {
                node.Pages.Add(page);
            }

            var legacyKey = SlugifySegment(baseName);
            if (!pagesByLegacyStem.TryGetValue(legacyKey, out var matches))
            {
                matches = [];
                pagesByLegacyStem[legacyKey] = matches;
            }

            matches.Add(page);
        }
    }

    private static string? ExtractTitle(string? frontMatter, string markdownWithoutFrontMatter)
    {
        if (!string.IsNullOrWhiteSpace(frontMatter))
        {
            var frontMatterMatch = TitleRegex.Match(frontMatter);
            if (frontMatterMatch.Success)
            {
                return frontMatterMatch.Groups["title"].Value.Trim();
            }
        }

        var headingMatch = HeadingRegex.Match(markdownWithoutFrontMatter);
        return headingMatch.Success
            ? headingMatch.Groups["heading"].Value.Trim()
            : null;
    }

    private static string? ExtractSummary(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            if (trimmedLine.StartsWith('#'))
            {
                continue;
            }

            return trimmedLine.Length > 140
                ? trimmedLine[..140] + "..."
                : trimmedLine;
        }

        return null;
    }

    private static string StripFrontMatter(string markdown, out string? frontMatter)
    {
        var frontMatterMatch = FrontMatterRegex.Match(markdown);
        if (!frontMatterMatch.Success)
        {
            frontMatter = null;
            return markdown;
        }

        frontMatter = frontMatterMatch.Groups["frontmatter"].Value;
        return markdown[frontMatterMatch.Length..];
    }

    private static void IndexDirectories(DocsNode current, Dictionary<string, DocsNode> directoryBySlug)
    {
        directoryBySlug[current.SlugPath] = current;
        foreach (var child in current.Directories)
        {
            IndexDirectories(child, directoryBySlug);
        }
    }

    private static string RewriteRelativeDocLinks(string html, string currentSlug)
    {
        var currentDirectory = Path.GetDirectoryName(currentSlug.Replace('/', Path.DirectorySeparatorChar))
            ?.Replace(Path.DirectorySeparatorChar, '/')
            ?? string.Empty;

        return LinkRegex.Replace(html, match =>
        {
            var href = match.Groups["href"].Value;
            if (href.StartsWith("#", StringComparison.Ordinal)
                || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("/", StringComparison.Ordinal))
            {
                return match.Value;
            }

            var hashIndex = href.IndexOf('#');
            var pathPart = hashIndex >= 0 ? href[..hashIndex] : href;
            var anchorPart = hashIndex >= 0 ? href[hashIndex..] : string.Empty;

            if (!pathPart.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            var withoutExtension = pathPart[..^3];
            var normalizedRelativePath = withoutExtension.Replace('\\', '/');
            var resolvedSlug = normalizedRelativePath.StartsWith('/')
                ? NormalizeSlugPath(normalizedRelativePath)
                : NormalizeSlugPath(JoinSlug(currentDirectory, normalizedRelativePath));

            return $"href=\"/docs/{resolvedSlug}{anchorPart}\"";
        });
    }

    private static string NormalizeSlugPath(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var segments = input
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(SlugifySegment)
            .Where(segment => !string.IsNullOrWhiteSpace(segment));

        return string.Join('/', segments);
    }

    private static string JoinSlug(string parentSlug, string childSlug)
    {
        if (string.IsNullOrEmpty(parentSlug))
        {
            return NormalizeSlugPath(childSlug);
        }

        if (string.IsNullOrEmpty(childSlug))
        {
            return NormalizeSlugPath(parentSlug);
        }

        return NormalizeSlugPath($"{parentSlug}/{childSlug}");
    }

    private static string SlugifySegment(string input)
    {
        var trimmedInput = input.Trim().Replace('\\', '-').Replace('/', '-');
        var lowercase = trimmedInput.ToLowerInvariant();
        var alphanumeric = Regex.Replace(lowercase, "[^a-z0-9_-]+", "-");
        var normalizedDashes = Regex.Replace(alphanumeric.Replace('_', '-'), "-+", "-");
        return normalizedDashes.Trim('-');
    }

    private static string ToDisplayName(string input)
    {
        var normalized = input.Replace('_', ' ').Replace('-', ' ').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return input;
        }

        return Regex.Replace(normalized, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled);
    }

    private sealed class DocsCache(
        DocsNode root,
        Dictionary<string, DocPage> pagesBySlug,
        Dictionary<string, DocsNode> directoriesBySlug,
        Dictionary<string, List<DocPage>> pagesByLegacyStem)
    {
        public DocsNode Root { get; } = root;
        public Dictionary<string, DocPage> PagesBySlug { get; } = pagesBySlug;
        public Dictionary<string, DocsNode> DirectoriesBySlug { get; } = directoriesBySlug;
        public Dictionary<string, List<DocPage>> PagesByLegacyStem { get; } = pagesByLegacyStem;
    }

    private sealed class NodeBuilder(string name, string slugPath)
    {
        public string Name { get; } = name;
        public string SlugPath { get; } = slugPath;
        public DocPage? IndexPage { get; set; }
        public List<NodeBuilder> Directories { get; } = [];
        public List<DocPage> Pages { get; } = [];

        public DocsNode ToNode()
        {
            var childNodes = Directories.Select(child => child.ToNode()).ToList();
            return new DocsNode(Name, SlugPath, IndexPage, childNodes, Pages.ToList());
        }
    }
}