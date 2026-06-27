using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OpenTKAvalonia;
using System;
using System.Collections.Generic;
using TypeD.Models.Data;
using TypeD.View.Viewer;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Desktop;
using TypeOEngine.Typedeaf.TK;

namespace TypeDTK.View.Viewer;

/// <summary>
/// 
/// </summary>
public partial class TKViewer : BaseTkOpenGlControl, IViewer
{
    private bool IsLoaded { get; set; }
    public Project Project { get; set; }

    private Component ComponentToAdd { get; set; }

    public Component Component { get; private set; }

    private FakeViewer Viewer { get; set; }

    public TKViewer()
    {
        IsLoaded = false;
    }

    public void Init()
    {
        Viewer = new FakeViewer(Project, new List<Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>>()
            {
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new DesktopModule(), new DesktopModuleOption() {}),
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new TKModule(), new TKModuleOption() {})
            });
        this.SizeChanged += (object sender, SizeChangedEventArgs e) => { SetWindowSize(e.NewSize); };
    }

    public void Load(Component component)
    {
        if (Project != null && Component != null && Viewer != null)
        {
            Viewer.Clear();
            Component = null;
        }

        ComponentToAdd = component;
    }

    public void Unload()
    {
        if(IsLoaded && Component != null)
        {
            Viewer.Clear();
            Component = null;
        }
    }

    protected override void OpenTkInit()
    {
        if (IsLoaded) return;
        Viewer.Start();
        
        SetWindowSize(new Size(Bounds.Size.Width, Bounds.Size.Height));
        IsLoaded = true;
    }

    protected override void OpenTkTeardown()
    {
        if (Viewer != null)
            Viewer.Close();

        IsLoaded = false;
    }

    protected override void OpenTkRender()
    {

        if (IsLoaded && ComponentToAdd != null)
        {
            Viewer.AddComponent(Project, ComponentToAdd);
            Component = ComponentToAdd;
            ComponentToAdd = null;
        }

        if (Viewer != null)
            Viewer.UpdateAndDraw();
    }

    private void SetWindowSize(Size size)
    {
        if(Viewer != null)
            Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetCurrentPoint(this);
        Viewer.SetMousePos(new Vec2i((int)point.Position.X, (int)point.Position.Y));
    }
}