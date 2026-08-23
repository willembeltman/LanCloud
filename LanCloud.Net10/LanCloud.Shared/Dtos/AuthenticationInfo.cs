namespace LanCloud.Shared.Dtos;

public record AuthenticationInfo(
    bool Required,
    string Realm);