using LanCloud.Shared.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

namespace LanCloud.Shared.Dtos;

public static class ShareEntryDtoCreateCopy
{
    [IsCreateCopy]
    public static ShareEntryDto CreateCopy(this ShareEntryDto value)
    {
        var copy = new ShareEntryDto();
        copy.Name = value.Name;
        copy.Path = value.Path;
        copy.IsDirectory = value.IsDirectory;
        copy.Size = value.Size;
        copy.Created = value.Created;
        copy.LastModified = value.LastModified;
        copy.SessionId = value.SessionId;
        return copy;
    }
}