using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Desktop.Engine.Services;

namespace TypeDTK.View.Viewer
{
    internal class FakeGame : Game
    {
        public Vec2i InitSize { get; set; }
        private WindowService WindowService { get; set; }
        public FakeWindow Window { get; set; }
        public ICanvas Canvas { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            //TODO: Should use WindowService to create the FakeWindow
            MainWindow = Window = new FakeWindow()
            {
                Size = InitSize
            };

            Window.Canvas = Canvas = WindowService.CreateCanvas(Window);
            Scenes.SetScene<FakeScene>();
        }

        protected override void Cleanup()
        {
            Scenes.Cleanup();
        }

        public override void Draw()
        {
            if (Canvas == null) return;
            Canvas.Clear(Color.DarkGray);
            Scenes.Draw();
            Canvas.Present();
        }

        public override void Update(double dt)
        {
            Scenes?.Update(dt);
        }
    }
}
