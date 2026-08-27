using gAPI.Core.Dtos;

namespace LanCloud.Shared.Dtos;

public class StateDto : AuthStateDto
{
    public string? ProfilePictureUrl { get; set; }
}
