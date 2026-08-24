using LanCloud.Api.Models;

namespace LanCloud.Api.Helpers;

public class EntryCollection
{
    private readonly Lock _lock = new();

    public Dictionary<string, Entry> RespondedEntries { get; } = [];

    public void Responded(string path, Entry entry)
    {
        lock (_lock)
        {
            RespondedEntries[path] = entry;
        }
    }

    public bool TryGet(string path, out Entry? entry)
    {
        lock (_lock)
        {
            return RespondedEntries.TryGetValue(path, out entry);
        }
    }
    public static string Normalize(string path)
        => path
            .Replace('\\', '/')
            .Trim('/');
}

//public class EntryCollection
//{
//    private static readonly string DiskFilePath = Path.Combine(Environment.CurrentDirectory, "entry_collection.json");

//    public EntryCollection()
//    {
//        LoadFromDisk();
//    }

//    private void LoadFromDisk()
//    {
//        try
//        {
//            if (File.Exists(DiskFilePath))
//            {
//                var json = File.ReadAllText(DiskFilePath);

//                var state = DeserializeState(json);
//                if (state != null)
//                {
//                    lock (_lock)
//                    {
//                        foreach (var item in state.RemovedPaths)
//                            _removed.Add(Normalize(item));

//                        foreach (var move in state.Moves)
//                            _moves[Normalize(move.SourcePath)] = Normalize(move.DestinationPath);
//                    }
//                }
//            }
//        }
//        catch
//        {
//            // Negeren bij eerste start of corruptie
//        }
//    }

//    private async Task SaveToDisk(CancellationToken ct)
//    {
//        try
//        {
//            EntryCollectionState state;
//            lock (_lock)
//            {
//                state = new EntryCollectionState
//                {
//                    RemovedPaths = _removed.Order().ToList(),
//                    Moves = _moves
//                        .OrderBy(a => a.Key)
//                        .Select(a => new EntryMove
//                        {
//                            SourcePath = a.Key,
//                            DestinationPath = a.Value
//                        })
//                        .ToList()
//                };
//            }
//            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
//            await File.WriteAllTextAsync(DiskFilePath, json, ct);
//        }
//        catch
//        {
//            // Negeren bij file I/O fouten
//        }
//    }

//    private readonly HashSet<string> _removed = [];
//    private readonly Dictionary<string, string> _moves = [];
//    private readonly Lock _lock = new();
//    public Dictionary<string, Entry> RespondedEntries { get; } = [];

//    public Task CreateDirectory(string path, CancellationToken ct) => Restore(path, ct);

//    public Task Delete(string path, CancellationToken ct)
//    {
//        lock (_lock)
//        {
//            _removed.Add(Normalize(path));
//        }

//        return SaveToDisk(ct);
//    }

//    public bool IsRemoved(string path)
//    {
//        var normalized = Normalize(path);

//        lock (_lock)
//        {
//            if (_removed.Contains(normalized))
//                return true;

//            // Een parent-directory kan verwijderd zijn.
//            var current = normalized;

//            while (true)
//            {
//                var slash = current.LastIndexOf('/');

//                if (slash < 0)
//                    break;

//                current = current[..slash];

//                if (_removed.Contains(current))
//                    return true;
//            }
//        }

//        return false;
//    }

//    public Task Write(string path, CancellationToken ct) => Restore(path, ct);

//    public Task Responded(string path, Entry entry, CancellationToken ct)
//    {
//        RespondedEntries[path] = entry;

//        return SaveToDisk(ct);
//    }

//    public string ResolveReadPath(string path)
//    {
//        var normalized = Normalize(path);

//        lock (_lock)
//        {
//            var move = FindMoveByDestination(normalized);

//            if (move is null)
//                return normalized;

//            var (sourcePath, destinationPath) = move.Value;
//            var suffix = GetRelativeSuffix(destinationPath, normalized);

//            return Combine(sourcePath, suffix);
//        }
//    }

//    public string ResolveVisiblePath(string sourcePath)
//    {
//        var normalized = Normalize(sourcePath);

//        lock (_lock)
//        {
//            var move = FindMoveBySource(normalized);

//            if (move is null)
//                return normalized;

//            var (source, destination) = move.Value;
//            var suffix = GetRelativeSuffix(source, normalized);

//            return Combine(destination, suffix);
//        }
//    }

