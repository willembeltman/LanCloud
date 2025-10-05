//using System;
//using DokanNet.Logging;

//namespace LanCloud.Domain.VirtualDrive
//{
//    internal sealed class DokanLoggerAdapter : ILogger
//    {
//        private readonly ILogger logger;

//        public DokanLoggerAdapter(ILogger logger)
//        {
//            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public bool DebugEnabled => logger.DebugEnabled;

//        public void Debug(string message, params object[] args)
//        {
//            WriteInfo("DEBUG", message, args);
//        }

//        public void Info(string message, params object[] args)
//        {
//            WriteInfo("INFO", message, args);
//        }

//        public void Warn(string message, params object[] args)
//        {
//            WriteInfo("WARN", message, args);
//        }

//        public void Error(string message, params object[] args)
//        {
//            WriteError("ERROR", message, args);
//        }

//        public void Fatal(string message, params object[] args)
//        {
//            WriteError("FATAL", message, args);
//        }

//        private void WriteInfo(string level, string message, params object[] args)
//        {
//            var formatted = FormatMessage(message, args);
//            logger.Info($"Dokan[{level}] {formatted}");
//        }

//        private void WriteError(string level, string message, params object[] args)
//        {
//            var formatted = FormatMessage(message, args);
//            logger.Error($"Dokan[{level}] {formatted}");
//        }

//        private static string FormatMessage(string message, object[] args)
//        {
//            if (string.IsNullOrEmpty(message))
//            {
//                return string.Empty;
//            }

//            if (args == null || args.Length == 0)
//            {
//                return message;
//            }

//            try
//            {
//                return string.Format(message, args);
//            }
//            catch (FormatException)
//            {
//                return message + " " + string.Join(", ", args);
//            }
//        }
//    }
//}
