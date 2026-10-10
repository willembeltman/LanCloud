using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dashboard;

namespace LanCloud.Api;

public static class AddConsoleDashboardExtension
{
    public static IServiceCollection AddConsoleDashboard(
        this IServiceCollection services,
        ServerConfig serverConfig)
    {
        var nameGetter = new ApplicationNameGetter("Rekenkamer Dashboard");
        services.AddSingleton(nameGetter);
        var consoleBuffer = new ConsoleBuffer();
        services.AddSingleton(consoleBuffer);
        if (CanRenderConsole())
            services.AddHostedService<DashboardConsoleOutput>();
        else
            services.AddHostedService<NormalConsoleOutput>();
        services.AddSingleton<IDashboardName>(sp => serverConfig);
        services.AddSingleton<IDashboardSnapshotGetter, DashboardSnapshotGetter>();

        return services;
    }
    static bool CanRenderConsole()
    {
        try
        {
            return !Console.IsOutputRedirected && Console.WindowWidth >= 10;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
