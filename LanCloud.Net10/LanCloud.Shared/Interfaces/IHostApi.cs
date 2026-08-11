using gAPI.Core.Attributes;

namespace StorageApi.Shared.Interfaces;

[GenerateApi]
public interface IHostApi
{
    Task SignalAsync(string txt);
}
