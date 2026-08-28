using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;
using LanCloud.Shared.Dtos;

namespace LanCloud.Api.Authentication;

public class StateMapping
    : AuthenticationStateMapping<AuthUser, StateDto>
{
    public override async Task<StateDto> ToDtoAsync(
        AuthUser? dbUser,
        UserToken<AuthUser>? dbToken,
        Ip<AuthUser> dbIp,
        StateDto? receivedClientState,
        CancellationToken ct)
    {
        var state = await base.ToDtoAsync(dbUser, dbToken, dbIp, receivedClientState, ct);
        return state;
    }
}
