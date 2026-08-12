using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;
using gAPI.Core.Server;
using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;
using gAPI.Generated;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWss("LanCloudApp");
builder.Services.AddScoped<IServerAuthenticationService, EmptyServerAuthenticationService>();

var app = builder.Build();
app.MapAutoWss();
app.MapStateEndpoint_ForNoMiddleware<AuthUser, AuthStateDto>();
app.UseHttpsRedirection();
app.Run();
