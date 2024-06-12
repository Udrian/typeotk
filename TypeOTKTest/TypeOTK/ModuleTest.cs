using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using TypeOTKTest.TypeTest;

namespace TypeOTKTest;

public partial class ModuleTest
{
    [Fact]
    public void LoadModuleTest()
    {
        var typeO = Utility.CreateTypeO();
        typeO.Start();

        var module = typeO.Context.Modules.FirstOrDefault(m => m.GetType() == typeof(TKModule)) as TKModule;
        Assert.NotNull(module);
        Assert.IsType<TKModule>(module);
        Assert.NotEmpty(typeO.Context.Modules);

        var tkGameService = typeO.Context.GetService<TKGameService>();
        Assert.NotNull(tkGameService);
    }

    [Fact]
    public void OpenGlWindow()
    {
        var typeO = Utility.CreateTypeO();
        typeO.Start();
        Utility.CreateTKWindow(typeO);
        Assert.NotNull(typeO.Context.Game.MainWindow);
    }
}