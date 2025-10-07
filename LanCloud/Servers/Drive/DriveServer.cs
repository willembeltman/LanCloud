using DokanNet;
using LanCloud.Interfaces;
using System;
using System.Threading;

namespace LanCloud.Servers.VirtualDrive
{
    public sealed class DriveServer : IDisposable
    {
        private string _Status { get; set; }
        public string Status
        {
            get => _Status;
            set
            {
                _Status = value;
                Application.StatusChanged();
            }
        }

        private readonly DriveMountOptions Options;
        private readonly IDriveFileSystem FileSystem;
        private readonly IApplication Application;

        private readonly DriveOperations Operations;
        private readonly ManualResetEventSlim MountCompleted = new ManualResetEventSlim(false);
        private readonly ILogger Logger;
        private Thread MountThread;

        private Dokan Dokan;
        private DokanInstance DokanInstance;

        public DriveServer(
            DriveMountOptions options, 
            IDriveFileSystem fileSystem,
            IApplication application,
            ILogger logger = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            Application = application ?? throw new ArgumentNullException(nameof(application));
            Logger = logger;

            Operations = new DriveOperations(FileSystem, Options);
        }

        public DokanStatus MountStatus { get; private set; } = DokanStatus.Success;
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

            // korte wait zodat Start() terugkeert nadat mount-proces gestart is (net als jouw eerdere gedrag)
            //MountCompleted.Wait(TimeSpan.FromSeconds(5));
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
                MountStatus = DokanStatus.Success;
                Logger?.Info("Mounted virtual drive at {0}", Options.MountPoint);
            }
            catch (DokanException ex)
            {
                MountStatus = ex.ErrorStatus;
                Logger?.Error("Dokan mount failed with status {0}: {1}", ex.ErrorStatus, ex.Message);
                Dokan?.Dispose();
                return;
            }
            catch (Exception ex)
            {
                MountStatus = DokanStatus.Error;
                Logger?.Error("Dokan mount failed with unexpected error: {0}", ex);
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
                Logger?.Error("Dokan mount failed with status {0}: {1}", ex.ErrorStatus, ex.Message);
                Dokan?.Dispose();
                return;
            }
            catch (Exception ex)
            {
                MountStatus = DokanStatus.Error;
                Logger?.Error("Dokan mount failed with unexpected error: {0}", ex);
                Dokan?.Dispose();
                return;
            }
            finally
            {
                MountStatus = DokanStatus.MountError;
                Logger?.Error("Dokan unmounted from {0}", Options.MountPoint);
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
}
