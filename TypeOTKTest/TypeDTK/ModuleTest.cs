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

    [Fact]
    public void InitializeFakeGameViewer()
    {
        var initializer = TypeDTKTestExtension.Initializer;
        initializer.Initializer(null);
        try
        {
            var tkViewer = new TKViewer();
            tkViewer.Init();
            Assert.NotNull(Utility.GetProperty<object>(tkViewer, "Viewer"));
        }
        finally
        {
            initializer.Uninitializer();
        }
    }
}