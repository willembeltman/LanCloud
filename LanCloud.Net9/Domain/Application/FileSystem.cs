using LanCloud.Domain.Drive;
using LanCloud.Domain.FileRef;
using LanCloud.Interfaces;
using LanCloud.Models.Entities;

namespace LanCloud.Domain.Application;

public class FileSystem
{
    private readonly LocalApplication Application;
    private readonly ILogger Logger;

    public FileSystem(
        LocalApplication application,
        ILogger logger)
    {
        Application = application;
        Logger = logger;
    }

    public User ValidateUser(string? userName, string? password)
        => Application.Authentication.ValidateUser(userName, password);

    public IFileRefDirectory[] EnumerateDirectories(string path)
        => new LocalFileRefDirectory(Application, path).GetDirectories();
    public IFileRef[] EnumerateFiles(string path)
        => new LocalFileRefDirectory(Application, path).GetFiles();

    public void CreateDirectory(string path)
        => new LocalFileRefDirectory(Application, path).Create();
    public void DeleteDirectory(string path)
        => new LocalFileRefDirectory(Application, path).Delete();
    public bool DirectoryExists(string path)
        => new LocalFileRefDirectory(Application, path).Exists;
    public void DirectoryMove(string renameFrom, string renameTo)
    {
        var from = new LocalFileRefDirectory(Application, renameFrom);
        from.MoveTo(renameTo);
    }

    public bool FileExists(string path)
        => new LocalFileRef(Application, path).Exists;
    public void FileDelete(string path)
        => new LocalFileRef(Application, path).Delete();
    public void FileMove(string renameFrom, string renameTo)
    {
        var from = new LocalFileRef(Application, renameFrom);
        from.MoveTo(renameTo);
    }
    public DateTime FileGetLastWriteTime(string path)
        => new LocalFileRef(Application, path).LastWriteTime;

