using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using TypeD.Models.Data;
using TypeD.View.Viewer;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Desktop;
using TypeOEngine.Typedeaf.TK;
using OpenTKAvalonia;

namespace TypeDTK.View.Viewer;

/// <summary>
/// 
/// </summary>
public partial class TKViewer : BaseTkOpenGlControl, IViewer
{
    private bool IsLoaded { get; set; }
    public Project Project { get; private set; }

    public Component Component { get; private set; }

    private FakeViewer Viewer { get; set; }

    public TKViewer()
    {
        IsLoaded = false;
    }

    public void Init(Project project, Component component)
    {
        if (Project != null && Component != null && Viewer != null)
        {
            Viewer.Clear();
        }

        Project = project;
        Component = component;

        if (Viewer == null)
        {
            Viewer = new FakeViewer(Project, new List<Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>>()
                {
                    new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new DesktopModule(), new DesktopModuleOption() {}),
                    new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new TKModule(), new TKModuleOption() {})
                });
            this.SizeChanged += (object sender, SizeChangedEventArgs e) => { SetWindowSize(e.NewSize); };
        }

        if(IsLoaded)
        {
            if (Project != null && Component != null)
            {
                Viewer.AddComponent(Project, Component);
            }
        }
    }

    protected override void OpenTkInit()
    {
        Viewer.Start();
        if (Project != null && Component != null)
        {
            Viewer.AddComponent(Project, Component);
        }
        
        SetWindowSize(new Size(Bounds.Size.Width, Bounds.Size.Height));
        IsLoaded = true;
    }

    protected override void OpenTkTeardown()
    {
        if (Viewer != null)
            Viewer.Close();
    }

    protected override void OpenTkRender()
    {
        if (Viewer != null)
            Viewer.UpdateAndDraw();
    }

    private void SetWindowSize(Size size)
    {
        if(Viewer != null)
            Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }
}