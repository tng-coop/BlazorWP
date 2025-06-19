using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWP;
using MudBlazor.Services;
using PanoramicData.Blazor.Extensions;
using AntDesign;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Ensure configuration from appsettings.json and appsettings.{Environment}.json
// is loaded into builder.Configuration.
builder.Configuration.AddJsonStream(
    await builder.HostEnvironment.OpenAppSettingsStreamAsync());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddPanoramicDataBlazor();
builder.Services.AddAntDesign();

await builder.Build().RunAsync();
