using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Interfaces;
using TypeOEngine.Typedeaf.Desktop.Engine.Graphics;
using TypeOEngine.Typedeaf.TK.Engine.Services;

namespace TypeOEngine.Typedeaf.TK.Engine.Graphics
{
    internal class TKWindow : DesktopWindow
    {
        private TKGameService TKGameService { get; set; }

        private TKGame TKGame { get; set; }

        private ILogger Logger { get; set; }

        public override void Initialize(string title, Vec2i position, Vec2i size, bool fullscreen = false, bool borderless = false)
        {
            var setting = new NativeWindowSettings
            {
                Location = new Vector2i(position.X, position.Y),
                WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal,
                WindowBorder = borderless ? WindowBorder.Hidden : WindowBorder.Fixed,
                Size = new Vector2i(size.X, size.Y),
                Title = title
            };

            TKGame = TKGameService.CreateTKGameWindow(setting);
        }

        protected override void Cleanup()
        {
            TKGame.Close();
        }

        public override string Title
        {
            get => TKGame.Title;
            set => TKGame.Title = value;
        }
        public override Vec2i Position
        {
            get => new Vec2i(TKGame.Location.X, TKGame.Location.Y);
            set => TKGame.Location = new Vector2i(value.X, value.Y);
        }
        public override Vec2i Size
        {
            get => new Vec2i(TKGame.Size.X, TKGame.Size.Y);
            set => TKGame.Size = new Vector2i(value.X, value.Y);
        }
        public override bool Fullscreen
        {
            get => TKGame.IsFullscreen;
            set => TKGame.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }
        public override bool Borderless
        {
            get => TKGame.WindowBorder == WindowBorder.Hidden;
            set => TKGame.WindowBorder = value ? WindowBorder.Hidden : WindowBorder.Fixed;
        }
    }
}
