using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using LanCloud.Shared.Dtos;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace LanCloud.Shared.Dtos;

public static class ShareEntryDtoMultipartFormDataContentSerializer
{

    [IsMultipartFormDataContentSerializer]
    public static void Write(this MultipartFormDataContent ___content, string ___name, ShareEntryDto value)
    {
        ___content.Add(new StringContent(value.Name), "Name");
        ___content.Add(new StringContent(value.Path), "Path");
        ___content.Add(new StringContent(value.IsDirectory.ToString()), "IsDirectory");
        ___content.Add(new StringContent(value.Size.ToString()), "Size");
        ___content.Add(new StringContent(value.Created.ToString("O")), "Created");
        ___content.Add(new StringContent(value.LastModified.ToString("O")), "LastModified");
        if (value.SessionId != null)
            SessionIdMultipartFormDataContentSerializer.Write(___content, "SessionId", value.SessionId);
    }
}