using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWP;
using MudBlazor.Services;
using PanoramicData.Blazor.Extensions;
using AntDesign;
using BlazorWP.Data;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddPanoramicDataBlazor();
builder.Services.AddAntDesign();
builder.Services.AddScoped<TreeState>();

await builder.Build().RunAsync();
