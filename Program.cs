using APSIM.Docs.Components;
using APSIM.Docs.Components.State;
using APSIM.Docs.Utility;
using Models.Core;
using Models;
using Models.PMF.Phen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents()
    .AddHubOptions(options =>
{
    // These settings prevent "Attempting to reconnect to server..." modal messages from popping up.
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
    options.HandshakeTimeout = TimeSpan.FromSeconds(60);
});
    
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


// Check if the time is between 17:00 and 18:00 UTC
// If it is then generate all the docs
if (DateTime.UtcNow.Hour == 17)
{
    Console.WriteLine($"Starting documentation generation at {DateTime.UtcNow} UTC");
    //On startup, make doc files one at a time as to not kill server
    List<string> validationFileNames = ["AgPasture", "Barley", "Canola", "Chicory", "Chickpea", "Clock", "SorghumDCaPST", "Eucalyptus", "FodderBeet", "Gliricidia", "Maize", "MicroClimate", "Mungbean", "Nutrient", "Oats", "OilPalm", "Peanut", "Pinus", "PlantainForage", "Potato", "RedClover", "SCRUM", "STRUM", "SPRUM", "Slurp", "SoilArbitrator", "SoilTemperature", "Sorghum", "Soybean", "Sugarcane", "Stock", "SWIM", "Wheat", "WhiteClover", "ClimateController", "Lifecycle", "Manager", "Sensitivity_MorrisMethod", "Sensitivity_SobolMethod", "Sensitivity_FactorialANOVA", "PredictedObserved", "Report", "CLEM_Example_Cropping", "CLEM_Example_Grazing"];

    List<IModel> modelsToDocument = new() { new Clock(), new ZadokPMFWheat() };

    DocumentationUtility.CreateHTMLForValidations(validationFileNames);

    DocumentationUtility.CreateHTMLForModelTypes(modelsToDocument);

    Console.WriteLine($"Documentation generation complete.");
}

app.Run();

