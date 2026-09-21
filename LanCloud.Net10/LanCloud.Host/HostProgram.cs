using gAPI.Core.Client.Config;
using gAPI.Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LanCloud.Host;

public class HostProgram
{
    // var config = new HostConfig([new LocalShare("E:\\Films")], "https://localhost:7087", "wss://127.0.0.1:7087");
    public static void Run(string[] args, LanCloudHostConfig config)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Services.AddAutoWssClient(config);
        builder.Services.AddAutoAuthClient(config);

        builder.Services.AddHostedService<HostHub>();
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton<ClientConfig>(sp => sp.GetRequiredService<LanCloudHostConfig>());

        var app = builder.Build();
        app.Run();
    }
}