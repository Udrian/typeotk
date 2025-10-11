using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TypeD.Models.Data;
using TypeD.View.Viewer;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Desktop;
using TypeOEngine.Typedeaf.TK;

namespace TypeDTK.View.Viewer;

/// <summary>
/// 
/// </summary>
public partial class TKViewer : UserControl, IViewer
{
    private bool IsLoaded { get; set; }

    public Project Project { get; private set; }

    public Component Component { get; private set; }

    private FakeViewer Viewer { get; set; }

    public TKViewer()
    {
        InitializeComponent();
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
            var mainSettings = new GLWpfControlSettings();
            OpenTkControl.Start(mainSettings);
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Viewer.Start();
        if(Project != null && Component != null)
        {
            Viewer.AddComponent(Project, Component);
        }
        
        SetWindowSize(new Size(ActualWidth, ActualHeight));
        IsLoaded = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (Viewer != null)
            Viewer.Close();
    }

    private void OnRender(TimeSpan delta)
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