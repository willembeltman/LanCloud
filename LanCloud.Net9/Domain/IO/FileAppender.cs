using LanCloud.Domain.Local;
using LanCloud.Interfaces;

namespace LanCloud.Domain.IO;

public class FileAppender : Stream
{
    public FileAppender(LocalFile virtualFileInfo)
    {
        VirtualFileInfo = virtualFileInfo;
    }
    public LocalFile VirtualFileInfo { get; }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;

    public override long Position { get; set; }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
    public override void Flush()
    {
        throw new NotImplementedException();
    }
    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }
    public new void Dispose()
    {
        throw new NotImplementedException();
    }

    #region Not implemented

    public override long Length => throw new NotImplementedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    #endregion
}
