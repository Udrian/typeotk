using System;
using System.Collections.Generic;
using TypeD.Models.Data;
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

        public FakeViewer(Project project, List<Tuple<Module, ModuleOption>> modules)
        {
            FakeTypeO = (TypeO)TypeO.Create<FakeGame>("Drawable Viewer");
            Game = (FakeGame)FakeTypeO.Context.Game;

            foreach (var module in modules)
            {
                FakeTypeO.LoadModule(module.Item1, module.Item2);
            }
        }

        public void Start()
        {
            if(!FakeTypeO.Context.Game.Initialized)
                FakeTypeO.Start();
        }

        public void AddComponent(Project project, Component component)
        {
            var typeInfo = project.Assembly.GetType(component.FullName);
            if (typeInfo == null) return;
            if (component.TypeOBaseType == typeof(Drawable))
            {
                Game.Scenes.CurrentScene.Drawables.Create(typeInfo);
            }
            else if (component.TypeOBaseType == typeof(Entity))
            {
                Game.Scenes.CurrentScene.Entities.Create(typeInfo);
            }
            else if (component.TypeOBaseType == typeof(Scene))
            {
                Game.Scenes.SetScene(typeInfo);
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

        public void Update(double dt)
        {
            Game?.Update(dt);
        }

        public void Draw()
        {
            Game?.Draw();
        }

        public void Close()
        {
            Game.Exit();
        }
    }
}