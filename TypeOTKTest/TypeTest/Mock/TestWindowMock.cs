using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.TK;

namespace TypeOTKTest.TypeTest.Mock
{
    internal class TestWindowMock : IWindow
    {
        public string Title { get; set; }
        public Vec2i Size { get; set; }
        public ICanvas Canvas { get; set; }
        public TKGameWindow GameWindow { get; set; }
    }
}
