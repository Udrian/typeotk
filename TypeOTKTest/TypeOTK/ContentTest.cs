using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOTKTest.TypeTest;
using StbImageSharp;

namespace TypeOTKTest.TypeOTK
{
    public class ContentTest
    {
        [Fact]
        public void LoadTKTexture()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.SetTestContentBasePath(typeO);
            Utility.CreateTKWindow(typeO);

            var tkNonExistingTexture = typeO.Context.Game.ContentLoader.LoadContent<TKTexture>(Path.Combine("Mock", "Resources", "notexistingtexture.png"));
            Assert.Null(tkNonExistingTexture);

            var tkTexture = typeO.Context.Game.ContentLoader.LoadContent<TKTexture>(Path.Combine("Mock", "Resources", "texture.png"));
            Assert.NotNull(tkTexture);

            Assert.NotEqual(0, Utility.GetProperty<int>(tkTexture, "Handle"));
            Assert.NotNull(Utility.GetProperty<ImageResult>(tkTexture, "RgbaImage"));

            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "Mock", "Resources", "texture.png"), tkTexture.FilePath);
            Assert.Equal(new Vec2(256, 256), tkTexture.Size);
        }

        [Fact]
        public void LoadTKFont()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.SetTestContentBasePath(typeO);
            Utility.CreateTKWindow(typeO);

            var tkNonExistingFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "notexistingtexture.ttf"));
            Assert.Null(tkNonExistingFont);

            var tkFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"));
            Assert.NotNull(tkFont);
            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "Mock", "Resources", "Lato-Black.ttf"), tkFont.FilePath);
            Assert.Equal(0, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "FontSystem"));
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "SpriteFont"));

            tkFont.FontSize = 24;
            Assert.Equal(24, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "SpriteFont"));
            var size24 = tkFont.MeasureString("Test");
            Assert.True(size24.X > 0 && size24.Y > 0);

            tkFont.FontSize = 100;
            Assert.Equal(100, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "SpriteFont"));
            var size100 = tkFont.MeasureString("Test");
            Assert.True(size100.X > size24.X && size100.Y > size24.Y);

            tkFont.Draw("Hello", new Vec2(100, 100), Color.CapeHoney);
        }

        [Fact]
        public void LoadTKFontWithSize()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.SetTestContentBasePath(typeO);
            Utility.CreateTKWindow(typeO);

            var tkFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"), 25);
            Assert.NotNull(tkFont);
            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "Mock", "Resources", "Lato-Black.ttf"), tkFont.FilePath);
            Assert.Equal(25, tkFont.FontSize);
        }

        [Fact]
        public void SaveScreenshotTextureToDisk()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "tmp", "output.png");
            try
            {
                var typeO = Utility.DrawTest((context) =>
                {
                    context.Game.Scenes.Canvas.Clear(Color.Brown);
                    context.Game.Scenes.Canvas.Present();

                    var texture = context.Game.Scenes.Canvas.Screenshot();
                    Assert.NotNull(texture);
                    Assert.Equal(Color.Brown, texture.PixelAt(0, 0));

                    texture.Save(outputPath);
                    Assert.True(Path.Exists(outputPath));

                    var loadedTexture = context.Game.ContentLoader.LoadContent<Texture>(outputPath);
                    Assert.NotNull(loadedTexture);
                    Assert.Equal(Color.Brown, loadedTexture.PixelAt(0, 0));

                    context.Exit();
                });
                typeO.Start();
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }
    }
}
