using TypeOEngine.Typedeaf.Core;

namespace TypeOTKTest.Mock
{
    internal class TestGameMock : Game
    {
        public static string GameName { get; set; } = "test";
        public override void Initialize() { }
        public override void Update(double dt) { Exit(); }
        public override void Draw() { }
        public override void Cleanup() { }
    }
}
