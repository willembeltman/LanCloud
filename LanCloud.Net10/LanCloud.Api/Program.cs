using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Models;
using LanCloud.Api.Services;
using LanCloud.Shared.Models;

var config = new ApiConfig(new LocalShare(Path.Combine(Environment.CurrentDirectory, "LocalData")));
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWss("LanCloudApp");
builder.Services.AddControllers();
builder.Services.AddScoped<IFileSystem, FileSystem>();
builder.Services.AddSingleton<EntryCollection>();
builder.Services.AddSingleton(config);

var app = builder.Build();
app.MapAutoWss();
app.MapControllers();
app.UseHttpsRedirection();
app.Run();
