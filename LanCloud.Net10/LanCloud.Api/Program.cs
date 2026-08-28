using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Services;
using LanCloud.Shared.Models;

var localShare = new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalData"));
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWssServer(builder.Configuration);
builder.Services.AddAutoAuthServer(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddScoped<FileSystem>();
builder.Services.AddSingleton(localShare);
builder.Services.AddSingleton<EntryCollection>();

var app = builder.Build();
app.MapAutoWssServer();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
