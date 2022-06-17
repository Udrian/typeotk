using OpenTK.Windowing.Desktop;
using TypeOEngine.Typedeaf.Core.Engine.Services;

namespace TypeOEngine.Typedeaf.TK.Engine.Services
{
    /// <summary>
    /// Service that holds a instance to the OpenTK Game window implementation
    /// </summary>
    public class TKGameService : Service
    {
        internal TKGame TKGame { get; set; }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            TKGame = new TKGame(GameWindowSettings.Default, new NativeWindowSettings());
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
        }
    }
}
