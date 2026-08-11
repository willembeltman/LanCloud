namespace LanCloud.Api.Helpers;

public class RemovedItemsCollection
{
    private readonly HashSet<string> _removed = [];
    private readonly Lock _lock = new();

    internal Task CreateDirectory(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Remove(path);

        return Task.CompletedTask;
    }

    internal Task Delete(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

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
        return Task.CompletedTask;
    }

    internal Task<bool> IsRemoved(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var normalized = Normalize(path);

        lock (_lock)
        {
            if (_removed.Contains(normalized))
                return Task.FromResult(true);

            // Een parent-directory kan verwijderd zijn.
            var current = normalized;

            while (true)
            {
                var slash = current.LastIndexOf('/');

                if (slash < 0)
                    break;

                current = current[..slash];

                if (_removed.Contains(current))
                    return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    internal Task Write(
        string path,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        Remove(path);

        return Task.CompletedTask;
    }

    private void Remove(string path)
    {
        var normalized = Normalize(path);

        lock (_lock)
        {
            _removed.Remove(normalized);
        }
    }

    private static string Normalize(string path)
        => path
            .Replace('\\', '/')
            .Trim('/');
}