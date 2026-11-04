using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeDCore.Models.Data.SettingContexts;

namespace TypeDTK.View.Viewer
{
    internal class EditorScene : Scene
    {
        protected override void Initialize()
        {
            base.Initialize();
            ClearColor = ViewModelBase.ResourceModel.Get<ISettingModel>()
                .GetContext<ViewerSettingContext>().ClearColor.Value;
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
