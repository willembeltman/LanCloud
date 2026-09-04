using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using LanCloud.Shared.Dtos;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Generated;

public static class ShareEntryDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x951C1F21;
    public const uint SchemaHash = 0x1C31B869;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, ShareEntryDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Name);
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Path);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.IsDirectory);
        PrimitivesSpanSerializer.WriteInt64(ref ___span, ref ___offset, value.Size);
        DateTimeSerializers.Write(ref ___span, ref ___offset, value.Created);
        DateTimeSerializers.Write(ref ___span, ref ___offset, value.LastModified);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Write(ref ___span, ref ___offset, value.SessionId);
    }

    [IsSpanSerializerRead]
    public static ShareEntryDto ReadShareEntryDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new ShareEntryDto();
        value.Name = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Path = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.IsDirectory = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset);
        value.Size = PrimitivesSpanSerializer.ReadInt64(___span, ref ___offset);
        value.Created = DateTimeSerializers.ReadDateTime(___span, ref ___offset);
        value.LastModified = DateTimeSerializers.ReadDateTime(___span, ref ___offset);
        value.SessionId = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : SessionIdSpanSerializer.ReadSessionId(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, ShareEntryDto value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.Name);
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.Path);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.IsDirectory);
        PrimitivesSpanSerializer.LengthInt64(ref ___offset, value.Size);
        DateTimeSerializers.Length(ref ___offset, value.Created);
        DateTimeSerializers.Length(ref ___offset, value.LastModified);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Length(ref ___offset, value.SessionId);
        return ___offset;
    }
}