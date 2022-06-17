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
        private ILogger Logger { get; set; }

        public override void Initialize(string title, Vec2i position, Vec2i size, bool fullscreen = false, bool borderless = false)
        {
            TKGameService.TKGame.Location = new Vector2i(position.X, position.Y);
            TKGameService.TKGame.WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal;
            TKGameService.TKGame.WindowBorder = borderless ? WindowBorder.Hidden : WindowBorder.Fixed;
            TKGameService.TKGame.Size = new Vector2i(size.X, size.Y);
            TKGameService.TKGame.Title = title;
            TKGameService.TKGame.Run();
        }

        protected override void Cleanup()
        {
            TKGameService.TKGame.Close();
        }

        public override string Title
        {
            get => TKGameService.TKGame.Title;
            set => TKGameService.TKGame.Title = value;
        }
        public override Vec2i Position
        {
            get => new Vec2i(TKGameService.TKGame.Location.X, TKGameService.TKGame.Location.Y);
            set => TKGameService.TKGame.Location = new Vector2i(value.X, value.Y);
        }
        public override Vec2i Size
        {
            get => new Vec2i(TKGameService.TKGame.Size.X, TKGameService.TKGame.Size.Y);
            set => TKGameService.TKGame.Size = new Vector2i(value.X, value.Y);
        }
        public override bool Fullscreen
        {
            get => TKGameService.TKGame.IsFullscreen;
            set => TKGameService.TKGame.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }
        public override bool Borderless
        {
            get => TKGameService.TKGame.WindowBorder == WindowBorder.Hidden;
            set => TKGameService.TKGame.WindowBorder = value ? WindowBorder.Hidden : WindowBorder.Fixed;
        }
    }
}
