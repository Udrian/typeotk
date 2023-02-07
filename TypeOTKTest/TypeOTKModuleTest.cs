using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK;

namespace TypeOTKTest;

public class TestGame : Game
{
    public static string GameName { get; set; } = "test";
    public override void Initialize() { }
    public override void Update(double dt) { Exit(); }
    public override void Draw() { }
    public override void Cleanup() { }
}

public class TypeOTKModuleTest
{
    [Fact]
    public void StartTest()
    {
        var typeO = TypeO.Create<TestGame>(TestGame.GameName)
                .LoadModule<TKModule>() as TypeO;
        Assert.NotNull(typeO);
        typeO.Start();
        var module = typeO.Context.Modules.FirstOrDefault(m => m.GetType() == typeof(TKModule)) as TKModule;
        Assert.NotNull(module);
        Assert.IsType<TKModule>(module);
        Assert.NotEmpty(typeO.Context.Modules);
    }
}