using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Services;
using LanCloud.Shared.Models;

var localShare = new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalData"));
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWssServer("LanCloudApp");
builder.Services.AddControllers();
builder.Services.AddScoped<FileSystemApi>();
builder.Services.AddSingleton(localShare);
builder.Services.AddSingleton<EntryCollection>();

var app = builder.Build();
app.MapAutoWss();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
