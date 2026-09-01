using LanCloud.Shared.Dtos;
using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace gAPI.Generated;

public static class StateDtoCreateCopy
{
    [IsCreateCopy]
    public static StateDto CreateCopy(this StateDto value)
    {
        var copy = new StateDto();
        copy.User = value.User == null ? null : value.User.CreateCopy();
        copy.ForceReconnect = value.ForceReconnect;
        copy.Index = value.Index;
        return copy;
    }
}