using LanCloudSimple.Shared.Enums;

namespace LanCloudSimple.Shared.Models;

public class FileUpdateInfo
{
    public FileUpdateType UpdateType { get; set; }
    public CloudFileDto File { get; set; } = null!;
}
