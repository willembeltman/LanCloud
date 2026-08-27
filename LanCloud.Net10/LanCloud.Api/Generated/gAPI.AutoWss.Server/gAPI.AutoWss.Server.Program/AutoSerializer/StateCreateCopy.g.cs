using LanCloud.Shared.Dtos;
using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Generated;

public static class StateCreateCopy
{
    [IsCreateCopy]
    public static State CreateCopy(this State value)
    {
        var copy = new State();
        copy.User = value.User == null ? null : value.User.CreateCopy();
        copy.ForceReconnect = value.ForceReconnect;
        return copy;
    }
}