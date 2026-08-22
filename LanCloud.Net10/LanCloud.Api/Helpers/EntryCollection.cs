using LanCloud.Api.Models;
using System.Text.Json;

namespace LanCloud.Api.Helpers;

public class EntryCollection
{
    private static readonly string DiskFilePath = Path.Combine(Environment.CurrentDirectory, "entry_collection.json");

    public EntryCollection()
    {
        LoadFromDisk();
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(DiskFilePath))
            {
                var json = File.ReadAllText(DiskFilePath);
                var items = JsonSerializer.Deserialize<List<string>>(json);
                if (items != null)
                {
                    lock (_lock)
                    {
                        foreach (var item in items)
                            _removed.Add(item);
                    }
                }
            }
        }
        catch
        {
            // Negeren bij eerste start of corruptie
        }
    }

    private async Task SaveToDisk(CancellationToken ct)
    {
        try
        {
            List<string> list;
            lock (_lock)
            {
                list = _removed.ToList();
            }
            var json = JsonSerializer.Serialize(list);
            await File.WriteAllTextAsync(DiskFilePath, json, ct);
        }
        catch
        {
            // Negeren bij file I/O fouten
        }
    }

    private readonly HashSet<string> _removed = [];
    private readonly Lock _lock = new();
    public Dictionary<string, Entry> RespondedEntries { get; } = [];

    public Task CreateDirectory(string path, CancellationToken ct) => Remove(path, ct);

    public Task Delete(string path, CancellationToken ct)
    {
        lock (_lock)
        {
            _removed.Add(Normalize(path));
        }

        return SaveToDisk(ct);
    }

    public bool IsRemoved(string path)
    {
        var normalized = Normalize(path);

        lock (_lock)
        {
            if (_removed.Contains(normalized))
                return true;

            // Een parent-directory kan verwijderd zijn.
            var current = normalized;

            while (true)
            {
                var slash = current.LastIndexOf('/');

                if (slash < 0)
                    break;

                current = current[..slash];

                if (_removed.Contains(current))
                    return true;
            }
        }

        return false;
    }

    public Task Write(string path, CancellationToken ct) => Remove(path, ct);

    public Task Responded(string path, Entry entry, CancellationToken ct)
    {
        RespondedEntries[path] = entry;

        return SaveToDisk(ct);
    }

    private Task Remove(string path, CancellationToken ct)
    {
        var normalized = Normalize(path);

        lock (_lock)
        {
            _removed.Remove(normalized);

            return SaveToDisk(ct);
        }
    }

    private static string Normalize(string path)
        => path
            .Replace('\\', '/')
            .Trim('/');
}