//    public IReadOnlyList<string> GetMovedSourcesForDirectory(string visibleDirectoryPath)
//    {
//        var normalized = Normalize(visibleDirectoryPath);

//        lock (_lock)
//        {
//            return _moves
//                .Where(a => GetParentPath(a.Value) == normalized)
//                .Select(a => a.Key)
//                .ToList();
//        }
//    }

//    public bool HasMovedSourceForVisibleDirectory(string visibleDirectoryPath)
//    {
//        var normalized = Normalize(visibleDirectoryPath);

//        lock (_lock)
//        {
//            return FindMoveByDestination(normalized) is not null;
//        }
//    }

//    private Task Restore(string path, CancellationToken ct)
//    {
//        var normalized = Normalize(path);

//        lock (_lock)
//        {
//            _removed.Remove(normalized);
//            RemoveMovesAtOrBelowDestination(normalized);
//        }

//        return SaveToDisk(ct);
//    }

//    public static string Normalize(string path)
//        => path
//            .Replace('\\', '/')
//            .Trim('/');

//    public async Task Move(string sourcePath, string destinationPath, bool trackSourceAsMoved, CancellationToken ct)
//    {
//        sourcePath = Normalize(sourcePath);
//        destinationPath = Normalize(destinationPath);

//        lock (_lock)
//        {
//            _removed.Add(sourcePath);
//            _removed.Remove(destinationPath);
//            _moves.Remove(sourcePath);
//            RemoveMovesAtOrBelowDestination(destinationPath);

//            if (trackSourceAsMoved &&
//                !string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
//            {
//                _moves[sourcePath] = destinationPath;
//            }
//        }

//        await SaveToDisk(ct);
//    }

//    private (string SourcePath, string DestinationPath)? FindMoveByDestination(string path)
//    {
//        return _moves
//            .Where(a => IsSameOrChild(path, a.Value))
//            .OrderByDescending(a => a.Value.Length)
//            .Select(a => ((string SourcePath, string DestinationPath)?)(a.Key, a.Value))
//            .FirstOrDefault();
//    }

//    private (string SourcePath, string DestinationPath)? FindMoveBySource(string path)
//    {
//        return _moves
//            .Where(a => IsSameOrChild(path, a.Key))
//            .OrderByDescending(a => a.Key.Length)
//            .Select(a => ((string SourcePath, string DestinationPath)?)(a.Key, a.Value))
//            .FirstOrDefault();
//    }

//    private void RemoveMovesAtOrBelowDestination(string path)
//    {
//        var keys = _moves
//            .Where(a => IsSameOrChild(a.Value, path))
//            .Select(a => a.Key)
//            .ToList();

//        foreach (var key in keys)
//            _moves.Remove(key);
//    }

//    private static bool IsSameOrChild(string path, string parent)
//    {
//        if (string.IsNullOrEmpty(parent))
//            return true;

//        return path.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
//               path.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase);
//    }

//    private static string GetRelativeSuffix(string parent, string path)
//    {
//        if (string.IsNullOrEmpty(parent))
//            return path;

//        if (path.Equals(parent, StringComparison.OrdinalIgnoreCase))
//            return string.Empty;

//        return path[(parent.Length + 1)..];
//    }

//    private static string Combine(string parent, string child)
//    {
//        if (string.IsNullOrEmpty(parent))
//            return child;

//        if (string.IsNullOrEmpty(child))
//            return parent;

//        return $"{parent}/{child}";
//    }

//    private static string GetParentPath(string path)
//    {
//        path = Normalize(path);
//        var slash = path.LastIndexOf('/');

//        return slash < 0 ? string.Empty : path[..slash];
//    }

//    private static EntryCollectionState? DeserializeState(string json)
//    {
//        try
//        {
//            return JsonSerializer.Deserialize<EntryCollectionState>(json);
//        }
//        catch (JsonException)
//        {
//            var removedPaths = JsonSerializer.Deserialize<List<string>>(json);

//            return removedPaths is null
//                ? null
//                : new EntryCollectionState { RemovedPaths = removedPaths };
//        }
//    }

//    private sealed class EntryCollectionState
//    {
//        public List<string> RemovedPaths { get; set; } = [];
//        public List<EntryMove> Moves { get; set; } = [];
//    }

//    private sealed class EntryMove
//    {
//        public string SourcePath { get; set; } = string.Empty;
//        public string DestinationPath { get; set; } = string.Empty;
//    }
//}
