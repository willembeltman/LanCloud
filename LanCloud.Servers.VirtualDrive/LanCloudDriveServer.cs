using System;
using System.Threading;
using DokanNet;
using LanCloud.Shared.Interfaces;

namespace LanCloud.VirtualDrive
{
    public sealed class LanCloudDriveServer : IDisposable
    {
        private readonly ILanCloudFileSystem _fileSystem;
        private readonly LanCloudDriveMountOptions _options;
        private readonly LanCloudDriveOperations _operations;
        private readonly ManualResetEventSlim _mountCompleted = new ManualResetEventSlim(false);
        private readonly ILogger _dokanLogger;
        private Thread _mountThread;
        private DokanStatus _mountStatus = DokanStatus.Success;

        private Dokan _dokan;
        private DokanInstance _dokanInstance;

        public LanCloudDriveServer(ILanCloudFileSystem fileSystem, LanCloudDriveMountOptions options, ILogger dokanLogger = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _dokanLogger = dokanLogger;
            _operations = new LanCloudDriveOperations(_fileSystem, _options);
        }

        public bool IsRunning => _mountThread != null && _mountThread.IsAlive;
        public DokanStatus MountStatus => _mountStatus;

        public void Start(int threadCount = 2)
        {
            if (IsRunning)
                return;

            _mountCompleted.Reset();

            _mountThread = new Thread(() => MountWorker(threadCount))
            {
                IsBackground = true,
                Name = "LanCloud.VirtualDrive"
            };

            _mountThread.Start();

            // korte wait zodat Start() terugkeert nadat mount-proces gestart is (net als jouw eerdere gedrag)
            _mountCompleted.Wait(TimeSpan.FromSeconds(5));
        }

        private void MountWorker(int threadCount)
        {
            var mountOptions = DokanOptions.FixedDrive | DokanOptions.MountManager;
            if (_options.ReadOnly)
            {
                mountOptions |= DokanOptions.WriteProtection;
            }

            try
            {
                _dokan = new Dokan(_dokanLogger);
                var builder = new DokanInstanceBuilder(_dokan)
                    .ConfigureOptions(o =>
                    {
                        o.Options = mountOptions;
                        o.MountPoint = _options.MountPoint;
                        o.SingleThread = threadCount <= 1;
                    });

                _dokanInstance = builder.Build(_operations);
                _mountStatus = DokanStatus.Success;
                _dokanLogger?.Info("Mounted virtual drive at {0}", _options.MountPoint);
            }
            catch (DokanException ex)
            {
                _mountStatus = ex.ErrorStatus;
                _dokanLogger?.Error("Dokan mount failed with status {0}: {1}", ex.ErrorStatus, ex.Message);
                _dokan?.Dispose();
                return;
            }
            catch (Exception ex)
            {
                _mountStatus = DokanStatus.Error;
                _dokanLogger?.Error("Dokan mount failed with unexpected error: {0}", ex);
                _dokan?.Dispose();
                return;
            }
            finally
            {
                _mountCompleted.Set();
            }

            try
            {
                _dokanInstance.WaitForFileSystemClosed(10000);
            }
            finally
            {
                _dokanLogger?.Info("Dokan unmounted from {0}", _options.MountPoint);
                _dokanInstance?.Dispose();
                _dokan?.Dispose();
                _dokanInstance = null;
                _dokan = null;
            }
        }

        public void Dispose()
        {
            if (_mountThread == null)
                return;

            try
            {
                _dokan?.RemoveMountPoint(_options.MountPoint);
            }
            catch
            {
                // ignore unmount failures during shutdown
            }

            if (_mountThread.Join(TimeSpan.FromSeconds(5)) == false)
            {
                _mountThread.Interrupt();
            }

            _mountThread = null;
            _dokanInstance?.Dispose();
            _dokan?.Dispose();
            _dokanInstance = null;
            _dokan = null;
        }
    }
}
