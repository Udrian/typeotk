using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using QuickFont;
using QuickFont.Configuration;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Interfaces;
using TypeOTK.Typedeaf.TK.Engine.Graphics;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Contents
    {
        /// <summary>
        /// OpenTK implementation of Font
        /// </summary>
        public class TKFont : Font
        {
            /// <summary>
            /// QFontDrawing used to handle the OpenTK implementation of drawing fonts
            /// </summary>
            public static QFontDrawing Drawing = new QFontDrawing();
            private QFont QFont { get; set; }
            private ILogger Logger { get; set; }

            /// <inheritdoc/>
            public override int FontSize
            {
                get => base.FontSize;
                set
                {
                    base.FontSize = value;
                    Load(FilePath);
                }
            }

            /// <inheritdoc/>
            protected override void Load(string path)
            {
                QFont = new QFont(path, FontSize, new QFontBuilderConfiguration());

                TKGLHelper.CheckGLError($"Error loading Font '{FilePath}'");
            }

            /// <inheritdoc/>
            protected override void Cleanup()
            {
                QFont?.Dispose();
            }

            /// <inheritdoc/>
            public override Vec2 MeasureString(string text)
            {
                var size = QFont.Measure(text);
                return new Vec2(size.Width, size.Height);
            }

            public void Draw(string text, Vec2 position, Color color)
            {
                Drawing.Print(QFont, text, new Vector3((float)position.X, (float)position.Y, 0), QFontAlignment.Left, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
                var error = GL.GetError();
                if (error != ErrorCode.NoError && error != ErrorCode.InvalidEnum)
                {
                    Logger.Log(LogLevel.Error, $"Error drawing Font '{FilePath}' with the error: '{error.ToString()}'");
                }
            }
        }
    }
}