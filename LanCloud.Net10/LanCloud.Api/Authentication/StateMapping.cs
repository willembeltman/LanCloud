using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Authentication;

public class StateMapping
    : AuthenticationStateMapping<User, StateDto>
{
    public override async Task<StateDto> ToDtoAsync(
        User? dbUser,
        UserToken<User>? dbToken,
        Ip<User> dbIp,
        StateDto? receivedClientState,
        CancellationToken ct)
    {
        var state = await base.ToDtoAsync(dbUser, dbToken, dbIp, receivedClientState, ct);
        return state;
    }
}
