using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Janphe
{
    public class Debug
    {
#if DEBUG
        const bool Enable = true;
#else
        const bool Enable = false;
#endif

        public static void Log(string msg)
        {
            if (Enable)
#if GODOT
                Godot.GD.Print(msg);
#else
            Godot.GD.Print(msg);
#endif
        }

        public static void LogWarning(string msg)
        {
            if (Enable)
#if GODOT
                Godot.GD.PushWarning(msg);
#else
            Godot.GD.PushWarning(msg);
#endif
        }
        public static void LogError(string msg)
        {
            if (Enable)
#if GODOT
                Godot.GD.PushError(msg);
#else
            Godot.GD.PushError(msg);
#endif
        }

        public static string UserDataDir => Godot.OS.GetUserDataDir();

        public static void SaveArray<T>(string fileName, T[] d, int limit = int.MaxValue)
        {
            var fileInfo = new FileInfo($"{UserDataDir}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Length && i < limit; ++i)
                streamWriter.WriteLine($"{i} {d[i]}");
            streamWriter.Close();
            streamWriter.Dispose();
        }
        public static void SaveArray<T>(string fileName, List<T> d, int limit = int.MaxValue)
        {
            SaveArray(fileName, d.ToArray(), limit);
        }
        public static void SaveArray<T>(string fileName, IEnumerable<T> d, int limit = int.MaxValue)
        {
            SaveArray(fileName, d.ToArray(), limit);
        }
        public static void SaveArray<T>(string fileName, HashSet<T> d, int limit = int.MaxValue)
        {
            var fileInfo = new FileInfo($"{UserDataDir}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            int i = 0;
            foreach (var a in d)
            {
                streamWriter.WriteLine($"{i++} {a}");
                if (i >= limit)
                    break;
            }
            streamWriter.Close();
            streamWriter.Dispose();
        }

        public static void SaveArray<T>(string fileName, T[][] d, int limit = int.MaxValue)
        {
            var fileInfo = new FileInfo($"{UserDataDir}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Length && i < limit; ++i)
                streamWriter.WriteLine($"{i} {toString(d[i])}");
            streamWriter.Close();
            streamWriter.Dispose();

        }

        public static void SaveArray(string fileName, List<double[]> d, int limit = int.MaxValue)
        {
            var fileInfo = new FileInfo($"{UserDataDir}/{fileName}");
            var streamWriter = fileInfo.CreateText();
            for (var i = 0; i < d.Count && i < limit; ++i)
                streamWriter.WriteLine($"{i} {d[i][0]},{d[i][1]}");
            streamWriter.Close();
            streamWriter.Dispose();
        }

        public static string toString<T>(T[] d)
        {
            if (d == null)
                return null;

            string s = "";
            for (int i = 0; i < d.Length - 1; ++i)
            {
                s += $"{d[i]},";
            }
            s += d[d.Length - 1];
            return s;
        }

    }
}
