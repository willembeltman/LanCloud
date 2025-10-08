using DokanNet;
using LanCloud.Domain.Application;
using LanCloud.Interfaces;

namespace LanCloud.Domain.Drive;

public sealed class DriveServer : IDisposable
{
    private string _Status { get; set; } = string.Empty;
    public string Status
    {
        get => _Status;
        set
        {
            _Status = value;
            Application.StatusChanged();
        }
    }

    private readonly DriveOperations Operations;
    private readonly ManualResetEventSlim MountCompleted = new ManualResetEventSlim(false);

    private Thread? MountThread;
    private Dokan? Dokan;
    private DokanInstance? DokanInstance;

    public DriveServer(
        DriveMountOptions options,
        LocalApplication application)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Application = application ?? throw new ArgumentNullException(nameof(application));
        Operations = new DriveOperations(this);
    }

    public DriveMountOptions Options { get; }
    public LocalApplication Application { get; }
    public DokanStatus MountStatus { get; set; } = DokanStatus.Success;

    public FileSystem FileSystem => Application.FileSystem;
    public ILogger Logger => Application.Logger;
    public bool IsRunning => MountThread != null && MountThread.IsAlive;

    public void Start(int threadCount = 2)
    {
        if (IsRunning)
            return;

        MountCompleted.Reset();

        MountThread = new Thread(() => MountWorker(threadCount))
        {
            IsBackground = true,
            Name = "LanCloud.Servers.VirtualDrive"
        };

        MountThread.Start();
    }

    private void MountWorker(int threadCount)
    {
        var mountOptions = DokanOptions.FixedDrive | DokanOptions.MountManager;
        if (Options.ReadOnly)
        {
            mountOptions |= DokanOptions.WriteProtection;
        }

        try
        {
            Dokan = new Dokan(Logger);
            var builder = new DokanInstanceBuilder(Dokan)
                .ConfigureOptions(o =>
                {
                    o.Options = mountOptions;
                    o.MountPoint = Options.MountPoint;
                    o.SingleThread = threadCount <= 1;
                });

            DokanInstance = builder.Build(Operations);
            Status = Logger.Info($"OK");
        }
        catch (DokanException ex)
        {
            MountStatus = ex.ErrorStatus;
            Status = Logger.Error($"Dokan mount failed with status {MountStatus}: {ex}");
            Dokan?.Dispose();
            return;
        }
        catch (Exception ex)
        {
            MountStatus = DokanStatus.Error;
            Status = Logger.Error($"Dokan mount failed with unexpected error: {ex}");
            Dokan?.Dispose();
            return;
        }
        finally
        {
            MountCompleted.Set();
        }

        try
        {
            DokanInstance.WaitForFileSystemClosed(100000);
        }
        catch (DokanException ex)
        {
            MountStatus = ex.ErrorStatus;
            Status = Logger.Error($"Dokan mount failed with status {MountStatus}: {ex}");
            Dokan?.Dispose();
            return;
        }
        catch (Exception ex)
        {
            MountStatus = DokanStatus.Error;
            Status = Logger.Error($"Dokan mount failed with unexpected error: {ex}");
            Dokan?.Dispose();
            return;
        }
        finally
        {
            MountStatus = DokanStatus.MountError;
            Status = Logger.Error($"Dokan mount failed with status {Options.MountPoint}");
            DokanInstance?.Dispose();
            Dokan?.Dispose();
            DokanInstance = null;
            Dokan = null;
        }
        Application.StatusChanged();
    }

    public void Dispose()
    {
        if (MountThread == null)
            return;

        try
        {
            Dokan?.RemoveMountPoint(Options.MountPoint);
        }
        catch
        {
            // ignore unmount failures during shutdown
        }

        if (MountThread.Join(TimeSpan.FromSeconds(5)) == false)
        {
            MountThread.Interrupt();
        }

        MountThread = null;
        DokanInstance?.Dispose();
        Dokan?.Dispose();
        DokanInstance = null;
        Dokan = null;
    }
}
