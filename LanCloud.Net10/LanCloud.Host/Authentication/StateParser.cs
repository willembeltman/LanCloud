//using gAPI.Core.Dtos;
//using LanCloud.Shared.Dtos;
//using Microsoft.Extensions.Primitives;

//namespace gAPI.Generated;

//public class StateParser
//{
//    public bool TryParse(string? value, out State state)
//    {
//        state = default!;

//        if (string.IsNullOrWhiteSpace(value))
//            return false;

//        try
//        {
//            var data = Convert.FromBase64String(value);
//            var offset = 0;
//            try
//            {
//                state = data.ReadState(ref offset);
//            }
//            catch (Exception ex)
//            {
//                return false;
//            }
//            return true;
//        }
//        catch (Exception ex)
//        {
//            return false;
//        }
//    }
//    public StringValues ToStringValuesBase64(State value)
//    {
//        string? base64State = ToStringBase64(value);
//        return new StringValues(base64State);
//    }
//    public string? ToStringBase64(State? value)
//    {
//        if (value == null)
//            value = new State();

//        byte[] Buffer = new byte[64 * 1024];
//        var span = new Span<byte>(Buffer, 0, Buffer.Length);
//        var offset = 0;
//        span.Write(ref offset, value);
//        var base64State = Convert.ToBase64String(Buffer, 0, offset);
//        return base64State;
//    }
//    public bool IsDifferent(State? value1, State? value2)
//    {
//        if (value1 == null && value2 == null) return true;
//        if (value1 == null || value2 == null) return false;
//        return value1.IsDifferent(value2);
//    }
//    public State? CreateCopy(State? value)
//    {
//        return value?.CreateCopy();
//    }
//}
