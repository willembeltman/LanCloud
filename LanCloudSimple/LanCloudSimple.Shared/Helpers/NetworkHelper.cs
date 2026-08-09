using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LanCloudSimple.Shared.Helpers;

public static class NetworkHelper
{
    public static async Task SendFrameAsync(Stream stream, byte[] data, CancellationToken cancellationToken = default)
    {
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(lengthPrefix);
        }
        await stream.WriteAsync(lengthPrefix, 0, 4, cancellationToken);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<byte[]> ReceiveFrameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] lengthPrefix = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int read = await stream.ReadAsync(lengthPrefix, bytesRead, 4 - bytesRead, cancellationToken);
            if (read == 0)
            {
                throw new IOException("Connection closed while reading frame length.");
            }
            bytesRead += read;
        }

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(lengthPrefix);
        }
        int length = BitConverter.ToInt32(lengthPrefix, 0);
        if (length < 0 || length > 100 * 1024 * 1024) // 100 MB safety limit
        {
            throw new InvalidDataException($"Invalid frame length: {length}");
        }

        byte[] buffer = new byte[length];
        bytesRead = 0;
        while (bytesRead < length)
        {
            int read = await stream.ReadAsync(buffer, bytesRead, length - bytesRead, cancellationToken);
            if (read == 0)
            {
                throw new IOException("Connection closed while reading frame payload.");
            }
            bytesRead += read;
        }

        return buffer;
    }

    public static async Task SendJsonAsync<T>(Stream stream, T obj, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(obj);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await SendFrameAsync(stream, data, cancellationToken);
    }

    public static async Task<T?> ReceiveJsonAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] data = await ReceiveFrameAsync(stream, cancellationToken);
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json);
    }
}
