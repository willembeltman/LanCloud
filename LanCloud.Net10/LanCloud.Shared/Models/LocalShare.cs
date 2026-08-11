using LanCloud.Shared.Dtos;
using System.Runtime.CompilerServices;

namespace LanCloud.Shared.Models;

public class LocalShare
{
    public LocalShare() { }
    public LocalShare(string localFullName)
    {
        LocalFullName = localFullName;
    }

    public string LocalFullName { get; set; } = string.Empty;

    public async IAsyncEnumerable<ShareEntryDto> ListDirectory(
        string relativePath, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativePath);
        var list = Directory.EnumerateFileSystemEntries(path);
        foreach (var fullName in list)
        {
            yield break;
        }
    }
    public async IAsyncEnumerable<ShareEntryDto> Get(
        string relativeFullName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativeFullName);
        var list = Directory.EnumerateFileSystemEntries(path);
        foreach (var fullName in list)
        {
            yield break;
        }
    }
    public async IAsyncEnumerable<FileChunkDto> ReadFile(
        string relativeFullName, 
        [EnumeratorCancellation] CancellationToken ct)
    {
        var path = CreateLocalFullName(relativeFullName);
        var list = Directory.EnumerateFileSystemEntries(path);
        foreach (var fullName in list)
        {
            yield break;
        }
    }

    public async Task CreateDirectory(string path, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(string path, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task Write(string path, Stream content, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private string CreateLocalFullName(string relativeName)
    {
        throw new NotImplementedException();
    }
}
