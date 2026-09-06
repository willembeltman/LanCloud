using LanCloud.Shared.Dtos;
using gAPI.Core.Serializers;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace LanCloud.Shared.Dtos;

public static class ShareEntryDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x951C1F21;
    public const uint SchemaHash = 0x1C31B869;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, ShareEntryDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        ___writer.Write(value.Name);
        ___writer.Write(value.Path);
        ___writer.Write(value.IsDirectory);
        ___writer.Write(value.Size);
        ___writer.Write(value.Created);
        ___writer.Write(value.LastModified);
        ___writer.Write(value.SessionId != null);
        if (value.SessionId != null)
           ___writer.Write(value.SessionId);
    }

    [IsSerializerRead]
    public static ShareEntryDto ReadShareEntryDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new ShareEntryDto();
        value.Name = ___reader.ReadString();
        value.Path = ___reader.ReadString();
        value.IsDirectory = ___reader.ReadBoolean();
        value.Size = ___reader.ReadInt64();
        value.Created = ___reader.ReadDateTime();
        value.LastModified = ___reader.ReadDateTime();
        value.SessionId = ___reader.ReadBoolean() ? ___reader.ReadSessionId() : null;
        return value;
    }
}