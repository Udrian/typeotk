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

    public Project Project { get; private set; }

    public Component Component { get; private set; }

    private FakeViewer Viewer { get; set; }

    public TKViewer()
    {
        InitializeComponent();
    }

    public void Init(Project project, Component component)
    {
        var mainSettings = new GLWpfControlSettings();
        OpenTkControl.Start(mainSettings);

        Project = project;
        Component = component;
        Viewer = new FakeViewer(Project, new List<Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>>()
            {
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new DesktopModule(), new DesktopModuleOption() {}),
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new TKModule(), new TKModuleOption() {})
            });
        this.SizeChanged += (object sender, SizeChangedEventArgs e) => { SetWindowSize(e.NewSize); };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ActualWidth == 0 || ActualHeight == 0) return;
    
        Viewer.Start();
        if(Project != null && Component != null)
        {
            Viewer.AddComponent(Project, Component);
        }
        
        SetWindowSize(new Size(ActualWidth, ActualHeight));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Viewer.Close();
    }

    private void OnRender(TimeSpan delta)
    {
        Viewer.Update(delta.Milliseconds);
        Viewer.Draw();
    }
    private void SetWindowSize(Size size)
    {
        Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }
}