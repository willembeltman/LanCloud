using LanCloud.Api.Models;

namespace LanCloud.Api.Helpers;

public class EntryCollection
{
    public EntryCollection()
    {
        LoadFromDisk();
    }

    private void LoadFromDisk()
    {
        throw new NotImplementedException();
    }

    private Task SaveToDisk(CancellationToken ct)
    {
        throw new NotImplementedException();
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

            // Als een directory verwijderd wordt, moeten ook
            // eventuele children verborgen blijven.
            var prefix = Normalize(path) + "/";

            _removed.RemoveWhere(x =>
                x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == false
                    ? false
                    : true);
        }

        // ↑ Zie opmerking hieronder: dit verwijdert juist de children.
        // Voor tombstones wil je die waarschijnlijk OOK bewaren.

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