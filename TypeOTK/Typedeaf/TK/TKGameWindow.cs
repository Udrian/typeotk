using OpenTK.Windowing.Desktop;

namespace TypeOEngine.Typedeaf.TK
{
    /// <summary>
    /// TypeOTK inherited OpenTK NativeWindow
    /// </summary>
    public class TKGameWindow : NativeWindow
    {
        internal TKGameWindow(NativeWindowSettings nativeWindowSettings) : base(nativeWindowSettings)
        {
        }
    }
}
