using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOTKTest.TypeTest;

namespace TypeOTKTest.TypeOTK
{
    public class ContentTest
    {
        [Fact]
        public void LoadTKTexture()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.CreateTKWindow(typeO);

            var tkNonExistingTexture = typeO.Context.Game.ContentLoader.LoadContent<TKTexture>(Path.Combine("Mock", "Resources", "notexistingtexture.png"));
            Assert.Null(tkNonExistingTexture);

            var tkTexture = typeO.Context.Game.ContentLoader.LoadContent<TKTexture>(Path.Combine("Mock", "Resources", "texture.png"));
            Assert.NotNull(tkTexture);

            Assert.NotEqual(0, Utility.GetProperty<int>(tkTexture, "Handle"));
            Assert.NotNull(Utility.GetProperty<Image<Rgba32>>(tkTexture, "RgbaImage"));

            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "Mock", "Resources", "texture.png"), tkTexture.FilePath);
            Assert.Equal(new Vec2(256, 256), tkTexture.Size);
        }

        [Fact]
        public void LoadTKFont()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.CreateTKWindow(typeO);

            var tkNonExistingFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "notexistingtexture.ttf"));
            Assert.Null(tkNonExistingFont);

            var tkFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"));
            Assert.NotNull(tkFont);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "Mock", "Resources", "Lato-Black.ttf"), tkFont.FilePath);
            Assert.Equal(0, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));

            tkFont.FontSize = 24;
            Assert.Equal(24, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));

            var sizeSmall = tkFont.MeasureString("Test");
            Assert.Equal(new Vec2(64, 32), sizeSmall);

            tkFont.FontSize = 100;
            Assert.Equal(100, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));

            var sizeBig = tkFont.MeasureString("Test");
            Assert.Equal(new Vec2(252, 128), sizeBig);

            tkFont.Draw("Hello", new Vec2(100, 100), Color.CapeHoney);
        }

        [Fact]
        public void LoadTKFontWithSize()
        {
            var typeO = Utility.CreateTypeO();
            typeO.Start();
            Utility.CreateTKWindow(typeO);

            var tkFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"), 25);
            Assert.NotNull(tkFont);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "Mock", "Resources", "Lato-Black.ttf"), tkFont.FilePath);
            Assert.Equal(25, tkFont.FontSize);
            Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));
        }

        [Fact]
        public void SaveScreenshotTextureToDisk()
        {
            var typeO = Utility.DrawTest((context) =>
            {
                context.Game.Scenes.Canvas.Clear(Color.Brown);
                context.Game.Scenes.Canvas.Present();

                var texture = context.Game.Scenes.Canvas.Screenshot();
                Assert.NotNull(texture);
                Assert.Equal(Color.Brown, texture.PixelAt(0, 0));

                texture.Save("tmp/output.png");
                Assert.True(Path.Exists("tmp/output.png"));

                var loadedTexture = context.Game.ContentLoader.LoadContent<Texture>("tmp/output.png");
                Assert.NotNull(loadedTexture);
                Assert.Equal(Color.Brown, loadedTexture.PixelAt(0, 0));

                File.Delete("tmp/output.png");

                context.Exit();
            });
            typeO.Start();
        }
    }
}
