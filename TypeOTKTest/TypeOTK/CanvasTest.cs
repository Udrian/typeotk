using TypeOTKTest.TypeTest;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;

namespace TypeOTKTest.TypeOTK
{
    public class CanvasTest
    {
        [Fact]
        public void CreateCanvasAndClear()
        {
            var typeO = Utility.DrawTest((context) =>
            {
                context.Game.Scenes.Canvas.Clear(Color.Brown);
                context.Game.Scenes.Canvas.Present();

                var texture = context.Game.Scenes.Canvas.Screenshot();
                Assert.NotNull(texture);
                Assert.Equal(Color.Brown, texture.PixelAt(0, 0));

                context.Game.Scenes.Canvas.Clear(Color.Yellow);

                texture = context.Game.Scenes.Canvas.Screenshot();
                Assert.NotNull(texture);
                Assert.NotEqual(Color.Yellow, texture.PixelAt(0, 0));

                context.Game.Scenes.Canvas.Present();

                texture = context.Game.Scenes.Canvas.Screenshot();
                Assert.NotNull(texture);
                Assert.Equal(Color.Yellow, texture.PixelAt(0, 0));

                context.Exit();
            });
            typeO.Start();
        }
    }
}
