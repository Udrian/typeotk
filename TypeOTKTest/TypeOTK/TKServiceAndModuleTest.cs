using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using TypeOEngine.Typedeaf.TK;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOEngine.Typedeaf.TK.Engine.Services;
using TypeOTKTest.TypeTest;

namespace TypeOTKTest.TypeOTK;

public class TKServiceAndModuleTest
{
    [Fact]
    public void TKModuleOption_CanBeCreated()
    {
        Assert.NotNull(new TKModuleOption());
    }

    [Fact]
    public void Module_BindsFontContentToTKFont()
    {
        Utility.DrawTest((context) =>
        {
            Utility.SetTestContentBasePath(context.TypeO);

            var font = context.Game.ContentLoader.LoadContent<Font>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"));

            Assert.IsType<TKFont>(font);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void TKFont_MeasureString_GrowsWithTextLength()
    {
        Utility.DrawTest((context) =>
        {
            Utility.SetTestContentBasePath(context.TypeO);
            var font = context.Game.ContentLoader.LoadContent<TKFont>(Path.Combine("Mock", "Resources", "Lato-Black.ttf"), 24);

            var short1 = font.MeasureString("A");
            var long1 = font.MeasureString("AAAAAAAA");

            Assert.True(short1.X > 0);
            Assert.True(long1.X > short1.X);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void GameService_StartsWithoutWindowsAndIsNotPaused()
    {
        var service = new TKGameService();

        Assert.Empty(service.TKGames);
        Assert.False(service.Pause);
    }

    [Fact]
    public void GameService_CreateTKGameWindow_TracksTheWindow()
    {
        Utility.DrawTest((context) =>
        {
            var service = context.GetService<TKGameService>();
            var before = service.TKGames.Count;

            var window = service.CreateTKGameWindow(new OpenTK.Windowing.Desktop.NativeWindowSettings());
            window.MakeCurrent();

            Assert.Equal(before + 1, service.TKGames.Count);
            Assert.Contains(window, service.TKGames);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void GameService_Cleanup_ClosesAndForgetsAllWindows()
    {
        TKGameService service = null;
        var typeO = Utility.DrawTest((context) =>
        {
            service = context.GetService<TKGameService>();
            Assert.NotEmpty(service.TKGames);
            context.Exit();
        });

        typeO.Start();

        Assert.Empty(service.TKGames);
    }
}
