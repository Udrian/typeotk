using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using TypeOTKTest.TypeTest.Mock;

namespace TypeOTKTest.TypeTest
{
    public static partial class Utility
    {
        internal static TKGameWindow CreateTKWindow(TypeO typeO)
        {
            var tkGameService = typeO.Context.GetService<TKGameService>();
            Assert.NotNull(tkGameService);

            GLFWProvider.CheckForMainThread = false;
            var tkGameWindow = tkGameService.CreateTKGameWindow(new NativeWindowSettings() { });
            Assert.NotNull(tkGameWindow);

            typeO.Context.Game.MainWindow = new TestWindowMock()
            {
                Title = typeO.Context.Name,
                Size = new Vec2i(tkGameWindow.Size.X, tkGameWindow.Size.Y),
                GameWindow = tkGameWindow
            };
            return tkGameWindow;
        }

        internal static TKCanvas CreateTKCanvas(Context context)
        {
            var canvas = new TKCanvas(context.Game.MainWindow, new Rectangle(new Vec2(), context.Game.MainWindow.Size), ((TestWindowMock)context.Game.MainWindow).GameWindow);
            context.InitializeObject(canvas);

            context.Game.MainWindow.Canvas = canvas;

            return canvas;
        }

        internal static TypeO DrawTest(Action<Context> drawAction)
        {
            var typeO = CreateTypeO((context) => {
                CreateTKWindow(context.TypeO);
                CreateTKCanvas(context);
                Assert.NotNull(context.Game.MainWindow);
                Assert.NotNull(context.Game.Scenes.Canvas);
                Assert.IsType<TKCanvas>(context.Game.Scenes.Canvas);
            }, (context) =>
            {
            }, drawAction);

            return typeO;
        }
    }
}
