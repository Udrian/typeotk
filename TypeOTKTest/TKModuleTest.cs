using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK;

namespace TypeOTKTest
{
    public class TKModuleTest
    {
        public class TestGame : Game
        {
            public override void Initialize()
            {
            }

            public override void Update(double dt)
            {
            }

            public override void Draw()
            {
            }

            public override void Cleanup()
            {
            }
        }


        [Fact]
        public void LoadTKModule()
        {
            var typeO = TypeO.Create<TestGame>("")
                             .LoadModule<TKModule>() as TypeO;
            Assert.NotNull(typeO);
            var module = typeO.Context.Modules.FirstOrDefault(m => m.GetType() == typeof(TKModule)) as TKModule;
            Assert.NotNull(module);
            Assert.IsType<TKModule>(module);
            Assert.NotEmpty(typeO.Context.Modules);
        }
    }
}