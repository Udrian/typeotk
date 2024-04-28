using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using TypeOTKTest.Mock;

namespace TypeOTKTest
{
    public partial class TypeOTKModuleTest
    {
        public TypeO CreateTypeO()
        {
            var typeO = TypeO.Create<TestGameMock>(TestGameMock.GameName)
                    .LoadModule<TKModule>() as TypeO;
            Assert.NotNull(typeO);
            return typeO;
        }

        public TKGameWindow CreateWindow(TypeO typeO)
        {
            var tkGameService = typeO.Context.GetService<TKGameService>();
            Assert.NotNull(tkGameService);

            GLFWProvider.CheckForMainThread = false;
            var tkGameWindow = tkGameService.CreateTKGameWindow(new NativeWindowSettings() { });
            Assert.NotNull(tkGameWindow);
            return tkGameWindow;
        }
    }
}
