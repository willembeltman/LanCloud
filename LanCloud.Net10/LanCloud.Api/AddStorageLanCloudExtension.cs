using gAPI.Generated;
using LanCloud.Api.Collections;
using LanCloud.Api.Ftp;
using LanCloud.Api.Interfaces;
using LanCloud.Api.Services;
using LanCloud.Shared.Models;

namespace LanCloud.Api;

public static class AddStorageLanCloudExtension
{
    public static WebApplicationBuilder AddStorageLanCloud(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddAutoWssServer();
        builder.Services.AddAutoAuthServer();
        builder.Services.AddControllers(); // For the WebDav controller
        builder.Services.AddHostedService<FtpServer>();
        builder.Services.AddScoped<IFileSystemDirect, FileSystem>();
        builder.Services.AddSingleton<RespondedEntryCollection>();

        var localShareDirectory = Path.Combine(Environment.CurrentDirectory, "LocalData");
        var localShare = new LocalShare(localShareDirectory);
        var apiConfig = new LanCloudApiConfig(localShare);
        builder.Services.AddSingleton(apiConfig);

        return builder;
    }
    public static WebApplication MapStorageLanCloud(this WebApplication app)
    {
        app.MapAutoWssServer();
        app.MapAutoAuthServer();
        app.MapControllers(); // for the WebDav controller
        app.UseHttpsRedirection();

        return app;
    }
}
