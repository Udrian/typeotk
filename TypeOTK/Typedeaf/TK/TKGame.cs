using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace TypeOEngine.Typedeaf.TK
{
    internal class TKGame : GameWindow
    {
        public TKGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
        }
    }
}
