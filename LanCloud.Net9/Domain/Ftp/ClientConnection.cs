using LanCloud.Domain.Application;
using LanCloud.Interfaces;
using LanCloud.Models.Entities;
using LanCloud.Models.Enums;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LanCloud.Domain.Ftp;

public class ClientConnection : IDisposable
{

    TcpClient? DataClient;
    TcpListener? PassiveListener;
    NetworkStream? ControlStream;
    StreamReader? ControlReader;
    StreamWriter? ControlWriter;
    TransferType ConnectionType = TransferType.Ascii;
    //FormatControlType FormatControlType = FormatControlType.NonPrint;
    DataConnectionType DataConnectionType = DataConnectionType.Active;
    //FileStructureType FileStructureType = FileStructureType.File;

    string? UserName;
    IPEndPoint? DataEndpoint;
    readonly string? CertificateFileName;
    X509Certificate? Cert;
    SslStream? SslStream;
    bool Disposed;
    string CurrentPath = "/";
    User? CurrentUser;
    List<string> _validCommands;

    public ClientConnection(
        TcpClient client,
        LocalApplication application,
        string? certificateFilename = null)
    {

        ControlClient = client;
        Application = application;
        CertificateFileName = certificateFilename;

        var RemoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        Name = RemoteEndPoint?.Address.ToString() ?? "";

        _validCommands = new List<string>();
    }

    string Name { get; }
    TcpClient ControlClient { get; }
    public FileSystem FileSystem => Application.FileSystem;
    public LocalApplication Application { get; }

    ILogger Logger => Application.Logger;

    private string? CheckUser()
    {
        if (CurrentUser == null)
        {
            return "530 Not logged in";
        }

        return null;
    }

