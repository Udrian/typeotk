using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core;

namespace TypeDTK.View.Viewer
{
    internal class FakeScene : Scene
    {
        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void Cleanup()
        {
        }

        public override void Update(double dt)
        {
            Entities.Update(dt);
            UpdateLoop.Update(dt);
        }
        public override void Draw()
        {
            DrawStack.Draw(Canvas);
        }

        public override void OnEnter(Scene from)
        {
        }

        public override void OnExit(Scene to)
        {
        }
    }
}
