//using gAPI.Core.Server.Authentication;
//using gAPI.Core.Server.Entities;
//using gAPI.Core.Server.Storage;
//using LanCloud.Shared.Dtos;

//namespace LanCloud.Api;

//public class StateMapping(
//    IStorageService storageService)
//    : AuthenticationStateMapping<User, State>
//{
//    public override async Task<State> ToDtoAsync(
//        User? dbUser, 
//        UserToken<User>? dbToken, 
//        Ip<User> dbIp,
//        State? receivedClientState,
//        CancellationToken ct)
//    {
//        var state = await base.ToDtoAsync(dbUser, dbToken, dbIp, receivedClientState, ct);
//        if (dbUser != null)
//        {
//            state.ProfilePictureUrl = await storageService.GetStorageFileUrlAsync(dbUser, ct);
//        }
//        return state;
//    }
//}