    public void HandleClient(object? obj)
    {
        ControlStream = ControlClient.GetStream();

        ControlReader = new StreamReader(ControlStream);
        ControlWriter = new StreamWriter(ControlStream);

        ControlWriter.WriteLine("220 Service Ready.");
        ControlWriter.Flush();

        _validCommands.AddRange(new string[] { "AUTH", "USER", "PASS", "QUIT", "HELP", "NOOP" });

        string? line;

        DataClient = new TcpClient();

        string? renameFrom = null;

        try
        {
            while ((line = ControlReader.ReadLine()) != null)
            {
                string? response = null;

                string[] command = line.Split(' ');

                string cmd = command[0].ToUpperInvariant();
                string? arguments = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;

                if (arguments != null && arguments.Trim().Length == 0)
                {
                    arguments = null;
                }

                //LogEntry logEntry = new LogEntry
                //{
                //    Date = DateTime.Now,
                //    CIP = ClientIP,
                //    CSUriStem = arguments
                //};

                if (!_validCommands.Contains(cmd))
                {
                    response = CheckUser();
                }

                if (cmd != "RNTO")
                {
                    renameFrom = null;
                }

                if (response == null)
                {
                    switch (cmd)
                    {
                        case "USER":
                            response = User(arguments);
                            break;
                        case "PASS":
                            response = Password(arguments);
                            //logEntry.CSUriStem = "******";
                            break;
                        case "CWD":
                            response = ChangeWorkingDirectory(arguments);
                            break;
                        case "CDUP":
                            response = ChangeWorkingDirectory("..");
                            break;
                        case "QUIT":
                            response = "221 Service closing control connection";
                            break;
                        case "REIN":
                            CurrentUser = null;
                            UserName = null;
                            PassiveListener = null;
                            DataClient = null;

                            response = "220 Service ready for new user";
                            break;
                        case "PORT":
                            response = Port(arguments);
                            //logEntry.CPort = DataEndpoint.Port.ToString();
                            break;
                        case "PASV":
                            response = Passive();
                            //logEntry.SPort = ((IPEndPoint)PassiveListener.LocalEndpoint).Port.ToString();
                            break;
                        case "TYPE":
                            response = Type(command[1], command.Length == 3 ? command[2] : null);
                            //logEntry.CSUriStem = command[1];
                            break;
                        case "STRU":
                            response = Structure(arguments);
                            break;
                        case "MODE":
                            response = Mode(arguments);
                            break;
                        case "RNFR":
                            renameFrom = arguments;
                            response = "350 Requested file action pending further information";
                            break;
                        case "RNTO":
                            response = Rename(renameFrom, arguments);
                            break;
                        case "DELE":
                            response = Delete(arguments);
                            break;
                        case "RMD":
                            response = RemoveDir(arguments);
                            break;
                        case "MKD":
                            response = CreateDir(arguments);
                            break;
                        case "PWD":
                            response = PrintWorkingDirectory();
                            break;
                        case "RETR":
                            response = Retrieve(arguments);
                            //logEntry.Date = DateTime.Now;
                            break;
                        case "STOR":
                            response = Store(arguments);
                            //logEntry.Date = DateTime.Now;
                            break;
                        case "STOU":
                            response = StoreUnique();
                            //logEntry.Date = DateTime.Now;
                            break;
                        case "APPE":
                            response = Append(arguments);
                            //logEntry.Date = DateTime.Now;
                            break;
                        case "LIST":
                            response = List(arguments ?? CurrentPath);
                            //logEntry.Date = DateTime.Now;
                            break;
                        case "SYST":
                            response = "215 UNIX Type: L8";
                            break;
                        case "NOOP":
                            response = "200 OK";
                            break;
                        case "ACCT":
                            response = "200 OK";
                            break;
                        case "ALLO":
                            response = "200 OK";
                            break;
                        case "NLST":
                            response = "502 Command not implemented";
                            break;
                        case "SITE":
                            response = "502 Command not implemented";
                            break;
                        case "STAT":
                            response = "502 Command not implemented";
                            break;
                        case "HELP":
                            response = "502 Command not implemented";
                            break;
                        case "SMNT":
                            response = "502 Command not implemented";
                            break;
                        case "REST":
                            response = "502 Command not implemented";
                            break;
                        case "ABOR":
                            response = "502 Command not implemented";
                            break;

                        // Extensions defined by rfc 2228
                        case "AUTH":
                            response = Auth(arguments);
                            break;

                        // Extensions defined by rfc 2389
                        case "FEAT":
                            response = FeatureList();
                            break;
                        case "OPTS":
                            response = Options(arguments);
                            break;

                        // Extensions defined by rfc 3659
                        case "MDTM":
                            response = FileModificationTime(arguments);
                            break;
                        case "SIZE":
                            response = FileSize(arguments);
                            break;

                        // Extensions defined by rfc 2428
                        case "EPRT":
                            response = EPort(arguments);
                            //logEntry.CPort = DataEndpoint.Port.ToString();
                            break;
                        case "EPSV":
                            response = EPassive();
                            //logEntry.SPort = ((IPEndPoint)PassiveListener.LocalEndpoint).Port.ToString();
                            break;

                        default:
                            response = "502 Command not implemented";
                            break;
                    }
                }

                //logEntry.CSMethod = cmd;
                //logEntry.CSUsername = UserName;
                //logEntry.SCStatus = response.Substring(0, response.IndexOf(' '));

                //Logger.Info(logEntry);

                if (ControlClient == null || !ControlClient.Connected)
                {
                    break;
                }
                else
                {
                    ControlWriter.WriteLine(response);
                    ControlWriter.Flush();

                    Logger.Info("FTP Received:  " + line);
                    Logger.Info("FTP Responded: " + response);

                    if (response.StartsWith("221"))
                    {
                        break;
                    }

                    if (cmd == "AUTH" && CertificateFileName != null)
                    {
                        var certData = System.IO.File.ReadAllBytes(CertificateFileName);
                        Cert = X509CertificateLoader.LoadCertificate(certData);

                        SslStream = new SslStream(ControlStream);

                        SslStream.AuthenticateAsServer(Cert);

                        ControlReader = new StreamReader(SslStream);
                        ControlWriter = new StreamWriter(SslStream);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        Dispose();
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

    #region FTP Commands

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
        if (CertificateFileName != null)
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
        UserName = username;

        return "331 Username ok, need password";
    }

    private string Password(string? password)
    {
        CurrentUser = FileSystem.ValidateUser(UserName, password);

        if (CurrentUser != null)
        {
            return "230 User logged in";
        }
        else
        {
            return "530 Not logged in";
        }
    }

    private string ChangeWorkingDirectory(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (!FileSystem.DirectoryExists(pathname))
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

        var localEndPoint = ControlClient.Client.LocalEndPoint as IPEndPoint;
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

        var localEndPoint = ControlClient.Client.LocalEndPoint as IPEndPoint;
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

    private string Delete(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (FileSystem.FileExists(pathname))
            {
                FileSystem.FileDelete(pathname);
            }
            else
            {
                return "550 File Not Found";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 File Not Found";
    }

    private string RemoveDir(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (FileSystem.DirectoryExists(pathname))
            {
                FileSystem.DeleteDirectory(pathname);
            }
            else
            {
                return "550 Directory Not Found";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 Directory Not Found";
    }

    private string CreateDir(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (!FileSystem.DirectoryExists(pathname))
            {
                FileSystem.CreateDirectory(pathname);
            }
            else
            {
                return "550 Directory already exists";
            }

            return "250 Requested file action okay, completed";
        }

        return "550 Directory Not Found";
    }

    private string FileModificationTime(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (FileSystem.FileExists(pathname))
            {
                return string.Format("213 {0}", FileSystem.FileGetLastWriteTime(pathname).ToString("yyyyMMddHHmmss.fff"));
            }
        }

        return "550 File Not Found";
    }

    private string FileSize(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (FileSystem.FileExists(pathname))
            {
                long length = 0;

                using (var fs = FileSystem.FileOpenRead(pathname))
                {
                    if (fs == null)
                        return "550 File Not Found";

                    length = fs.Length;
                }

                return string.Format("213 {0}", length);
            }
        }

        return "550 File Not Found";
    }

    private string Retrieve(string? pathname)
    {
        pathname = NormalizeFilename(pathname);

        if (pathname != null)
        {
            if (FileSystem.FileExists(pathname))
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

    private string Rename(string? renameFrom, string? renameTo)
    {
        if (string.IsNullOrWhiteSpace(renameFrom) || string.IsNullOrWhiteSpace(renameTo))
        {
            return "450 Requested file action not taken";
        }

        renameFrom = NormalizeFilename(renameFrom);
        renameTo = NormalizeFilename(renameTo);

        if (renameFrom != null && renameTo != null)
        {
            if (FileSystem.FileExists(renameFrom))
            {
                FileSystem.FileMove(renameFrom, renameTo);
            }
            else if (FileSystem.DirectoryExists(renameFrom))
            {
                FileSystem.DirectoryMove(renameFrom, renameTo);
            }
            else
            {
                return "450 Requested file action not taken";
            }

            return "250 Requested file action okay, completed";
        }

        return "450 Requested file action not taken";
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
            DataClient = new TcpClient(DataEndpoint.AddressFamily);
            DataClient.BeginConnect(DataEndpoint.Address, DataEndpoint.Port, DoDataConnectionOperation, state);
        }
        else if (PassiveListener != null)
        {
            PassiveListener.BeginAcceptTcpClient(DoDataConnectionOperation, state);
        }
    }

    private void DoDataConnectionOperation(IAsyncResult result)
    {
        if (DataClient == null || ControlWriter == null) return;

        HandleAsyncResult(result);

        DataConnectionOperation? op = result.AsyncState as DataConnectionOperation;
        if (op == null) throw new Exception("op is null");

        string response;

        using (NetworkStream dataStream = DataClient.GetStream())
        {
            response = op.Operation(dataStream, op.Arguments);
        }

        DataClient.Close();
        DataClient = null;

        ControlWriter.WriteLine(response);
        ControlWriter.Flush();

        //Logger.Info("FTP Responded: " + response);
    }

    private string RetrieveOperation(NetworkStream dataStream, string pathname)
    {
        //try
        //{
        var stopWatch = Stopwatch.StartNew();
        long bytes = 0;

        using (var fs = FileSystem.FileOpenRead(pathname))
        {
            bytes = CopyStream(fs, dataStream);
        }

        var sec = stopWatch.Elapsed.TotalSeconds;
        var speed = Convert.ToInt64(bytes / sec);
        return $"226 Closing data connection, file transfer successful ({speed}b/sec)";
        //}
        //catch (Exception ex)
        //{
        //    Logger.Error(ex);
        //    return $"502 Error while retreiving data";
        //}
    }

    private string StoreOperation(NetworkStream dataStream, string pathname)
    {
        //try
        //{
        var stopWatch = Stopwatch.StartNew();
        long bytes = 0;

        using (var fs = FileSystem.FileOpenWriteCreate(pathname))
        {
            bytes = CopyStream(dataStream, fs);
        }

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
        //}
        //catch (Exception ex)
        //{
        //    Logger.Error(ex);
        //    return $"502 Error while storing data";
        //}
    }

    private string AppendOperation(NetworkStream dataStream, string pathname)
    {
        //try
        //{
        var stopWatch = Stopwatch.StartNew();
        long bytes = 0;

        using (var fs = FileSystem.FileOpenWriteAppend(pathname))
        {
            bytes = CopyStream(dataStream, fs);
        }

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
        //}
        //catch (Exception ex)
        //{
        //    Logger.Error(ex);
        //    return $"502 Error while appending data";
        //}
    }

    private string ListOperation(NetworkStream dataStream, string pathname)
    {
        var dataWriter = new StreamWriter(dataStream, Encoding.ASCII);

        var directories = FileSystem.EnumerateDirectories(pathname);

        foreach (var directory in directories)
        {
            string date = directory.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180)
                ? directory.LastWriteTime.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
                : directory.LastWriteTime.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

            string line = $"drwxr-xr-x 1 2003 2003 {Application.FtpBufferSize} {date} {directory.Name}";
            Logger.Info($"Send: {line}");

            dataWriter.WriteLine(line);
            dataWriter.Flush();
        }

        var files = FileSystem.EnumerateFiles(pathname);

        foreach (var file in files)
        {
            string date = date = file.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180)
                ? file.LastWriteTime.ToString("MMM d  yyyy", CultureInfo.InvariantCulture)
                : file.LastWriteTime.ToString("MMM d HH:mm", CultureInfo.InvariantCulture);

            string line = $"-rw-r--r-- 1 2003 2003 {file.Length} {date} {file.Name}";
            Logger.Info($"Send: {line}");

            dataWriter.WriteLine(line);
            dataWriter.Flush();
        }

        return "226 Transfer complete";
    }

    #endregion

    #region Copy Stream Implementations

    private static long CopyStream(Stream input, Stream output, int bufferSize)
    {
        byte[] buffer = new byte[bufferSize];
        int count = 0;
        long total = 0;

        while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, count);
            total += count;
        }

        return total;
    }

    private static long CopyStreamAscii(Stream input, Stream output, int bufferSize)
    {
        char[] buffer = new char[bufferSize];
        int count = 0;
        long total = 0;

        using (var rdr = new StreamReader(input, Encoding.ASCII))
        {
            using (var wtr = new StreamWriter(output, Encoding.ASCII))
            {
                while ((count = rdr.Read(buffer, 0, buffer.Length)) > 0)
                {
                    wtr.Write(buffer, 0, count);
                    total += count;
                }
            }
        }

        return total;
    }

    private long CopyStream(Stream input, Stream output)
    {
        var limitedStream = output; // new RateLimitingStream(output, 131072, 0.5);

        if (ConnectionType == TransferType.Image)
        {
            return CopyStream(input, limitedStream, Application.FtpBufferSize);
        }
        else
        {
            return CopyStreamAscii(input, limitedStream, Application.FtpBufferSize);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                if (ControlClient != null)
                {
                    ControlClient.Close();
                }

                if (DataClient != null)
                {
                    DataClient.Close();
                }

                if (ControlStream != null)
                {
                    ControlStream.Close();
                }

                if (ControlReader != null)
                {
                    ControlReader.Close();
                }

                if (ControlWriter != null)
                {
                    ControlWriter.Close();
                }
            }
        }

        Disposed = true;
    }

    #endregion

    class DataConnectionOperation(Func<NetworkStream, string, string> operation, string arguments)
    {
        public Func<NetworkStream, string, string> Operation { get; set; } = operation;
        public string Arguments { get; set; } = arguments;
    }
}