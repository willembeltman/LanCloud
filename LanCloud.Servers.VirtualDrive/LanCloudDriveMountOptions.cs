namespace LanCloud.VirtualDrive
{
    public sealed class LanCloudDriveMountOptions
    {
        public LanCloudDriveMountOptions(string mountPoint, string volumeLabel, bool readOnly = false)
        {
            MountPoint = mountPoint;
            VolumeLabel = volumeLabel;
            ReadOnly = readOnly;
        }

        public string MountPoint { get; }
        public string VolumeLabel { get; }
        public bool ReadOnly { get; }
    }
}
