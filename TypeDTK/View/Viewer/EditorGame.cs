using TypeOEngine.Typedeaf.Core.Common;
using TypeD.View.Viewer;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Desktop.Engine.Services;

namespace TypeDTK.View.Viewer
{
    internal class EditorGame : Game
    {
        public Vec2i InitSize { get; set; }
        private WindowService WindowService { get; set; }
        public FakeWindow Window { get; set; }
        public ICanvas Canvas { get; set; }
        /// <summary>The editor camera, applied right before drawing so it wins over cameras moved by game logic.</summary>
        public ViewerCamera EditorCamera { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            //TODO: Should use WindowService to create the FakeWindow
            MainWindow = Window = new FakeWindow()
            {
                Size = InitSize
            };

            Window.Canvas = Canvas = WindowService.CreateCanvas(Window);
            Scenes.SetScene<EditorScene>();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
        }

        public override void Draw()
        {
            if (Canvas == null) return;
            if (EditorCamera != null)
            {
                Canvas.WorldTranslation = new Vec3(EditorCamera.X, EditorCamera.Y, 0);
                Canvas.WorldScale = EditorCamera.Zoom;
            }
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
