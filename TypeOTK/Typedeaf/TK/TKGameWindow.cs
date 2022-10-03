using OpenTK.Windowing.Desktop;
using System.ComponentModel;

namespace TypeOEngine.Typedeaf.TK
{
    public class TKGameWindow : NativeWindow
    {
        internal TKGameWindow(NativeWindowSettings nativeWindowSettings) : base(nativeWindowSettings)
        {
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        protected override void OnClosed()
        {
            base.OnClosed();
        }
    }
}