    public Stream FileOpenRead(string path)
        => new LocalFileRef(Application, path).OpenRead();
    public Stream FileOpenWriteCreate(string path)
        => new LocalFileRef(Application, path).Create();
    public Stream FileOpenWriteAppend(string path)
        => new LocalFileRef(Application, path).OpenAppend();

    

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return "/";
        }

        path = path.Replace('\\', '/');
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        var normalizedPath = path.TrimEnd('/');
        return normalizedPath;
    }
    private DirectoryInfo ResolveDirectoryInfo(string path)
    {
        path = NormalizePath(path);
        var fullName = PathTranslator.TranslateDirectoryPathToFullName(Application.RealRoot, path);
        var dirInfo = new DirectoryInfo(fullName);
        return dirInfo;
    }
    private FileInfo ResolveFileInfo(string path)
    {
        path = NormalizePath(path);
        var fullName = PathTranslator.TranslatePathToFullName(Application.RealRoot, path);
        var fileInfo = new FileInfo(fullName);
        return fileInfo;
    }
    
    
    public DriveFileSystemEntry? GetDirectory(string path)
    {
        var directoryInfo = ResolveDirectoryInfo(path);
        if (!directoryInfo.Exists)
        {
            return null;
        }

        var normalized = NormalizePath(path);
        var entry = new DriveFileSystemEntry(
            normalized,
            "",
            isDirectory: true,
            length: null,
            creationTime: directoryInfo.CreationTime,
            lastAccessTime: directoryInfo.LastAccessTime,
            lastWriteTime: directoryInfo.LastWriteTime,
            attributes: FileAttributes.Directory);
        return entry;
    }
    public DriveFileSystemEntry? GetFile(string path)
    {
        var file = new LocalFileRef(Application, NormalizePath(path));
        if (!file.Exists || file.Metadata == null)
        {
            return null;
        }

        var entry = new DriveFileSystemEntry(
            file.Path,
            file.Name,
            isDirectory: false,
            length: file.Length,
            creationTime: file.RealInfo.CreationTime,
            lastAccessTime: file.RealInfo.LastAccessTime,
            lastWriteTime: file.RealInfo.LastWriteTime,
            attributes: FileAttributes.Archive);
        return entry;
    }
    public IEnumerable<DriveFileSystemEntry> EnumerateDirectory(string path)
    {
        var directory = new LocalFileRefDirectory(Application, NormalizePath(path));
        if (!directory.Exists)
        {
            return Enumerable.Empty<DriveFileSystemEntry>();
        }

        var directories = directory.GetDirectories()
            .Select(dir => new DriveFileSystemEntry(
                dir.Path,
                dir.Name,
                isDirectory: true,
                length: null,
                creationTime: dir.LastWriteTime,
                lastAccessTime: dir.LastWriteTime,
                lastWriteTime: dir.LastWriteTime,
                attributes: FileAttributes.Directory));

        var files = directory.GetFiles()
            .Where(file => file.Metadata != null)
            .Select(file => new DriveFileSystemEntry(
                file.Path,
                file.Name,
                isDirectory: false,
                length: file.Length,
                creationTime: file.RealInfo.CreationTime,
                lastAccessTime: file.RealInfo.LastAccessTime,
                lastWriteTime: file.RealInfo.LastWriteTime,
                attributes: FileAttributes.Archive));

        var response = directories.Concat(files);
        return response;
    }
    
    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        var directory = new LocalFileRefDirectory(Application, NormalizePath(sourcePath));
        if (!directory.Exists)
        {
            return;
        }

        directory.MoveTo(NormalizePath(destinationPath));
    }
    public void MoveFile(string sourcePath, string destinationPath)
    {
        var file = new LocalFileRef(Application, NormalizePath(sourcePath));
        if (!file.Exists)
        {
            return;
        }

        file.MoveTo(NormalizePath(destinationPath));
    }
    public Stream OpenRead(string path)
    {
        var file = new LocalFileRef(Application, NormalizePath(path));
        return file.OpenRead() ?? throw new IOException($"File {path} could not be opened for reading.");
    }
    public Stream? OpenWrite(string path, FileMode mode)
    {
        var normalized = NormalizePath(path);
        var file = new LocalFileRef(Application, normalized);

        switch (mode)
        {
            case FileMode.Create:
            case FileMode.CreateNew:
            case FileMode.Truncate:
                return file.Create();
            case FileMode.Append:
                return file.OpenAppend();
            default:
                throw new NotSupportedException($"FileMode {mode} is not supported.");
        }
    }
    public void DeleteFile(string path)
    {
        var file = new LocalFileRef(Application, NormalizePath(path));
        if (file.Exists)
        {
            file.Delete();
        }
    }
    public void SetDirectoryTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
    {
        var info = ResolveDirectoryInfo(path);
        if (!info.Exists)
        {
            return;
        }

        if (creationTime.HasValue)
        {
            info.CreationTime = creationTime.Value;
        }
        if (lastAccessTime.HasValue)
        {
            info.LastAccessTime = lastAccessTime.Value;
        }
        if (lastWriteTime.HasValue)
        {
            info.LastWriteTime = lastWriteTime.Value;
        }
    }
    public void SetFileTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
    {
        var info = ResolveFileInfo(path);
        if (!info.Exists)
        {
            return;
        }

        if (creationTime.HasValue)
        {
            info.CreationTime = creationTime.Value;
        }
        if (lastAccessTime.HasValue)
        {
            info.LastAccessTime = lastAccessTime.Value;
        }
        if (lastWriteTime.HasValue)
        {
            info.LastWriteTime = lastWriteTime.Value;
        }
    }
    public void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes)
    {
        var rootPath = Application.RealRoot.FullName;
        var drive = new DriveInfo(Path.GetPathRoot(rootPath)!);
        freeBytesAvailable = drive.AvailableFreeSpace;
        totalNumberOfBytes = drive.TotalSize;
        totalNumberOfFreeBytes = drive.TotalFreeSpace;
    }

    //internal Stream OpenWrite(string path, FileMode createNew)
    //{
    //    throw new NotImplementedException();
    //}

    //internal Stream OpenRead(string path)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void DeleteFile(string path)
    //{
    //    throw new NotImplementedException();
    //}

    //internal DriveFileSystemEntry? GetFile(string path)
    //{
    //    throw new NotImplementedException();
    //}

    //internal DriveFileSystemEntry? GetDirectory(string path)
    //{
    //    throw new NotImplementedException();
    //}

    //internal IEnumerable<DriveFileSystemEntry> EnumerateDirectory(string path)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void SetFileTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void SetDirectoryTimestamps(string path, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void MoveDirectory(string source, string destination)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void MoveFile(string source, string destination)
    //{
    //    throw new NotImplementedException();
    //}

    //internal void GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes)
    //{
    //    throw new NotImplementedException();
    //}




    //public ILogger Logger { get; }
    //public ApplicationConfig Config => Application.Config;
    //public DokanStatus MountStatus => DriveServer.MountStatus;
    //public bool IsRunning => DriveServer.MountStatus == DokanNet.DokanStatus.Success;
    //public bool DirectoryExists(string path)
    //{
    //    var dir = ResolveDirectoryInfo(path);
    //    var exist = dir.Exists;
    //    return exist;
    //}

    //public bool FileExists(string path)
    //{
    //    var file = new LocalFileRef(Application, NormalizePath(path));
    //    var exist = file.Exists;
    //    return exist;
    //}
    //public void CreateDirectory(string path)
    //{
    //    var directory = new LocalFileRefDirectory(Application, NormalizePath(path));
    //    if (!directory.Exists)
    //    {
    //        directory.Create();
    //    }
    //}
    //public void DeleteDirectory(string path)
    //{
    //    var directory = new LocalFileRefDirectory(Application, NormalizePath(path));
    //    if (directory.Exists)
    //    {
    //        directory.Delete();
    //    }
    //}
}