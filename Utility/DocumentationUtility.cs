using APSIM.Core;
using Models.Core;

namespace APSIM.Docs.Utility;

public static class DocumentationUtility
{
    /// <summary>
    /// Create HTML files for the given validation file names.
    /// </summary>
    /// <param name="validationFileNames"></param>
    public static void CreateHTMLForValidations(List<string> validationFileNames)
    {
        foreach (string name in validationFileNames)
        {
            try
            {
                string docString;
                if (OperatingSystem.IsWindows())
                {
                    docString = APSIM.Documentation.WebDocs.GetPage($"{Directory.GetCurrentDirectory()}/../ApsimX", name);
                }
                else
                {
                    docString = APSIM.Documentation.WebDocs.GetPage("/ApsimX", name);
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine("./", name + ".html")))
                {
                    outputFile.Write(docString);
                }
                Console.WriteLine($"Documentation generated for {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Documentation failed for {name}: {ex.ToString()}");
            }

        }
    }


    /// <summary>
    /// Create HTML files for the given model types.
    /// </summary>
    /// <param name="modelsToDocument"></param>
    public static void CreateHTMLForModelTypes(List<IModel> modelsToDocument)
    {
        foreach (IModel model in modelsToDocument)
        {
            try
            {
                Node.Create((INodeModel)model);
                string docString = APSIM.Documentation.WebDocs.GenerateWeb(model);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine("./", model.Name + ".html")))
                {
                    outputFile.Write(docString);
                }
                Console.WriteLine($"Documentation generated for model {model.Name}");
            }
            catch (Exception)
            {
                Console.WriteLine($"Documentation failed for {model.Name}");
            }
        }
    }
}