using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;

namespace TypeDTK.View.Viewer
{
    internal class EditorScene : Scene
    {
        protected override void Initialize()
        {
            base.Initialize();
            ClearColor = new Color(28, 3, 51);
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
            Canvas?.Clear(ClearColor);
            DrawStack.Draw(Canvas);
            Canvas?.Present();
        }

        public override void OnEnter(Scene from)
        {
        }

        public override void OnExit(Scene to)
        {
        }
    }
}
