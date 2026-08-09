namespace LanCloud.Domain.Drive;

public sealed class DriveMountOptions
{
    public DriveMountOptions(string mountPoint, string volumeLabel, bool readOnly = false)
    {
        MountPoint = mountPoint;
        VolumeLabel = volumeLabel;
        ReadOnly = readOnly;
    }

    public string MountPoint { get; }
    public string VolumeLabel { get; }
    public bool ReadOnly { get; }
}
