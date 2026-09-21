using LanCloud.Shared.Models;

namespace LanCloud.Api;

public record LanCloudApiConfig(
    LocalShare LocalShare,
    string? CertificateFilename = null);