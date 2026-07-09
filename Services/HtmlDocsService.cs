using System.Text.RegularExpressions;

namespace APSIM.Docs.Services;

public sealed record HtmlDocPage(
    string SectionSlug,
    string SectionName,
    string FileStem,
    string DisplayName,
    string SourcePath);

public sealed record HtmlDocSection(
    string Name,
    string Slug,
    IReadOnlyList<HtmlDocPage> Pages);

public interface IHtmlDocsService
{
    IReadOnlyList<HtmlDocSection> GetSections();
    HtmlDocSection? TryGetSection(string? sectionSlug);
    HtmlDocPage? TryGetPage(string? sectionSlug, string? fileStem);
    string RenderPageHtml(HtmlDocPage page, string currentUri);
}

public sealed class HtmlDocsService : IHtmlDocsService
{
    private static readonly Regex LinkRegex = new(
        "href=\"(?<href>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string htmlRoot;
    private readonly object cacheLock = new();

    private HtmlDocsCache? cache;

    public HtmlDocsService(IConfiguration configuration)
    {
        var options = new DocsOptions();
        configuration.GetSection("Docs").Bind(options);

        htmlRoot = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), options.HtmlRoot));
    }

    public IReadOnlyList<HtmlDocSection> GetSections() => GetOrBuildCache().Sections;

    public HtmlDocSection? TryGetSection(string? sectionSlug)
    {
        var normalizedSlug = NormalizeSlug(sectionSlug);
        return GetOrBuildCache().SectionsBySlug.GetValueOrDefault(normalizedSlug);
    }

    public HtmlDocPage? TryGetPage(string? sectionSlug, string? fileStem)
    {
        var normalizedSection = NormalizeSlug(sectionSlug);
        var normalizedFileStem = NormalizeSlug(fileStem);

        if (string.IsNullOrWhiteSpace(normalizedSection) || string.IsNullOrWhiteSpace(normalizedFileStem))
        {
            return null;
        }

        return GetOrBuildCache().PagesByKey.GetValueOrDefault((normalizedSection, normalizedFileStem));
    }

    public string RenderPageHtml(HtmlDocPage page, string currentUri)
    {
        if (!File.Exists(page.SourcePath))
        {
            return "<p>The selected document is unavailable.</p>";
        }

        var html = File.ReadAllText(page.SourcePath);
        return LinkRegex.Replace(html, match =>
        {
            var href = match.Groups["href"].Value;
            if (href.StartsWith("#", StringComparison.Ordinal))
            {
                return $"href=\"{currentUri}{href}\"";
            }

            return match.Value;
        });
    }

    private HtmlDocsCache GetOrBuildCache()
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

    private HtmlDocsCache BuildCache()
    {
        var sections = new List<HtmlDocSection>();
        var sectionsBySlug = new Dictionary<string, HtmlDocSection>(StringComparer.OrdinalIgnoreCase);
        var pagesByKey = new Dictionary<(string SectionSlug, string FileStem), HtmlDocPage>();

        if (Directory.Exists(htmlRoot))
        {
            foreach (var directoryPath in Directory.EnumerateDirectories(htmlRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var directoryName = Path.GetFileName(directoryPath);
                var sectionSlug = NormalizeSlug(directoryName);
                var sectionName = ToDisplayName(directoryName);

                var pages = Directory.EnumerateFiles(directoryPath, "*.html")
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(path =>
                    {
                        var fileStem = Path.GetFileNameWithoutExtension(path);
                        var page = new HtmlDocPage(
                            sectionSlug,
                            sectionName,
                            fileStem,
                            ToDisplayName(fileStem),
                            path);

                        pagesByKey[(sectionSlug, NormalizeSlug(fileStem))] = page;
                        return page;
                    })
                    .ToList();

                var section = new HtmlDocSection(sectionName, sectionSlug, pages);
                sections.Add(section);
                sectionsBySlug[sectionSlug] = section;
            }
        }

        return new HtmlDocsCache(sections, sectionsBySlug, pagesByKey);
    }

    private static string NormalizeSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var lowercase = input.Trim().ToLowerInvariant();
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

    private sealed class HtmlDocsCache(
        List<HtmlDocSection> sections,
        Dictionary<string, HtmlDocSection> sectionsBySlug,
        Dictionary<(string SectionSlug, string FileStem), HtmlDocPage> pagesByKey)
    {
        public List<HtmlDocSection> Sections { get; } = sections;
        public Dictionary<string, HtmlDocSection> SectionsBySlug { get; } = sectionsBySlug;
        public Dictionary<(string SectionSlug, string FileStem), HtmlDocPage> PagesByKey { get; } = pagesByKey;
    }
}