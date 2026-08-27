using LanCloud.Shared.Dtos;
using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Generated;

public static class StateComparer
{
    [IsComparer]
    public static bool IsDifferent(this State value, State otherValue)
    {
        if (value.User is null)
        {
            if (otherValue.User is not null) return true;
        }
        else
        {
            if (otherValue.User is null) return true;
            if (value.User.IsDifferent(otherValue.User)) return true;
        }

        if (value.ForceReconnect != otherValue.ForceReconnect) return true;
        if (value.ProfilePictureUrl != otherValue.ProfilePictureUrl) return true;
        return false;
    }
}