using LanCloud.Interfaces;
using LanCloud.Models.Config;
using Newtonsoft.Json;

namespace LanCloud.Repositories;

public class ConfigRepository
{
    public ConfigRepository(string currentDirectory, ILogger logger)
    {
        Logger = logger;
        Fullname = Path.Combine(currentDirectory, "LanCloud.json");
    }

    public string Fullname { get; }
    public ILogger Logger { get; }

    public ApplicationConfig Load()
    {
        if (!File.Exists(Fullname))
        {
            Logger.Info("No config found, creating dummy config");
            var config = new ApplicationConfig()
            {
                HostName = "RpcC2",
                RefDirectoryName = "E:\\Test\\Ref",
                FileStripeBufferSize = 1024 * 4,
                FtpBufferMultiplier = 1,
                VirtualDriveBufferMultiplier = 1,
                RpcBufferMultiplier = 4,
                EnableFtpServer = true,
                EnableVirtualDrive = true,
                VirtualDriveMountPoint = "N:\\",
                VirtualDriveVolumeLabel = "LANCloud",
                VirtualDriveReadOnly = false,
                
                Servers = new RemoteApplicationConfig[]
                {
                    new RemoteApplicationConfig()
                    {
                        HostName = "192.168.178.69",
                        Port = 8080,
                        IsThisComputer = true
                    },
                    //new RemoteApplicationConfig()
                    //{
                    //    HostName = "192.168.178.32",
                    //    Port = 8080,
                    //    IsThisComputer = false
                    //}
                },
                Shares = new LocalShareConfig[]
                {
                    new LocalShareConfig()
                    {
                        DirectoryName = "E:\\Test\\1",
                        IsSSD = true,
                        Indexes = [0]
                    },
                    new LocalShareConfig()
                    {
                        DirectoryName = "E:\\Test\\2",
                        IsSSD = true,
                        Indexes = [1]
                    },
                    new LocalShareConfig()
                    {
                        DirectoryName = "E:\\Test\\P",
                        IsSSD = true,
                        Indexes = [0, 1]
                    }
                },

            };
            Save(config);
            return config;
        }

        Logger.Info("Config found, reading config settings");
        using (var reader = new StreamReader(Fullname))
        {
            var json = reader.ReadToEnd();
            var config = JsonConvert.DeserializeObject<ApplicationConfig>(json);

            if ((config?.Servers == null || config.Servers.Length == 0) &&
                (config?.Shares == null || config.Shares.Length == 0))
                throw new Exception("Nothing is configured, please setup LanCloud.config file.");

            if (string.IsNullOrWhiteSpace(config.VirtualDriveMountPoint))
            {
                config.VirtualDriveMountPoint = "N:\\";
            }
            if (string.IsNullOrWhiteSpace(config.VirtualDriveVolumeLabel))
            {
                config.VirtualDriveVolumeLabel = "LANCloud";
            }
            if (!config.EnableFtpServer && !config.EnableVirtualDrive)
            {
                config.EnableVirtualDrive = true;
            }

            return config;
        }
    }

    public void Save(ApplicationConfig config)
    {
        Logger.Info("Saving the config");

        if (File.Exists(Fullname))
        {
            File.Delete(Fullname);
        }

        var json = JsonConvert.SerializeObject(config);
        using (var writer = new StreamWriter(Fullname))
        {
            writer.Write(json);
        }
    }
}

