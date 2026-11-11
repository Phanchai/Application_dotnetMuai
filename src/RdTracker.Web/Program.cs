using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RdTracker.UI;

// Browser host: the same Razor components as the MAUI app, compiled to WebAssembly.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddRdTracker();

await builder.Build().RunAsync();
