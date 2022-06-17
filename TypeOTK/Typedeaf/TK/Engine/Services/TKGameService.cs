using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Engine.Services;

namespace TypeOEngine.Typedeaf.TK.Engine.Services
{
    /// <summary>
    /// Service that holds a instance to the OpenTK Game window implementation
    /// </summary>
    public class TKGameService : Service
    {
        internal List<TKGame> TKGames { get; set; }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            TKGames = new List<TKGame>();
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
        }

        internal TKGame CreateTKGameWindow(NativeWindowSettings nativeWindowSettings)
        {
            var game = new TKGame(GameWindowSettings.Default, nativeWindowSettings);
            TKGames.Add(game);
            game.Run();

            return game;
        }
    }
}
