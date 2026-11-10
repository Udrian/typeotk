using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeOEngine.Typedeaf.Core.Engine;

namespace TypeDTK.View.Viewer
{
    /// <summary>
    /// Logger attached to the game hosted inside the editor viewer. Works the same as <see cref="DefaultLogger"/>,
    /// but also keeps every message in the editor's game log so it can be shown in the Output panel and in crash reports.
    /// </summary>
    internal class EditorGameLogger : DefaultLogger
    {
        private IGameLogModel GameLogModel { get; } = ViewModelBase.ResourceModel?.Get<IGameLogModel>();

        /// <inheritdoc/>
        public override void Log(LogLevel level, string log)
        {
            // The game log keeps everything; the display filters on level instead
            if (level != LogLevel.None)
                GameLogModel?.Add((GameLogLevel)(int)level, log);
            base.Log(level, log);
        }
    }
}
