using gAPI.Generated;
using LanCloud.Host.Models;
using LanCloud.Host.Services;
using LanCloud.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new HostConfig([new LocalShare(
    "E:\\Films")]);
    //Path.Combine(Environment.CurrentDirectory, "LocalData"))]);
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAutoAuthClient("https://localhost:7087");
builder.Services.AddAutoWssClient("wss://127.0.0.1:7087");

builder.Services.AddHostedService<HostHub>();
builder.Services.AddSingleton(config);

var app = builder.Build();
app.Run();