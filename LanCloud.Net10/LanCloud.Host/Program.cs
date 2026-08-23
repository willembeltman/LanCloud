using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Host.Services;
using LanCloud.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new HostConfig([new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalData"))]);
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HostHub>();
builder.Services.AddAutoWss("https://localhost:7087", "wss://127.0.0.1:7087");
builder.Services.AddSingleton(config);

var app = builder.Build();
app.Run();
