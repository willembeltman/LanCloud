using gAPI.Core.Server.Extensions;
using LanCloud.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddStorageLanCloud();

var app = builder.Build();
app.MapStorageLanCloud();
app.Run();
