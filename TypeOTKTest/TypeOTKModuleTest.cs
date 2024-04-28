using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOTKTest.Mock;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Common;
using System.IO;
using Xunit.Sdk;

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
}