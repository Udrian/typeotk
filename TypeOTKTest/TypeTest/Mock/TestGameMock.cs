using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Engine.Interfaces;

namespace TypeOTKTest.TypeTest.Mock
{
    internal class TestGameMock : Game
    {
        internal Action<Context> InitializeAction { get; set; }
        internal Action<Context> UpdateAction { get; set; }
        internal Action<Context> DrawAction { get; set; }
        internal Action<Context> CleanUpAction { get; set; }

        public static string GameName { get; set; } = "test";
        public override void Initialize() { InitializeAction?.Invoke((this as IHasContext).Context); }
        public override void Update(double dt) { if (UpdateAction == null) Exit(); else UpdateAction.Invoke((this as IHasContext).Context); }
        public override void Draw() { DrawAction?.Invoke((this as IHasContext).Context); }
        public override void Cleanup() { CleanUpAction?.Invoke((this as IHasContext).Context); }
    }
}
