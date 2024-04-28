namespace TypeOTKTest;

public partial class TypeDTKModuleTest
{
    [Fact]
    public void StartInitializerTest()
    {
        Assert.NotNull(Initializer);
        Initializer.Initializer(null);
        Initializer.Uninitializer();
    }


}