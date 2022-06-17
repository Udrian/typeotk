using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Graphics;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.Core.Engine.Hardwares;
using TypeOEngine.Typedeaf.Desktop.Engine.Graphics;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;

namespace TypeOEngine.Typedeaf.TK.Engine.Hardwares
{
    internal class TKWindowHardware : Hardware, IWindowHardware
    {
        public override void Initialize() { }

        public override void Cleanup() { }

        public Canvas CreateCanvas(IWindow window)
        {
            throw new NotImplementedException();
        }

        public ContentLoader CreateContentLoader(Canvas canvas)
        {
            throw new NotImplementedException();
        }

        public DesktopWindow CreateWindow()
        {
            return new TKWindow();
        }
    }
}
