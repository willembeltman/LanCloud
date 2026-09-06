using LanCloud.Shared.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace LanCloud.Shared.Dtos;

public static class ShareEntryDtoComparer
{
    [IsComparer]
    public static bool IsDifferent(this ShareEntryDto value, ShareEntryDto otherValue)
    {
        if (value.Name != otherValue.Name) return true;
        if (value.Path != otherValue.Path) return true;
        if (value.IsDirectory != otherValue.IsDirectory) return true;
        if (value.Size != otherValue.Size) return true;
        if (value.Created != otherValue.Created) return true;
        if (value.LastModified != otherValue.LastModified) return true;
        if (value.SessionId != otherValue.SessionId) return true;
        return false;
    }
}