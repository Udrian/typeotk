using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOTKTest.TypeTest;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;

namespace TypeOTKTest.TypeOTK;

public class TKTextureTest
{
    private static readonly Color Red = new(255, 0, 0);
    private static readonly Color Blue = new(0, 0, 255);
    private static readonly Color Green = new(0, 255, 0);
    private static readonly Color White = new(255, 255, 255);

    // 2x2: top row red/green, bottom row blue/white (RGBA, row 0 first).
    private static byte[] QuadrantData() => new byte[]
    {
        255, 0, 0, 255,   0, 255, 0, 255,
        0, 0, 255, 255,   255, 255, 255, 255
    };

    private static string TempPng() => Path.Combine(AppContext.BaseDirectory, "tmp", $"{Guid.NewGuid():N}.png");

    [Fact]
    public void CreateTexture_StoresSizeAndPixels()
    {
        Utility.DrawTest((context) =>
        {
            var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());

            Assert.Equal(new Vec2i(2, 2), texture.Size);
            Assert.Equal(Red, texture.PixelAt(0, 0));
            Assert.Equal(Green, texture.PixelAt(1, 0));
            Assert.Equal(Blue, texture.PixelAt(0, 1));
            Assert.Equal(White, texture.PixelAt(1, 1));
            Assert.Equal(Blue, texture.PixelAt(new Vec2i(0, 1)));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void CreateTexture_WithWrongDataLength_Throws()
    {
        Utility.DrawTest((context) =>
        {
            Assert.Throws<ArgumentException>(() =>
                context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), new byte[15]));
            context.Exit();
        }).Start();
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    [InlineData(-1, 2)]
    public void CreateTexture_WithNonPositiveSize_Throws(int width, int height)
    {
        Utility.DrawTest((context) =>
        {
            Assert.Throws<ArgumentException>(() =>
                context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(width, height), new byte[16]));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void FlipVertical_SwapsRows()
    {
        Utility.DrawTest((context) =>
        {
            var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());

            texture.FlipVertical();

            Assert.Equal(Blue, texture.PixelAt(0, 0));
            Assert.Equal(White, texture.PixelAt(1, 0));
            Assert.Equal(Red, texture.PixelAt(0, 1));
            Assert.Equal(Green, texture.PixelAt(1, 1));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void FlipHorizontal_SwapsColumns()
    {
        Utility.DrawTest((context) =>
        {
            var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());

            texture.FlipHorizontal();

            Assert.Equal(Green, texture.PixelAt(0, 0));
            Assert.Equal(Red, texture.PixelAt(1, 0));
            Assert.Equal(White, texture.PixelAt(0, 1));
            Assert.Equal(Blue, texture.PixelAt(1, 1));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Flip_OddSizes_LeavesMiddleUntouched()
    {
        Utility.DrawTest((context) =>
        {
            var data = new byte[3 * 3 * 4];
            for (var i = 0; i < data.Length; i += 4)
            {
                data[i] = (byte)(i / 4);
                data[i + 3] = 255;
            }
            var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(3, 3), data);

            texture.FlipVertical();
            Assert.Equal(new Color(4, 0, 0), texture.PixelAt(1, 1));
            Assert.Equal(new Color(6, 0, 0), texture.PixelAt(0, 0));

            texture.FlipHorizontal();
            Assert.Equal(new Color(4, 0, 0), texture.PixelAt(1, 1));
            Assert.Equal(new Color(8, 0, 0), texture.PixelAt(0, 0));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void SaveAndLoad_KeepsImageOrientation()
    {
        var path = TempPng();
        try
        {
            Utility.DrawTest((context) =>
            {
                var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());
                texture.Save(path);
                Assert.True(File.Exists(path));

                var loaded = context.Game.ContentLoader.LoadContent<TKTexture>(path);

                Assert.NotNull(loaded);
                Assert.Equal(new Vec2i(2, 2), loaded.Size);
                Assert.Equal(Red, loaded.PixelAt(0, 0));
                Assert.Equal(Green, loaded.PixelAt(1, 0));
                Assert.Equal(Blue, loaded.PixelAt(0, 1));
                Assert.Equal(White, loaded.PixelAt(1, 1));
                context.Exit();
            }).Start();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_CreatesMissingDirectories()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "tmp", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "nested", "texture.png");
        try
        {
            Utility.DrawTest((context) =>
            {
                var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());

                texture.Save(path);

                Assert.True(File.Exists(path));
                context.Exit();
            }).Start();
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Use_BindsTextureOnAnyUnit()
    {
        Utility.DrawTest((context) =>
        {
            var texture = context.Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i(2, 2), QuadrantData());

            texture.Use();
            texture.Use(OpenTK.Graphics.OpenGL4.TextureUnit.Texture1);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void ContentLoader_BindsTextureContentToTKTexture()
    {
        Utility.DrawTest((context) =>
        {
            Utility.SetTestContentBasePath(context.TypeO);

            var texture = context.Game.ContentLoader.LoadContent<Texture>(Path.Combine("Mock", "Resources", "texture.png"));

            Assert.IsType<TKTexture>(texture);
            context.Exit();
        }).Start();
    }
}
