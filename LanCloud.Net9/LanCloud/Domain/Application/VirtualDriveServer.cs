using LanCloud.Domain.Drive;

namespace LanCloud.Domain.Application;

public class VirtualDriveServer : IDisposable
{
    public VirtualDriveServer(LocalApplication application)
    {
        Application = application;

        var mountPoint = 
            string.IsNullOrWhiteSpace(application.Config.VirtualDriveMountPoint)
            ? "N:\\"
            : application.Config.VirtualDriveMountPoint;
        var volumeLabel =
            string.IsNullOrWhiteSpace(application.Config.VirtualDriveVolumeLabel)
            ? "LANCloud"
            : application.Config.VirtualDriveVolumeLabel;

        try
        {
            MountOptions = new DriveMountOptions(mountPoint, volumeLabel, application.Config.VirtualDriveReadOnly);
            DriveServer = new DriveServer(MountOptions, application);
            DriveServer.Start();
        }
        catch (Exception ex)
        {
            application.Logger.Error(ex);
            DriveServer?.Dispose();
            DriveServer = null;
        }

        application.Logger.Info($"OK");
    }

    public LocalApplication Application { get; }
    public DriveMountOptions? MountOptions { get; }
    public DriveServer? DriveServer { get; }

    public void Dispose()
    {
        DriveServer?.Dispose();
    }
}
