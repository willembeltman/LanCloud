//using System;
//using System.Collections.Concurrent;
//using System.IO;
//using System.Xml.Linq;

//namespace LanCloud.Logger
//{
//    public class LogManager
//    {
//        static readonly ConcurrentQueue<ILogger> Loggers = new ConcurrentQueue<ILogger>();
//        public static ILogger GetLogger(Type type)
//        {
//            var filename = $"{type.Name}.txt";
//            var fullname = Path.Combine(Environment.CurrentDirectory, filename);
//            var logger = new Logger(fullname);
//            Loggers.Enqueue(logger);
//            return logger;
//        }
//        public static ILogger GetLogger(Type type, string name)
//        {
//            var filename = $"{type.Name} {name}.txt";
//            var fullname = Path.Combine(Environment.CurrentDirectory, filename);
//            var logger = new Logger(fullname);
//            Loggers.Enqueue(logger);
//            return logger;
//        }
//        public static void DisposeLoggers()
//        {
//            while(Loggers.TryDequeue(out var logger))
//            {
//                logger.Dispose();
//            }
//        }
//    }
//}