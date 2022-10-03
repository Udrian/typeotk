using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Engine.Services;
using TypeOEngine.Typedeaf.Core.Interfaces;

namespace TypeOEngine.Typedeaf.TK.Engine.Services
{
    /// <summary>
    /// Service that holds a instance to the OpenTK Game window implementation
    /// </summary>
    public class TKGameService : Service, IUpdatable
    {
        internal List<TKGameWindow> TKGames { get; set; }
        public bool Pause { get; set; }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            Pause = false;
            TKGames = new List<TKGameWindow>();
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
        }

        public TKGameWindow CreateTKGameWindow(NativeWindowSettings nativeWindowSettings)
        {
            var game = new TKGameWindow(nativeWindowSettings);
            TKGames.Add(game);

            return game;
        }

        /// <inheritdoc/>
        public void Update(double dt)
        {
            NativeWindow.ProcessWindowEvents(false);
        }
    }
}
