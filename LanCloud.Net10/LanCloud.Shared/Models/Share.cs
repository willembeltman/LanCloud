using LanCloud.Shared.Dtos;
using System.Runtime.CompilerServices;

namespace LanCloud.Shared.Models;

public class Share
{
    public string LocalFullName { get; set; } = string.Empty;

    public void CreateDirectory(string path, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ShareEntryDto> ListDirectory(string relativePath, [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativePath);
        var list = Directory.EnumerateFileSystemEntries(path);
        foreach (var fullName in list)
        {
            yield break;
        }
    }
    public async IAsyncEnumerable<ShareEntryDto> Get(string relativeFullName, [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativeFullName);
        var list = Directory.EnumerateFileSystemEntries(path);
        foreach (var fullName in list)
        {
            yield break;
        }
    }

    private string CreateLocalFullName(string relativeName)
    {
        throw new NotImplementedException();
    }
}
