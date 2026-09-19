using gAPI.Generated;
using LanCloud.Api.Helpers;
using LanCloud.Api.Models;
using LanCloud.Api.Services;
using LanCloud.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWssServer("Server=localhost;Port=9494;");
builder.Services.AddAutoAuthServer();
builder.Services.AddControllers(); // For the WebDav controller
builder.Services.AddHostedService<FtpServer>();
builder.Services.AddScoped<FileSystem>();
builder.Services.AddSingleton<RespondedEntryCollection>();

var localShareDirectory = Path.Combine(Environment.CurrentDirectory, "LocalData");
var localShare = new LocalShare(localShareDirectory);
var apiConfig = new ApiConfig(localShare);
builder.Services.AddSingleton(apiConfig);

var app = builder.Build();
app.MapAutoWssServer();
app.MapAutoAuthServer();
app.MapControllers(); // for the WebDav controller
app.UseHttpsRedirection();
app.Run();
