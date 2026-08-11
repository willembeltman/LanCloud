using gAPI.Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HostService>();
builder.Services.AddAutoWss("https://localhost:7087", "wss://127.0.0.1:7087");

//builder.Services.AddAuthenticationServices<State>(builder.Configuration["FrontendConfig:ApiBackendUrl"] ?? "https://api.dinostamp.nl");
//builder.Services.AddScoped<IStateParser<State>, StateParser>();

var host = builder.Build();
host.Run();
