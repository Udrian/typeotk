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
        /// <summary>
        /// List of currently open Game Windows
        /// </summary>
        public List<TKGameWindow> TKGames { get; private set; }

        /// <inheritdoc/>
        public bool Pause { get; set; }

        /// <inheritdoc/>
        public TKGameService()
        {
            TKGames = new List<TKGameWindow>();
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            Pause = false;
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
        }

        /// <summary>
        /// Creates a new Native TK Game Window
        /// </summary>
        /// <param name="nativeWindowSettings">Window settings</param>
        /// <returns>The newly created window</returns>
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

            for(var i = 0; i < TKGames.Count; i++)
            {
                if (!TKGames[i].Exists || TKGames[i].IsExiting)
                {
                    TKGames[i].Dispose();
                    TKGames.RemoveAt(i);
                    i--;
                }
            }
            if (TKGames.Count == 0)
                Context.Exit();
        }
    }
}
