using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeDCore.Models.Data.Hooks;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Entities;
using TypeOEngine.Typedeaf.Core.Entities.Drawables;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;
using Module = TypeOEngine.Typedeaf.Core.Engine.Module;

namespace TypeDTK.View.Viewer
{
    internal class FakeViewer
    {
        private TypeO FakeTypeO { get; set; }
        private FakeGame Game { get; set; }
        private Project Project { get; set; }

        private IHookModel HookModel { get; set; }
        private ConcurrentQueue<ContentLoadRequestedHook> PendingContentLoads { get; } = new();

        public FakeViewer(Project project, List<Tuple<Module, ModuleOption>> modules)
        {
            this.Project = project;
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
                TypeOObject obj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.Component.FullParentPath + hook.Component.ID);
                var objType = obj.GetType();
                var value = hook.Property.Value;
                var definition = ViewModelBase.ResourceModel.Get<IPropertyModel>().Get(hook.Property.Type);
                if (definition != null && definition.ValueType != definition.Type)
                    return;
                objType.GetProperty(hook.Property.Name)?.SetValue(obj, value);
            });
            HookModel.AddHook<ContentLoadRequestedHook>((hook) =>
            {
                PendingContentLoads.Enqueue(hook);
            });
            HookModel.AddHook<ComponentAddedHook>((hook) =>
            {
                var addedObject = InternalAddComponent(project, hook.Child); //TODO: Should add it properly to the parent
                if(addedObject != null)
                {
                    hook.Child.ID = addedObject.ID;
                    HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = addedObject, Component = hook.Child });
                }
            });
            HookModel.AddHook<ComponentRemovedHook>((hook) =>
            {
                TypeOObject obj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.Child.ID);

                HookModel.Shoot(new TypeOObjectRemovedFromViewHook() { Context = FakeTypeO.Context, TypeOObject = obj, Component = hook.Child });
                InternalRemoveComponent(project, obj);
                hook.Child.ID = null;
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
            Game.Scenes.CurrentScene.Entities.Update(0);
            Game.Scenes.CurrentScene.Drawables.Clear();
            HookModel.Shoot(new ViewerUnloadedHook { Project = Project });
        }

        public void AddComponent(Project project, Component component)
        {
            InternalAddComponent(project, component);

            int i = 0;
            var components = component.Children.Flatten(c => c.Children).ToList();
            FakeTypeO.Context.ListAllTypeOObjects().ForEach(e =>
            {
                if (e is Drawable || e is Entity || (e is Scene && !(e is FakeScene)))
                {
                    if (i == 0)
                    {
                        component.ID = e.ID;
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = e, Component = component });
                    }
                    else
                    {
                        components[i - 1].ID = e.ID;
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = e, Component = components[i - 1] });
                    }
                    i++;
                }
            });
        }

        private void SyncContentBasePath()
        {
            if (Game?.ContentLoader != null)
                Game.ContentLoader.BasePath = Project.ProjectContentPath;
        }

        private TypeOObject InternalAddComponent(Project project, Component component)
        {
            SyncContentBasePath();
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

        private void InternalRemoveComponent(Project project, TypeOObject obj)
        {
            if (obj is Entity entity)
            {
                entity.Remove();
            }
            else if (obj is Drawable drawable)
            {
                Game.Scenes.CurrentScene.Drawables.Destroy(drawable);
            }
            else if (obj is Scene scene)
            {
                Game.Scenes.SetScene(null);
            }
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

        public void UpdateAndDraw(MouseStateSnapshot mouseState)
        {
            IMouseHardware mouseHardware = FakeTypeO.Context.Hardwares[typeof(IMouseHardware)] as IMouseHardware;
            mouseHardware?.SetState(mouseState);

            if (FakeTypeO.Context.Game.Initialized)
            {
                ProcessPendingContentLoads();
                FakeTypeO.Context.ProcessGame();
            }
        }

        private void ProcessPendingContentLoads()
        {
            while (PendingContentLoads.TryDequeue(out var request))
            {
                var obj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(request.Component.FullParentPath + request.Component.ID);
                var property = obj?.GetType().GetProperty(request.Property.Name);
                if (property == null)
                    continue;

                if (request.Association == null)
                {
                    property.SetValue(obj, null);
                    continue;
                }

                SyncContentBasePath();
                var load = typeof(ContentLoader)
                    .GetMethod(nameof(ContentLoader.LoadContent), new[] { typeof(string) })
                    .MakeGenericMethod(request.Association.EngineImplementationType);
                var content = load.Invoke(Game.ContentLoader, new object[] { request.Path });
                property.SetValue(obj, content);
                request.Loaded?.Invoke(content);
            }
        }

        public void Close()
        {
            Game.Exit();
        }
    }
}