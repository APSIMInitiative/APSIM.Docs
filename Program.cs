using APSIM.Docs.Components;
using APSIM.Docs.Components.State;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    
builder.Services.AddScoped<StateContainer>();
builder.Services.AddBlazorBootstrap();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

//On startup, make doc files one at a time as to not kill server
List<string> models = ["AgPasture", "Barley", "Canola", "Chicory", "Chickpea", "Clock", "SorghumDCaPST", "Eucalyptus", "FodderBeet", "Gliricidia", "Maize", "MicroClimate", "Mungbean", "Nutrient", "Oats", "OilPalm", "Peanut", "Pinus", "PlantainForage", "Potato", "RedClover", "SCRUM", "Slurp", "SoilArbitrator", "SoilTemperature", "Sorghum", "Soybean", "Sugarcane", "Stock", "SWIM", "Wheat", "WhiteClover", "ClimateController", "Lifecycle", "Manager", "Sensitivity_MorrisMethod", "Sensitivity_SobolMethod", "Sensitivity_FactorialANOVA", "PredictedObserved", "Report", "CLEM_Example_Cropping", "CLEM_Example_Grazing"];

foreach (string name in models)
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
        using (StreamWriter outputFile = new StreamWriter(Path.Combine("./", name+".html")))
        {
            outputFile.Write(docString);
        }
        Console.WriteLine($"Documentation generated for {name}");
    }
    catch 
    {
        Console.WriteLine($"Documentation failed for {name}");
    }
    
}
Console.WriteLine($"Documentation generation complete.");



app.Run();
