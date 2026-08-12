using gAPI.Core.Client;
using gAPI.Core.Client.Navigation;
using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Host.Services;
using LanCloud.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HostHub>();
builder.Services.AddAutoWss("https://localhost:7087", "wss://127.0.0.1:7087");
builder.Services.AddAuthenticationServices("https://localhost:7087");
builder.Services.AddScoped<IUriNavigationManager, StaticNavigationManager>();

builder.Services.AddSingleton(new HostConfig([new LocalShare()]));

//builder.Services.AddAuthenticationServices<State>(builder.Configuration["FrontendConfig:ApiBackendUrl"] ?? "https://api.dinostamp.nl");
//builder.Services.AddScoped<IStateParser<State>, StateParser>();

var app = builder.Build();
app.Run(); // Wordt gecalled
