using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Desktop.Engine.Graphics;

namespace TypeDTK.View.Viewer
{
    internal class FakeWindow : DesktopWindow
    {
        public override Vec2i Position { get; set; }
        public override bool Fullscreen { get; set; }
        public override bool Borderless { get; set; }
        public override string Title { get; set; }
        public override Vec2i Size { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
        }

        public override void Set(string title, Vec2i position, Vec2i size, bool fullscreen = false, bool borderless = false)
        {
            Title = title;
            Position = position;
            Size = size;
            Fullscreen = fullscreen;
            Borderless = borderless;
        }

        protected override void Cleanup()
        {
        }
    }
}
