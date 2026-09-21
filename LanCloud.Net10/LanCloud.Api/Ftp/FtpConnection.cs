using gAPI.Core.Dtos;
using LanCloud.Api.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LanCloud.Api.Ftp;

public enum TransferType
{
    Ascii,
    Ebcdic,
    Image,
    Local,
}
public enum DataConnectionType
{
    Passive,
    Active,
}

internal class FtpConnection(
    AsyncServiceScope scope,
    ILoggerFactory loggerFactory,
    IFileSystemDirect fileSystem,
    TcpClient client,
    string? certificateFilename = null)
{
    const int FtpBufferSize = 64 * 1024;
    readonly ILogger<FtpConnection> Logger = loggerFactory.CreateLogger<FtpConnection>();
    TcpClient? DataClient;
    TcpListener? PassiveListener;
    NetworkStream? ControlStream;
    StreamReader? ControlReader;
    StreamWriter? ControlWriter;
    TransferType ConnectionType = TransferType.Ascii;
    DataConnectionType DataConnectionType = DataConnectionType.Active;
    IPEndPoint? DataEndpoint;
    X509Certificate? Cert;
    SslStream? SslStream;
    string CurrentPath = "/";
    AuthStateUserDto? CurrentUser;
    string? StoredUserName;
    string? StoredRenameFrom = null;
    HashSet<string> AnomiousAllowedCommands = ["AUTH", "USER", "PASS", "QUIT", "HELP", "NOOP"];
    bool QuitFlag = false;

    public IPEndPoint? RemoteEndPoint => client.Client.RemoteEndPoint as IPEndPoint; // Exposed for monitoring

    public async Task Start(CancellationToken parentCt)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
        var ct = cts.Token;

        ControlStream = client.GetStream();

        ControlReader = new StreamReader(ControlStream);
        ControlWriter = new StreamWriter(ControlStream);
        DataClient = new TcpClient();

        ControlWriter.WriteLine("220 Service Ready.");
        ControlWriter.Flush();

        string? line;
        try
        {
            while (true)
            {
                line = await ControlReader.ReadLineAsync(ct);

                // First do our own cancellation check, the line might have come back because of a cancel
                if (ct.IsCancellationRequested)
                {
                    // No cancel needed if it is already cancelled
                    break;
                }

                // Then check if the connection closed / response is null
                if (line == null)
                {
                    // If so cancel and return
                    await cts.CancelAsync();
                    break;
                }

                // Then log the incomming request
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("FTP Received:  {line}", line);

                // Format the line in commands, command and arguments
                var commands = line.Split(' ');
                var command = commands[0].ToUpperInvariant();
                var arguments = commands.Length > 1 ? line.Substring(commands[0].Length + 1) : null;

                // Reset argument to null if it is a empty/whiteline string
                if (string.IsNullOrWhiteSpace(arguments))
                {
                    arguments = null;
                }

                // Reset RenameFrom cache if this is not the next RenameTo command
                if (command != "RNTO")
                {
                    StoredRenameFrom = null;
                }

                // Authentication toggles
                var commandRequiresAuthentication = AnomiousAllowedCommands.Contains(command) == false;
                var currentUserIsNotAuthenticated = CurrentUser is null;

                var response =
                    commandRequiresAuthentication && currentUserIsNotAuthenticated
                    ? "530 Not logged in"
                    : await HandleCommand(commands, command, arguments, ct);

                // Early return on own cancellation check (maybe parent has called it?)
                if (ct.IsCancellationRequested)
                {
                    // No cancel is needed
                    break;
                }

                // Early return if the connection has closed
                if (client?.Connected != true)
                {
                    await cts.CancelAsync();
                    break;
                }

                // Connection not closed, so we can write the response
                await ControlWriter.WriteLineAsync(response);
                await ControlWriter.FlushAsync(ct);

                // Amd log our response
                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("FTP Responded: {response}", response);

                // Early return / force close connection AFTER sending quit response (221)
                if (QuitFlag)
                {
                    await cts.CancelAsync();
                    break;
                }

                // RESPONSE FOLLOW UP 

                // If the server supports SSL and authentication request has been raised,
                // switch over to SSL connection after sending back the response
                // inside the Auth method we do the same check on CertificateFileName
                // (yes it's kinda hacky, but it follows "the natural way of the loop")
                if (command == "AUTH" && certificateFilename != null)
                {
                    // SSL is supported! Load the SSL certificate
                    var certData = await File.ReadAllBytesAsync(certificateFilename, ct);
                    Cert = X509CertificateLoader.LoadCertificate(certData);

                    // Setup SSL stream with original Control stream
                    SslStream = new SslStream(ControlStream);
                    await SslStream.AuthenticateAsServerAsync(Cert);

                    // Change the Control reader/writer towards the new SSL stream
                    ControlReader = new StreamReader(SslStream);
                    ControlWriter = new StreamWriter(SslStream);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FTP Error");
        }
        finally
        {
            client?.Close();
            DataClient?.Close();
            ControlStream?.Close();
            ControlReader?.Close();
            ControlWriter?.Close();
            cts.Dispose();
            await scope.DisposeAsync();
        }
    }
    private async Task<string> HandleCommand(string[] commands, string command, string? arguments, CancellationToken ct)
    {
        return command switch
        {
            "USER" => User(arguments),
            "PASS" => await Password(arguments, ct),
            "CWD" => await ChangeWorkingDirectory(arguments, ct),
            "CDUP" => await ChangeWorkingDirectory("..", ct),
            "QUIT" => Quit(),
            "REIN" => Rein(),
            "PORT" => Port(arguments),
            "PASV" => Passive(),
            "TYPE" => Type(commands[1], commands.Length == 3 ? commands[2] : null),
            "STRU" => Structure(arguments),
            "MODE" => Mode(arguments),
            "RNFR" => SetRenameFrom(arguments),
            "RNTO" => await SetRenameTo_And_ExecuteRename(arguments, ct),
            "DELE" => await Delete(arguments, ct),
            "RMD" => await RemoveDir(arguments, ct),
            "MKD" => await CreateDir(arguments, ct),
            "PWD" => PrintWorkingDirectory(),
            "RETR" => await Retrieve(arguments, ct),
            "STOR" => Store(arguments),
            "STOU" => StoreUnique(),
            "APPE" => Append(arguments),
            "LIST" => List(arguments ?? CurrentPath),
            "SYST" => "215 UNIX Type: L8",
            "NOOP" => "200 OK",
            "ACCT" => "200 OK",
            "ALLO" => "200 OK",
            "NLST" => "502 Command not implemented",
            "SITE" => "502 Command not implemented",
            "STAT" => "502 Command not implemented",
            "HELP" => "502 Command not implemented",
            "SMNT" => "502 Command not implemented",
            "REST" => "502 Command not implemented",
            "ABOR" => "502 Command not implemented",
            // Extensions defined by rfc 2228
            "AUTH" => Auth(arguments),
            // Extensions defined by rfc 2389
            "FEAT" => FeatureList(),
            "OPTS" => Options(arguments),
            // Extensions defined by rfc 3659
            "MDTM" => await FileModificationTime(arguments, ct),
            "SIZE" => await FileSize(arguments, ct),
            // Extensions defined by rfc 2428
            "EPRT" => EPort(arguments),
            "EPSV" => EPassive(),
            _ => "502 Command not implemented",
        };
    }

    #region FTP Commands

    private string Quit()
    {
        QuitFlag = true;
        return "221 Service closing control connection";
    }


    private string FeatureList()
    {
        if (ControlWriter == null)
            return "502 Command not implemented";
        ControlWriter.WriteLine("211- Extensions supported:");
        ControlWriter.WriteLine(" MDTM");
        ControlWriter.WriteLine(" SIZE");
        return "211 End";
    }

    private string Options(string? arguments)
    {
        return "200 Looks good to me...";
    }

    private string Auth(string? authMode)
    {
        if (certificateFilename != null)
        {
            if (authMode == "TLS")
            {
                return "234 Enabling TLS Connection";
            }
            else
            {
                return "504 Unrecognized AUTH mode";
            }
        }
        else
        {
            return "502 Command not implemented";
        }
    }

    private string User(string? username)
    {
        StoredUserName = username;

        return "331 Username ok, need password";
    }

    private async Task<string> Password(string? password, CancellationToken ct)
    {
        CurrentUser = await fileSystem.AuthenticateUser(StoredUserName, password, ct);

        if (CurrentUser != null)
        {
            return "230 User logged in";
        }
        else
        {
            return "530 Not logged in";
        }
    }

    private string Rein()
    {
        CurrentUser = null;
        StoredUserName = null;
        PassiveListener = null;
        DataClient = null;

        return "220 Service ready for new user";
    }

    private async Task<string> ChangeWorkingDirectory(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        var info = await fileSystem.Get(pathname, ct);
        if (info?.IsDirectory != true)// !FileSystem.DirectoryExists(pathname))
        {
            return $"550 CWD failed. Directory '{pathname}' not found.";
        }

        CurrentPath = pathname;
        return $"250 Changed to directory '{pathname}'";
    }

    private string Port(string? hostPort)
    {
        if (hostPort == null)
            return "504 Command not implemented for that parameter";

        DataConnectionType = DataConnectionType.Active;

        string[] ipAndPort = hostPort.Split(',');

        byte[] ipAddress = new byte[4];
        byte[] port = new byte[2];

        for (int i = 0; i < 4; i++)
        {
            ipAddress[i] = Convert.ToByte(ipAndPort[i]);
        }

        for (int i = 4; i < 6; i++)
        {
            port[i - 4] = Convert.ToByte(ipAndPort[i]);
        }

        if (BitConverter.IsLittleEndian)
            Array.Reverse(port);

        DataEndpoint = new IPEndPoint(new IPAddress(ipAddress), BitConverter.ToInt16(port, 0));

        return "200 Data Connection Established";
    }

    private string EPort(string? hostPort)
    {
        if (hostPort == null)
            return "504 Command not implemented for that parameter";

        DataConnectionType = DataConnectionType.Active;

        char delimiter = hostPort[0];

        string[] rawSplit = hostPort.Split(new char[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);

        char ipType = rawSplit[0][0];

        string ipAddress = rawSplit[1];
        string port = rawSplit[2];

        DataEndpoint = new IPEndPoint(IPAddress.Parse(ipAddress), int.Parse(port));

        return "200 Data Connection Established";
    }

    private string Passive()
    {
        DataConnectionType = DataConnectionType.Passive;

        var localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
        if (localEndPoint == null) throw new Exception("Endpoint null");
        IPAddress localIp = localEndPoint.Address;

        PassiveListener = new TcpListener(localIp, 0);
        PassiveListener.Start();

        IPEndPoint passiveListenerEndpoint = (IPEndPoint)PassiveListener.LocalEndpoint;

        byte[] address = passiveListenerEndpoint.Address.GetAddressBytes();
        short port = (short)passiveListenerEndpoint.Port;

        byte[] portArray = BitConverter.GetBytes(port);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(portArray);

        return string.Format("227 Entering Passive Mode ({0},{1},{2},{3},{4},{5})", address[0], address[1], address[2], address[3], portArray[0], portArray[1]);
    }

    private string EPassive()
    {
        DataConnectionType = DataConnectionType.Passive;

        var localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
        if (localEndPoint == null) throw new Exception("Endpoint null");
        IPAddress localIp = localEndPoint.Address;

        PassiveListener = new TcpListener(localIp, 0);
        PassiveListener.Start();

        IPEndPoint passiveListenerEndpoint = (IPEndPoint)PassiveListener.LocalEndpoint;

        return string.Format("229 Entering Extended Passive Mode (|||{0}|)", passiveListenerEndpoint.Port);
    }

    private string Type(string? typeCode, string? formatControl)
    {
        if (typeCode == null)
            return "504 Command not implemented for that parameter";

        switch (typeCode.ToUpperInvariant())
        {
            case "A":
                ConnectionType = TransferType.Ascii;
                break;
            case "I":
                ConnectionType = TransferType.Image;
                break;
            default:
                return "504 Command not implemented for that parameter";
        }

        if (!string.IsNullOrWhiteSpace(formatControl))
        {
            switch (formatControl.ToUpperInvariant())
            {
                case "N":
                    //FormatControlType = FormatControlType.NonPrint;
                    break;
                default:
                    return "504 Command not implemented for that parameter";
            }
        }

        return string.Format("200 Type set to {0}", ConnectionType);
    }

    private async Task<string> Delete(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info != null)// FileSystem.FileExists(pathname))
            {
                await fileSystem.Delete(pathname, ct);
            }
            else
            {
                return "550 File Not Found";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 File Not Found";
    }

    private async Task<string> RemoveDir(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info != null)// FileSystem.DirectoryExists(pathname))
            {
                await fileSystem.Delete(pathname, ct);
            }
            else
            {
                return "550 Directory Not Found";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 Directory Not Found";
    }

    private async Task<string> CreateDir(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info?.IsDirectory != true)// !FileSystem.DirectoryExists(pathname))
            {
                await fileSystem.CreateDirectory(pathname, ct);
            }
            else
            {
                return "550 Directory already exists";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 Directory Not Found";
    }

    private async Task<string> FileModificationTime(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info != null)// FileSystem.FileExists(pathname))
            {
                return string.Format("213 {0}", info.LastModified.ToString("yyyyMMddHHmmss.fff"));// FileSystem.FileGetLastWriteTime(pathname).ToString("yyyyMMddHHmmss.fff"));
            }
        }

        return "550 File Not Found";
    }

    private async Task<string> FileSize(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info != null) //FileSystem.FileExists(pathname))
            {
                //long length = 0;

                //using (var fs = await FileSystem.OpenRead(pathname))
                //{
                //    if (fs == null)
                //        return "550 File Not Found";

                //    length = fs.Length;
                //}

                return string.Format("213 {0}", info.Size);
            }
        }

        return "550 File Not Found";
    }

    private async Task<string> Retrieve(string? pathname, CancellationToken ct)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var info = await fileSystem.Get(pathname, ct);
            if (info != null) //FileSystem.FileExists(pathname))
            {
                var state = new DataConnectionOperation(RetrieveOperation, pathname);

                SetupDataConnectionOperation(state);

                return string.Format("150 Opening {0} mode data transfer for RETR", DataConnectionType);
            }
        }

        return "550 File Not Found";
    }

    private string Store(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var state = new DataConnectionOperation(StoreOperation, pathname);

            SetupDataConnectionOperation(state);

            return string.Format("150 Opening {0} mode data transfer for STOR", DataConnectionType);
        }

        return "450 Requested file action not taken";
    }

    private string Append(string? pathname)
    {
        //return "450 Requested file action not taken";

        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var state = new DataConnectionOperation(AppendOperation, pathname);

            SetupDataConnectionOperation(state);

            return string.Format("150 Opening {0} mode data transfer for APPE", DataConnectionType);
        }

        return "450 Requested file action not taken";
    }

    private string StoreUnique()
    {
        string pathname = NormalizeFilename(new Guid().ToString());

        var state = new DataConnectionOperation(StoreOperation, pathname);

        SetupDataConnectionOperation(state);

        return string.Format("150 Opening {0} mode data transfer for STOU", DataConnectionType);
    }

    private string PrintWorkingDirectory()
    {
        //string current = CurrentDirectory.Replace(Root, string.Empty).Replace('\\', '/');
        string current = CurrentPath;
        if (current.Length == 0)
        {
            current = "/";
        }

        return string.Format("257 \"{0}\" is current directory.", current); ;
    }

    private string List(string pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            var state = new DataConnectionOperation(ListOperation, pathname);

            SetupDataConnectionOperation(state);

            return string.Format("150 Opening {0} mode data transfer for LIST", DataConnectionType);
        }

        return "450 Requested file action not taken";
    }

    private string Structure(string? structure)
    {
        switch (structure)
        {
            case "F":
                //FileStructureType = FileStructureType.File;
                break;
            case "R":
            case "P":
                return string.Format("504 STRU not implemented for \"{0}\"", structure);
            default:
                return string.Format("501 Parameter {0} not recognized", structure);
        }

        return "200 Command OK";
    }

    private string Mode(string? mode)
    {
        if (mode?.ToUpperInvariant() == "S")
        {
            return "200 OK";
        }
        else
        {
            return "504 Command not implemented for that parameter";
        }
    }

    private string SetRenameFrom(string? arguments)
    {
        StoredRenameFrom = arguments;
        return "350 Requested file action pending further information";
    }
    private Task<string> SetRenameTo_And_ExecuteRename(string? arguments, CancellationToken ct)
    {
        return Rename(StoredRenameFrom, arguments, ct);
    }
    private async Task<string> Rename(string? renameFrom, string? renameTo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(renameFrom) || string.IsNullOrWhiteSpace(renameTo))
        {
            return "450 Requested file action not taken";
        }

        renameFrom = NormalizeFilename(renameFrom);
        renameTo = NormalizeFilename(renameTo);

        if (renameFrom != null && renameTo != null)
        {
            var info = await fileSystem.Get(renameFrom, ct);
            var infoTo = await fileSystem.Get(renameTo, ct);
            if (info?.IsDirectory == false) //FileSystem.FileExists(renameFrom))
            {
                await fileSystem.Move(renameFrom, renameTo, ct);
            }
            else if (infoTo?.IsDirectory == true) // FileSystem.DirectoryExists(renameFrom))
            {
                await fileSystem.Move(renameFrom, renameTo, ct);
            }
            else
            {
                return "450 Requested file action not taken";
            }

            return "250 Requested file action okay, completed";
        }

        return "450 Requested file action not taken";
    }

    private string NormalizeFilename(string? path)
    {
        if (path == null)
        {
            path = string.Empty;
        }

        if (!path.StartsWith("/")) // = bestand zonder directory
        {
            path = CombineWithCurrentPath(path);
        }

        return path;
    }
    private string CombineWithCurrentPath(string? path)
    {
        if (string.IsNullOrEmpty(CurrentPath))
        {
            return "/" + path;
        }
        else
        {
            if (CurrentPath.EndsWith("/"))
            {
                return CurrentPath + path;
            }
            else
            {
                return CurrentPath + "/" + path;
            }
        }
    }

    #endregion

    #region DataConnection Operations

    private void HandleAsyncResult(IAsyncResult result)
    {
        if (DataConnectionType == DataConnectionType.Active)
        {
            DataClient?.EndConnect(result);
        }
        else
        {
            DataClient = PassiveListener?.EndAcceptTcpClient(result);
        }
    }

    private void SetupDataConnectionOperation(DataConnectionOperation state)
    {
        if (DataConnectionType == DataConnectionType.Active && DataEndpoint != null)
        {
            //if ()
            {
                DataClient = new TcpClient(DataEndpoint.AddressFamily);
                DataClient.BeginConnect(DataEndpoint.Address, DataEndpoint.Port, DoDataConnectionOperation, state);
            }
        }
        else if (PassiveListener != null)
        {
            PassiveListener.BeginAcceptTcpClient(DoDataConnectionOperation, state);
        }
    }

    private async void DoDataConnectionOperation(IAsyncResult result)
    {
        HandleAsyncResult(result);

        DataConnectionOperation? op = result.AsyncState as DataConnectionOperation;
        if (op == null) throw new Exception("op is null");

        if (DataClient == null || ControlWriter == null) return;

        string response;
        using (NetworkStream? dataStream = DataClient.GetStream())
        {
            response = await op.Operation(dataStream, op.Arguments, default);
        }

        DataClient.Close();
        DataClient = null;

        ControlWriter.WriteLine(response);
        ControlWriter.Flush();

        //Logger.Info("FTP Responded: " + response);
    }

    private async Task<string> RetrieveOperation(NetworkStream dataStream, string pathname, CancellationToken ct)
    {
        try
        {
            var stopWatch = Stopwatch.StartNew();
            long bytes = 0;

            using (var fs = await fileSystem.OpenRead(pathname, ct))
            {
                if (fs != null)
                    bytes = await CopyStream(fs, dataStream, ct);
            }

            var sec = stopWatch.Elapsed.TotalSeconds;
            var speed = Convert.ToInt64(bytes / sec);
            return $"226 Closing data connection, file transfer successful ({speed}b/sec)";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while retreiving data");
            return $"502 Error while retreiving data";
        }
    }

    private async Task<string> StoreOperation(NetworkStream dataStream, string pathname, CancellationToken ct)
    {
        try
        {
            var stopWatch = Stopwatch.StartNew();
            long bytes = 0;

            await fileSystem.Write(pathname, dataStream, ct);

            //using (var fs = await FileSystem.Write(pathname))
            //{
            //    bytes = CopyStream(dataStream, fs);
            //}

            var sec = stopWatch.Elapsed.TotalSeconds;
            var speed = Convert.ToInt64(bytes / sec);

            //LogEntry logEntry = new LogEntry
            //{
            //    Date = DateTime.Now,
            //    CIP = ClientIP,
            //    CSMethod = "STOR",
            //    CSUsername = UserName,
            //    SCStatus = "226",
            //    CSBytes = bytes.ToString()
            //};

            //Logger.Info(logEntry);

            return $"226 Closing data connection, file transfer successful ({speed}b/sec)";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while storing data");
            return $"502 Error while storing data";
        }
    }

    private async Task<string> AppendOperation(NetworkStream dataStream, string pathname, CancellationToken ct)
    {
        try
        {
            var stopWatch = Stopwatch.StartNew();
            long bytes = 0;

            await fileSystem.Append(pathname, dataStream, ct);

            //using (var fs = FileSystem.FileOpenWriteAppend(pathname))
            //{
            //    bytes = CopyStream(dataStream, fs);
            //}

            var sec = stopWatch.Elapsed.TotalSeconds;
            var speed = Convert.ToInt64(bytes / sec);

            //LogEntry logEntry = new LogEntry
            //{
            //    Date = DateTime.Now,
            //    CIP = ClientIP,
            //    CSMethod = "APPE",
            //    CSUsername = UserName,
            //    SCStatus = "226",
            //    CSBytes = bytes.ToString()
            //};

            //Logger.Info(logEntry);

            return $"226 Closing data connection, file transfer successful ({speed}b/sec)";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error while appending data");
            return $"502 Error while appending data";
        }
    }

    private async Task<string> ListOperation(NetworkStream dataStream, string pathname, CancellationToken ct)
    {
        var dataWriter = new StreamWriter(dataStream, Encoding.ASCII);

        var entries = fileSystem.ListDirectory(pathname, ct);
        await foreach (var entry in entries)
        {
            if (entry.IsDirectory)
            {
                string date = entry.LastModified < DateTime.Now - TimeSpan.FromDays(180)
                    ? entry.LastModified.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
                    : entry.LastModified.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

                string line = $"drwxr-xr-x 1 2003 2003 {FtpBufferSize} {date} {entry.Name}";
                //Logger.Info($"Send: {line}");

                dataWriter.WriteLine(line);
                dataWriter.Flush();

            }
            else
            {
                string date = date = entry.LastModified < DateTime.Now - TimeSpan.FromDays(180)
                    ? entry.LastModified.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
                    : entry.LastModified.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

                string line = $"-rw-r--r-- 1 2003 2003 {entry.Size} {date} {entry.Name}";
                //Logger.Info($"Send: {line}");

                dataWriter.WriteLine(line);
                dataWriter.Flush();

            }
        }

        //var directories = FileSystem.EnumerateDirectories(pathname);

        //foreach (var directory in directories)
        //{
        //    string date = directory.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180)
        //        ? directory.LastWriteTime.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
        //        : directory.LastWriteTime.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

        //    string line = $"drwxr-xr-x 1 2003 2003 {Application.FtpBufferSize} {date} {directory.Name}";
        //    //Logger.Info($"Send: {line}");

        //    dataWriter.WriteLine(line);
        //    dataWriter.Flush();
        //}

        //var files = FileSystem.EnumerateFiles(pathname);

        //foreach (var file in files)
        //{
        //    string date = date = file.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180)
        //        ? file.LastWriteTime.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
        //        : file.LastWriteTime.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

        //    string line = $"-rw-r--r-- 1 2003 2003 {file.Length} {date} {file.Name}";
        //    //Logger.Info($"Send: {line}");

        //    dataWriter.WriteLine(line);
        //    dataWriter.Flush();
        //}

        return "226 Transfer complete";
    }

    #endregion

    #region Copy Stream Implementations

    private Task<long> CopyStream(Stream input, Stream output, CancellationToken ct)
    {
        if (ConnectionType == TransferType.Image)
        {
            return CopyStreamBinary(input, output, FtpBufferSize, ct);
        }
        else
        {
            return CopyStreamAscii(input, output, FtpBufferSize, ct);
        }
    }
    private static async Task<long> CopyStreamBinary(Stream input, Stream output, int bufferSize, CancellationToken ct)
    {
        byte[] buffer = new byte[bufferSize];
        int count = 0;
        long total = 0;

        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await output.WriteAsync(buffer, 0, count, ct);
            total += count;
        }

        return total;
    }

    private static async Task<long> CopyStreamAscii(Stream input, Stream output, int bufferSize, CancellationToken ct)
    {
        var buffer = new char[bufferSize];
        var count = 0;
        var total = 0L;

        using (var rdr = new StreamReader(input, Encoding.ASCII))
        {
            using (var wtr = new StreamWriter(output, Encoding.ASCII))
            {
                while ((count = await rdr.ReadAsync(buffer, ct)) > 0)
                {
                    await wtr.WriteAsync(new Memory<char>(buffer, 0, count), ct);
                    total += count;
                }
            }
        }

        return total;
    }


    #endregion

    record DataConnectionOperation(
        Func<NetworkStream, string, CancellationToken, Task<string>> Operation,
        string Arguments);
}