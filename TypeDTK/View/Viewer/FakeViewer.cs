using System;
using System.Collections.Generic;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Entities;
using TypeOEngine.Typedeaf.Core.Entities.Drawables;
using Module = TypeOEngine.Typedeaf.Core.Engine.Module;

namespace TypeDTK.View.Viewer
{
    internal class FakeViewer
    {
        private TypeO FakeTypeO { get; set; }
        private FakeGame Game { get; set; }

        private IHookModel HookModel { get; set; }

        public FakeViewer(Project project, List<Tuple<Module, ModuleOption>> modules)
        {
            HookModel = ViewModelBase.ResourceModel.Get<IHookModel>();

            FakeTypeO = (TypeO)TypeO.Create<FakeGame>("Drawable Viewer");
            Game = (FakeGame)FakeTypeO.Context.Game;
            Game.RunSynchronously = false;

            foreach (var module in modules)
            {
                FakeTypeO.LoadModule(module.Item1, module.Item2);
            }

            HookModel.AddHook<PropertyChangedHook>((hook) =>
            {
                TypeOObject obj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.ID);
                var objType = obj.GetType();
                objType.GetProperty(hook.Property.Name)?.SetValue(obj, hook.Property.Value);
            });
            HookModel.AddHook<ComponentAddedHook>((hook) =>
            {
                var addedObject = InternalAddComponent(project, hook.Child); //TODO: Should add it properly to the parent
                if(addedObject != null)
                {
                    hook.Child.ID = addedObject.ID;
                }
            });
        }

        public void Start()
        {
            if(!FakeTypeO.Context.Game.Initialized)
                FakeTypeO.Start();
        }

        public void Clear()
        {
            if (Game == null) return;
            Game.Scenes.CurrentScene.Entities.Clear();
            Game.Scenes.CurrentScene.Drawables.Clear();
        }

        public void AddComponent(Project project, Component component)
        {
            InternalAddComponent(project, component);

            int i = 0;
            FakeTypeO.Context.ListAllTypeOObjects().ForEach(e =>
            {
                if (e is Drawable || e is Entity)
                {
                    if (i == 0)
                    {
                        component.ID = e.ID;
                    }
                    else
                    {
                        component.Children[i - 1].ID = e.ID;
                    }
                    i++;
                }
            });
        }

        private TypeOObject InternalAddComponent(Project project, Component component)
        {
            var typeInfo = project.Assembly.GetType(component.FullName);
            if (typeInfo == null) return null;
            if (component.TypeOBaseType == typeof(Drawable))
            {
                return Game.Scenes.CurrentScene.Drawables.Create(typeInfo, component.ID);
            }
            else if (component.TypeOBaseType == typeof(Entity))
            {
                return Game.Scenes.CurrentScene.Entities.Create(typeInfo, component.ID);
            }
            else if (component.TypeOBaseType == typeof(Scene))
            {
                return Game.Scenes.SetScene(typeInfo);
            }
            return null;
        }

        public void SetWindowSize(Vec2i size)
        {
            if (Game == null) return;
            if (Game.Window == null)
            {
                Game.InitSize = size;
            }
            else
            {
                Game.Window.Size = new Vec2i(size);
                Game.Canvas.Viewport = new Rectangle(Game.Canvas.Viewport.Pos, size);
            }
        }

        public void UpdateAndDraw()
        {
            if (FakeTypeO.Context.Game.Initialized)
                FakeTypeO.Context.ProcessGame();
        }

        public void Close()
        {
            Game.Exit();
        }
    }
}