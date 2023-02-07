using TypeDTK;
using System.Reflection;

namespace TypeOTKTest;

public class TypeDTKModuleTest
{
    private TypeDTKInitializer CreateInitializer()
    {
        var initializer = new TypeDTKInitializer();

        Type t = initializer.GetType();
        t.InvokeMember("Hooks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, initializer, new object[] { new HookModelMock() });
        t.InvokeMember("Resources", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, initializer, new object[] { new ResourceModelMock() });

        return initializer;
    }

    [Fact]
    public void StartTest()
    {
        var initializer = CreateInitializer();
        Assert.NotNull(initializer);
        initializer.Initializer(null);
        initializer.Uninitializer();
    }
}