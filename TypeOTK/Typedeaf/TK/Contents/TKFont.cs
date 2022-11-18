using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using QuickFont;
using QuickFont.Configuration;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;

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
            public override void Load(string path, ContentLoader contentLoader)
            {
                Load(path);
            }

            private void Load(string path)
            {
                QFont = new QFont(path, FontSize, new QFontBuilderConfiguration());
                var error = GL.GetError();
                if (error != ErrorCode.NoError && error != ErrorCode.InvalidEnum)
                {
                    throw new Exception("Error: " + error.ToString());
                }
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

            public void Draw(Matrix4 projection, string text, Vec2 position, Color color)
            {
                Drawing.ProjectionMatrix = projection * Matrix4.CreateScale(1, -1, 1);
                Drawing.Print(QFont, text, new Vector3((float)position.X, (float)position.Y, 0), QFontAlignment.Left, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
                var error = GL.GetError();
                if (error != ErrorCode.NoError && error != ErrorCode.InvalidEnum)
                {
                    throw new Exception("Error: " + error.ToString());
                }
            }
        }
    }
}