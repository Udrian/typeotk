using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOTKTest.Mock;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using TypeOEngine.Typedeaf.Core.Common;
using System.IO;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;

namespace TypeOTKTest;

public partial class TypeOTKModuleTest
{
    [Fact]
    public void LoadModuleTest()
    {
        var typeO = CreateTypeO();
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
        var typeO = CreateTypeO();
        typeO.Start();
        CreateWindow(typeO);
    }

    [Fact]
    public void LoadTKTexture()
    {
        var typeO = CreateTypeO();
        typeO.Start();
        CreateWindow(typeO);

        Assert.Throws<FileNotFoundException>(() =>
        {
            var tkNonExistingTexture = typeO.Context.Game.ContentLoader.LoadContent<TKTexture>(Path.Combine("Mock", "Resources", "notexistingtexture.png"));
            Assert.Null(tkNonExistingTexture);
        });

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
        var typeO = CreateTypeO();
        typeO.Start();
        CreateWindow(typeO);

        Assert.Throws<FileNotFoundException>(() =>
        {
            var tkNonExistingFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "notexistingtexture.ttf"));
            tkNonExistingFont.FontSize = 24;
            Assert.Null(tkNonExistingFont);
        });

        var tkFont = typeO.Context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"));
        Assert.NotNull(tkFont);
        Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "Mock", "Resources", "Lato-Black.ttf"), tkFont.FilePath);
        Assert.False(tkFont.FontLoaded);
        Assert.Equal(0, tkFont.FontSize);
        Assert.Null(Utility.GetProperty<object>(tkFont, "QFont"));

        tkFont.FontSize = 24;
        Assert.True(tkFont.FontLoaded);
        Assert.Equal(24, tkFont.FontSize);
        Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));

        var sizeSmall = tkFont.MeasureString("Test");
        Assert.Equal(new Vec2(64, 32), sizeSmall);

        tkFont.FontSize = 100;
        Assert.True(tkFont.FontLoaded);
        Assert.Equal(100, tkFont.FontSize);
        Assert.NotNull(Utility.GetProperty<object>(tkFont, "QFont"));

        var sizeBig = tkFont.MeasureString("Test");
        Assert.Equal(new Vec2(252, 128), sizeBig);

        tkFont.Draw("Hello", new Vec2(100, 100), Color.CapeHoney);
    }
}