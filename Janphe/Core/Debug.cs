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

    }
}
