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
                AddComponentToView(project, hook.Child, hook.Parent);
            });
            HookModel.AddHook<ComponentMovedHook>((hook) =>
            {
                // Objects are appended on creation, so re-create the moved node and every later sibling to keep the order.
                var siblings = hook.Parent.Children.ToList();
                var start = siblings.IndexOf(hook.Child);
                if (start < 0) return;
                var affected = siblings.Skip(start).Where(c => !c.Removed).ToList();
                foreach (var sibling in affected)
                    RemoveComponentObjects(project, sibling);
                foreach (var sibling in affected)
                    AddComponentToView(project, sibling, hook.Parent);
            });
            HookModel.AddHook<ComponentRemovedHook>((hook) =>
            {
                RemoveComponentObjects(project, hook.Child);
                hook.Child.ID = null;
            });
            HookModel.AddHook<ComponentRemovedStateChangedHook>((hook) =>
            {
                var parentObj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.Parent.FullParentPath + hook.Parent.ID);
                if (hook.Child.Removed)
                {
                    RemoveComponentObjects(project, hook.Child);
                }
                else if (parentObj is Entity parentEntity)
                {
                    var typeInfo = ResolveType(project, hook.Child.FullName);
                    if (typeInfo == null) return;
                    TypeOObject restored = null;
                    if (hook.Child.TypeOBaseType == typeof(Drawable))
                        restored = parentEntity.Drawables.Create(typeInfo, hook.Child.ID);
                    else if (hook.Child.TypeOBaseType == typeof(Entity))
                        restored = parentEntity.Entities.Create(typeInfo, hook.Child.ID);
                    if (restored != null)
                    {
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = restored, Component = hook.Child });
                        foreach (var nested in hook.Child.Children.Flatten(c => c.Children))
                        {
                            var nestedObj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(nested.FullParentPath + nested.ID);
                            if (nestedObj != null)
                                HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = nestedObj, Component = nested });
                        }
                    }
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

        private Type ResolveType(Project project, string fullName)
        {
            var type = project.Assembly?.GetType(fullName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;
                try
                {
                    type = assembly.GetType(fullName);
                }
                catch (Exception)
                {
                    continue;
                }
                if (type != null)
                    return type;
            }
            return null;
        }

        private TypeOObject InternalAddComponent(Project project, Component component, Component parent = null)
        {
            SyncContentBasePath();
            var typeInfo = ResolveType(project, component.FullName);
            if (typeInfo == null) return null;
            if (parent != null && FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(parent.FullParentPath + parent.ID) is Entity parentEntity)
            {
                if (component.TypeOBaseType == typeof(Drawable))
                    return parentEntity.Drawables.Create(typeInfo, component.ID);
                if (component.TypeOBaseType == typeof(Entity))
                    return parentEntity.Entities.Create(typeInfo, component.ID);
            }
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

        private void AddComponentToView(Project project, Component child, Component parent)
        {
            var addedObject = InternalAddComponent(project, child, parent);
            if (addedObject == null)
                return;

            child.ID = addedObject.ID;
            HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = addedObject, Component = child });
            foreach (var nested in child.Children.Flatten(c => c.Children))
            {
                var nestedObj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(nested.FullParentPath + nested.ID);
                if (nestedObj != null)
                    HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = FakeTypeO.Context, TypeOObject = nestedObj, Component = nested });
            }
            foreach (var property in child.Properties.Where(p => p.Name != "ID" && child.HasPropertyOverridesFor(p.Name)).ToList())
                HookModel.Shoot(new PropertyChangedHook() { Component = child, Property = property });
        }

        private void RemoveComponentObjects(Project project, Component component)
        {
            foreach (var child in component.Children.ToList())
                RemoveComponentObjects(project, child);

            var obj = FakeTypeO.Context.GetTypeOObjectByID<TypeOObject>(component.FullParentPath + component.ID);
            if (obj == null)
                return;

            HookModel.Shoot(new TypeOObjectRemovedFromViewHook() { Context = FakeTypeO.Context, TypeOObject = obj, Component = component });
            InternalRemoveComponent(project, obj);
        }

        private void InternalRemoveComponent(Project project, TypeOObject obj)
        {
            if (obj is Entity entity)
            {
                entity.Remove();
            }
            else if (obj is Drawable drawable)
            {
                if (drawable.Parent != null)
                    drawable.Parent.Drawables.Destroy(drawable);
                else
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