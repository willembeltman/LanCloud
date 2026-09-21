using gAPI.Core.Client.Config;
using LanCloud.Shared.Models;

namespace LanCloud.Host;

public record LanCloudHostConfig(
    string ApiBackendUrl,
    string WssBackendUrl,
    LocalShare[] Shares)
    : ClientConfig(ApiBackendUrl, WssBackendUrl);