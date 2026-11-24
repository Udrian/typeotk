using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeOEngine.Typedeaf.Core.Engine;

namespace TypeDTK.View.Viewer
{
    /// <summary>
    /// Logger attached to the game hosted inside the editor viewer. Messages are sent to the game log in background batches.
    /// </summary>
    internal class EditorGameLogger : DefaultLogger
    {
        private IGameLogModel GameLogModel { get; } = ViewModelBase.ResourceModel?.Get<IGameLogModel>();

        protected override bool ShouldQueueFilteredMessage(LogLevel level)
        {
            return level != LogLevel.None;
        }

        protected override void ProcessLogBatch(IReadOnlyList<LogEntry> batch)
        {
            if (GameLogModel == null)
                return;

            var entries = new List<GameLogEntry>(batch.Count);
            foreach (var logEntry in batch)
            {
                if (logEntry.Level == LogLevel.None)
                    continue;

                entries.Add(new GameLogEntry
                {
                    Time = DateTime.Now,
                    Level = (GameLogLevel)(int)logEntry.Level,
                    Message = logEntry.Message ?? string.Empty
                });
            }

            if (entries.Count == 0)
                return;

            if (GameLogModel is IBatchedGameLogModel batchedGameLogModel)
            {
                batchedGameLogModel.AddRange(entries);
                return;
            }

            foreach (var entry in entries)
                GameLogModel.Add(entry.Level, entry.Message);
        }
    }
}
