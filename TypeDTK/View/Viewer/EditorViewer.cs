using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TypeD.Helpers;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;
using TypeD.ViewModel;
using TypeDCore.Models.Data.Hooks;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Entities;
using TypeOEngine.Typedeaf.Core.Entities.Drawables;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;
using Module = TypeOEngine.Typedeaf.Core.Engine.Module;

namespace TypeDTK.View.Viewer
{
    internal class EditorViewer
    {
        private TypeO EditorTypeO { get; set; }
        private EditorGame Game { get; set; }
        private Project Project { get; set; }

        private IHookModel HookModel { get; set; }
        private ConcurrentQueue<ContentLoadRequestedHook> PendingContentLoads { get; } = new();
        private List<Action> RemoveHooks { get; } = new();
        private bool Closed { get; set; }

        public EditorViewer(Project project, List<Tuple<Module, ModuleOption>> modules)
        {
            this.Project = project;
            HookModel = ViewModelBase.ResourceModel.Get<IHookModel>();

            EditorTypeO = (TypeO)TypeO.Create<EditorGame>("Drawable Viewer");
            Game = (EditorGame)EditorTypeO.Context.Game;
            Game.RunSynchronously = false;

            foreach (var module in modules)
            {
                EditorTypeO.LoadModule(module.Item1, module.Item2);
            }

            AddHook<PropertyChangedHook>((hook) =>
            {
                TypeOObject obj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.Component.FullParentPath + hook.Component.ID);
                var objType = obj.GetType();
                var value = hook.Property.Value;
                var definition = ViewModelBase.ResourceModel.Get<IPropertyModel>().Get(hook.Property.Type);
                if (definition != null && definition.ValueType != definition.Type)
                    return;
                objType.GetProperty(hook.Property.Name)?.SetValue(obj, value);
            });
            AddHook<ContentLoadRequestedHook>((hook) =>
            {
                PendingContentLoads.Enqueue(hook);
            });
            AddHook<ComponentAddedHook>((hook) =>
            {
                AddComponentToView(project, hook.Child, hook.Parent);
            });
            AddHook<ComponentMovedHook>((hook) =>
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
            AddHook<ComponentRemovedHook>((hook) =>
            {
                RemoveComponentObjects(project, hook.Child);
                hook.Child.ID = null;
            });
            AddHook<ComponentRemovedStateChangedHook>((hook) =>
            {
                var parentObj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(hook.Parent.FullParentPath + hook.Parent.ID);
                if (hook.Child.Removed)
                {
                    RemoveComponentObjects(project, hook.Child);
                }
                else if (parentObj is Entity parentEntity)
                {
                    var typeInfo = ResolveType(project, hook.Child.FullName);
                    if (typeInfo == null) return;
                    TypeOObject restored = null;
                    if (hook.Child.IsBasedOn(typeof(Drawable)))
                        restored = parentEntity.Drawables.Create(typeInfo, hook.Child.ID);
                    else if (hook.Child.IsBasedOn(typeof(Entity)))
                        restored = parentEntity.Entities.Create(typeInfo, hook.Child.ID);
                    if (restored != null)
                    {
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = restored, Component = hook.Child });
                        foreach (var nested in hook.Child.Children.Flatten(c => c.Children))
                        {
                            var nestedObj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(nested.FullParentPath + nested.ID);
                            if (nestedObj != null)
                                HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = nestedObj, Component = nested });
                        }
                    }
                }
            });
        }

        public void Start()
        {
            if(!EditorTypeO.Context.Game.Initialized)
                EditorTypeO.Start();
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
            EditorTypeO.Context.ListAllTypeOObjects().ForEach(e =>
            {
                if (e is Drawable || e is Entity || (e is Scene && !(e is EditorScene)))
                {
                    if (i == 0)
                    {
                        component.ID = e.ID;
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = e, Component = component });
                    }
                    else
                    {
                        components[i - 1].ID = e.ID;
                        HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = e, Component = components[i - 1] });
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
            if (parent != null && EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(parent.FullParentPath + parent.ID) is Entity parentEntity)
            {
                if (component.IsBasedOn(typeof(Drawable)))
                    return parentEntity.Drawables.Create(typeInfo, component.ID);
                if (component.IsBasedOn(typeof(Entity)))
                    return parentEntity.Entities.Create(typeInfo, component.ID);
            }
            if (component.IsBasedOn(typeof(Drawable)))
            {
                return Game.Scenes.CurrentScene.Drawables.Create(typeInfo, component.ID);
            }
            else if (component.IsBasedOn(typeof(Entity)))
            {
                return Game.Scenes.CurrentScene.Entities.Create(typeInfo, component.ID);
            }
            else if (component.IsBasedOn(typeof(Scene)))
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
            HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = addedObject, Component = child });
            foreach (var nested in child.Children.Flatten(c => c.Children))
            {
                var nestedObj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(nested.FullParentPath + nested.ID);
                if (nestedObj != null)
                    HookModel.Shoot(new TypeOObjectAddedToViewHook() { Context = EditorTypeO.Context, TypeOObject = nestedObj, Component = nested });
            }
            foreach (var property in child.Properties.Where(p => p.Name != "ID" && child.HasPropertyOverridesFor(p.Name)).ToList())
                HookModel.Shoot(new PropertyChangedHook() { Component = child, Property = property });
        }

        private void RemoveComponentObjects(Project project, Component component)
        {
            foreach (var child in component.Children.ToList())
                RemoveComponentObjects(project, child);

            var obj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(component.FullParentPath + component.ID);
            if (obj == null)
                return;

            HookModel.Shoot(new TypeOObjectRemovedFromViewHook() { Context = EditorTypeO.Context, TypeOObject = obj, Component = component });
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
                if(Game.Canvas != null)
                {
                    Game.Canvas.Viewport = new Rectangle(Game.Canvas.Viewport.Pos, size);
                    // Keeps one world unit per viewer unit, the editor camera maps the pointer with that
                    (Game.Canvas as TKCanvas)?.UpdateProjection();
                }
            }
        }

        public void SetCamera(ViewerCamera camera)
        {
            if (Game != null)
                Game.EditorCamera = camera;
        }

        public void UpdateAndDraw(MouseStateSnapshot mouseState)
        {
            IMouseHardware mouseHardware = EditorTypeO.Context.Hardwares[typeof(IMouseHardware)] as IMouseHardware;
            mouseHardware?.SetState(mouseState);

            if (EditorTypeO.Context.Game.Initialized)
            {
                ProcessPendingContentLoads();
                EditorTypeO.Context.ProcessGame();
            }
        }

        private void ProcessPendingContentLoads()
        {
            while (PendingContentLoads.TryDequeue(out var request))
            {
                var obj = EditorTypeO.Context.GetTypeOObjectByID<TypeOObject>(request.Component.FullParentPath + request.Component.ID);
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
            if (Closed) return;
            Closed = true;

            // The viewer is recreated when the game is reloaded, so the old game must stop receiving editor changes
            foreach (var removeHook in RemoveHooks)
                removeHook();
            RemoveHooks.Clear();

            Game.Exit();
        }

        private void AddHook<T>(Action<T> action) where T : Hook, new()
        {
            HookModel.AddHook(action);
            RemoveHooks.Add(() => HookModel.RemoveHook(action));
        }
    }
}