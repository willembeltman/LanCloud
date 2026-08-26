using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using LanCloud.Shared.Dtos;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Generated;

public static class StateSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x1DECA496;
    public const uint SchemaHash = 0x3CB30EC5;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, State value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.User != null);
        if (value.User != null)
            AuthStateUserDtoSpanSerializer.Write(ref ___span, ref ___offset, value.User);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.ForceReconnect);
    }

    [IsSpanSerializerRead]
    public static State ReadState(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new State();
        value.User = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : AuthStateUserDtoSpanSerializer.ReadAuthStateUserDto(___span, ref ___offset);
        value.ForceReconnect = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, State value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.User != null);
        if (value.User != null)
            AuthStateUserDtoSpanSerializer.Length(ref ___offset, value.User);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.ForceReconnect);
        return ___offset;
    }
}