using OpenTK.Windowing.Desktop;
using System.Windows;
using System.Windows.Controls;
using TypeD.Models.Interfaces;
using TypeDTK.View.Viewer;
using TypeOTKTest.TypeTest;

namespace TypeDTKTest;

public partial class ModuleTest
{
    [Fact]
    public void StartInitializerTest()
    {
        Assert.NotNull(TypeDTKTestExtension.Initializer);
        TypeDTKTestExtension.Initializer.Initializer(null);
        var panelModel = Utility.GetProperty<IPanelModel>(TypeDTKTestExtension.Initializer, "PanelModel");
        Assert.NotNull(panelModel);
        Assert.Contains(typeof(TKViewer).FullName, panelModel.ListViewers());
        TypeDTKTestExtension.Initializer.Uninitializer();
    }

    [StaFact]
    public void DisplayFakeGameViewer()
    {
        GLFWProvider.CheckForMainThread = false;
        Window window = new Window();
        Grid grid = new Grid();
        TKViewer tkViewer = new TKViewer();
        grid.Children.Add(tkViewer);
        window.Content = grid;

        //TODO: Should create a proper component that we could test
        tkViewer.Init(null, null);

        window.ShowDialog();
    }
}