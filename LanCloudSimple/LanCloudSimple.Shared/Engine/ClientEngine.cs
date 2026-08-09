using LanCloudSimple.Shared.Enums;
using LanCloudSimple.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LanCloudSimple.Shared.Engine;

public class ClientEngine
{
    private readonly ILogger _logger;
    private readonly List<string> _scanDirectories;
    private readonly ConcurrentDictionary<string, CloudFileDto> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FileSystemWatcher> _watchers = [];

    // Matches dates like 2026-08-02, 2026_08_02, 20260802, etc.
    private static readonly Regex DateRegex = new(
        @"(?<!\d)(?<year>19\d{2}|20\d{2})[-_./]?(?<month>0[1-9]|1[0-2])[-_./]?(?<day>0[1-9]|[12]\d|3[01])(?!\d)",
        RegexOptions.Compiled);

    public event Action<FileUpdateInfo>? OnFileUpdated;

    public ClientEngine(List<string> scanDirectories, ILogger logger)
    {
        _scanDirectories = scanDirectories;
        _logger = logger;
    }

    public List<CloudFileDto> GetIndex() => [.. _index.Values];

    public void Start()
    {
        _logger.LogInformation("Starting cloud engine indexing and file system watchers...");
        foreach (var dir in _scanDirectories)
        {
            if (!Directory.Exists(dir))
            {
                _logger.LogWarning("Scan directory does not exist, skipping: {dir}", dir);
                continue;
            }

            IndexDirectory(dir);
            SetupWatcher(dir);
        }
    }

    public void Stop()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _logger.LogInformation("Cloud engine stopped.");
    }

    private void IndexDirectory(string dir)
    {
        _logger.LogInformation("Indexing directory: {dir}", dir);
        try
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                var dto = CreateFileDto(dir, file);
                if (dto != null)
                    _index[dto.Path] = dto;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing directory: {dir}", dir);
        }
    }

    private void SetupWatcher(string dir)
    {
        try
        {
            var watcher = new FileSystemWatcher(dir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime
            };

            watcher.Created += (_, e) => OnFileChanged(dir, e.FullPath, FileUpdateType.Added);
            watcher.Changed += (_, e) => OnFileChanged(dir, e.FullPath, FileUpdateType.Updated);
            watcher.Deleted += (_, e) => OnFileChanged(dir, e.FullPath, FileUpdateType.Deleted);
            watcher.Renamed += (_, e) =>
            {
                OnFileChanged(dir, e.OldFullPath, FileUpdateType.Deleted);
                OnFileChanged(dir, e.FullPath, FileUpdateType.Added);
            };

            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
            _logger.LogInformation("Watching directory: {dir}", dir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up file system watcher for: {dir}", dir);
        }
    }

    private void OnFileChanged(string rootDir, string fullPath, FileUpdateType updateType)
    {
        if (Directory.Exists(fullPath))
        {
            // Is (Empty) Directory
            return;
        }

        var relativePath = GetRelativePath(rootDir, fullPath);

        if (updateType == FileUpdateType.Deleted)
        {
            if (_index.TryRemove(relativePath, out var removed))
            {
                _logger.LogInformation("File deleted from index: {path}", relativePath);
                OnFileUpdated?.Invoke(new FileUpdateInfo { UpdateType = FileUpdateType.Deleted, File = removed });
            }
            return;
        }

        // Added or Updated — retry briefly if file is still being written
        CloudFileDto? dto = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!File.Exists(fullPath)) return;
                dto = CreateFileDto(rootDir, fullPath);
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
        }

        if (dto != null)
        {
            _index[dto.Path] = dto;
            _logger.LogInformation("File {action}: {path} (Date: {date})", updateType, dto.Path, dto.MediaDate);
            OnFileUpdated?.Invoke(new FileUpdateInfo { UpdateType = updateType, File = dto });
        }
    }

    /// <summary>
    /// Resolves a client-relative path (e.g. "ShareName/sub/file.jpg") to an absolute physical path.
    /// Returns null if path traversal is detected or the file does not exist.
    /// </summary>
    public string? ResolvePhysicalPath(string requestPath)
    {
        foreach (var dir in _scanDirectories)
        {
            var rootName = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(rootName)) continue;

            if (requestPath.StartsWith(rootName + "/", StringComparison.OrdinalIgnoreCase) ||
                requestPath.StartsWith(rootName + "\\", StringComparison.OrdinalIgnoreCase))
            {
                var relativePart = requestPath[rootName.Length..].TrimStart('/', '\\');
                var physicalPath = Path.GetFullPath(Path.Combine(dir, relativePart));
                var dirFullPath = Path.GetFullPath(dir);

                if (physicalPath.StartsWith(dirFullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(physicalPath))
                    return physicalPath;
            }
        }
        return null;
    }

    private CloudFileDto? CreateFileDto(string rootDir, string fullPath)
    {
        try
        {
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists) return null;

            return new CloudFileDto
            {
                Path = GetRelativePath(rootDir, fullPath),
                Size = fileInfo.Length,
                LastWriteTime = fileInfo.LastWriteTimeUtc,
                CreationTime = fileInfo.CreationTimeUtc,
                MediaDate = DetermineMediaDate(fileInfo)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to read file info for {path}: {msg}", fullPath, ex.Message);
            return null;
        }
    }

    private string GetRelativePath(string rootDir, string fullPath)
    {
        var parentDir = Path.GetDirectoryName(rootDir) ?? rootDir;
        return Path.GetRelativePath(parentDir, fullPath).Replace('\\', '/');
    }

    public static DateTime DetermineMediaDate(FileInfo fileInfo)
    {
        var match = DateRegex.Match(fileInfo.Name);
        if (match.Success)
        {
            try
            {
                return new DateTime(
                    int.Parse(match.Groups["year"].Value),
                    int.Parse(match.Groups["month"].Value),
                    int.Parse(match.Groups["day"].Value),
                    0, 0, 0, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException) { }
        }

        // Fallback: earliest of creation time and last write time
        return fileInfo.CreationTimeUtc < fileInfo.LastWriteTimeUtc
            ? fileInfo.CreationTimeUtc
            : fileInfo.LastWriteTimeUtc;
    }
}
