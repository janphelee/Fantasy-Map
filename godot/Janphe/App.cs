using System;
using System.IO;
using System.Runtime.InteropServices;
using Godot;

namespace Janphe
{
    public class App
    {
        public static byte[] LoadData(string path)
        {
            var file = new Godot.File();
            file.Open($"res://public/{path}", Godot.File.ModeFlags.Read);

            var buffer = file.GetBuffer((int)file.GetLen());
            file.Close();

            return buffer;
        }

        public static void LoadRes(string path, Action<Stream> callback)
        {
            var bytes = LoadData(path);
            var stream = new MemoryStream(bytes);
            callback?.Invoke(stream);

            stream.Dispose();
        }

        public static void LoadRes(string path, Action<IntPtr> callback)
        {
            var bytes = LoadData(path);

            var unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            // Call unmanaged code
            callback?.Invoke(unmanagedPointer);

            Marshal.FreeHGlobal(unmanagedPointer);
        }

        public static string[] GetLocales()
        {
            var loaded = TranslationServer.GetLoadedLocales();
            var locales = new string[loaded.Count];
            for (var i = 0; i < locales.Length; ++i)
                locales[i] = loaded[i] as string;
            return locales;
        }
        public static string GetLocale() => TranslationServer.GetLocale();
        public static void SetLocale(string locale) => TranslationServer.SetLocale(locale);
    }
}
