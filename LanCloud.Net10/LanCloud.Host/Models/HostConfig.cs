using LanCloud.Shared.Models;

namespace LanCloud.Host.Models;

public record HostConfig(
    LocalShare[] Shares);
