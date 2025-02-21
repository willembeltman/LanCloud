using System.IO;
using System.Linq;

namespace LanCloud.Domain.FileRef
{
    public static class PathTranslator
    {
        public static string TranslateDirectoryPathToFullName(DirectoryInfo rootInfo, string directoryPath)
        {
            if (directoryPath == null || directoryPath == "" || directoryPath == "//")
                directoryPath = "/";

            directoryPath = directoryPath.Replace("/", "\\");
            return rootInfo.FullName + directoryPath;
        }
        public static string TranslateDirectoryFullNameToPath(DirectoryInfo rootInfo, DirectoryInfo dirInfo)
        {
            var pathFullname = dirInfo.FullName.Substring(rootInfo.FullName.Length);
            var path = pathFullname.Replace("\\", "/");
            return path;
        }

        public static string TranslatePathToFullName(DirectoryInfo rootInfo, string path)
        {
            if (path == null || path == "" || path == "//")
                path = "/";

            path = path.Replace("/", "\\");
            return rootInfo.FullName + path + ".fileref";
        }
        public static string TranslateFullnameToPath(DirectoryInfo rootInfo, FileInfo fileInfo)
        {
            var pathFullnameWithExtention = fileInfo.FullName.Substring(rootInfo.FullName.Length);
            var pathFullname = pathFullnameWithExtention.Substring(0, pathFullnameWithExtention.Length - ".fileref".Length);
            var path = pathFullname.Replace("\\", "/");
            return path;
        }

        public static string TranslatePathToExtention(string path)
        {
            if (path == null) return null;

            var name = TranslatePathToName(path);
            var exts = name.Split('.');
            var ext = exts.Last();
            return ext;
        }

        public static string TranslatePathToName(string path)
        {
            if (path == null) return null;

            var dirs = path.Split('/');
            var name = dirs.Last();
            return name;
        }
    }
}