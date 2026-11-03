using FontStashSharp;
using OpenTK.Graphics.OpenGL4;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Interfaces;
using TypeOEngine.Typedeaf.Core.Entities;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;
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
            private ILogger Logger { get; set; }
            private FontSystem FontSystem { get; set; }
            private SpriteFontBase SpriteFont { get; set; }
            private TKFontRenderer Renderer { get; set; }

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
                Release();

                // Obsolete setting kept to preserve the current glyph rendering behavior.
#pragma warning disable CS0618
                var fontSystem = new FontSystem(new FontSystemSettings { PremultiplyAlpha = false });
#pragma warning restore CS0618
                fontSystem.AddFont(File.ReadAllBytes(path));

                FontSystem = fontSystem;
                Renderer = new TKFontRenderer();
                SpriteFont = fontSystem.GetFont(Math.Max(FontSize, 1));

                TKGLHelper.CheckGLError($"Error loading Font '{FilePath}'");
            }

            /// <inheritdoc/>
            protected override void Cleanup()
            {
                Release();
            }

            private void Release()
            {
                SpriteFont = null;
                FontSystem?.Dispose();
                FontSystem = null;
                Renderer?.Dispose();
                Renderer = null;
            }

            /// <inheritdoc/>
            public override Vec2 MeasureString(string text)
            {
                var size = SpriteFont.MeasureString(text);
                return new Vec2(size.X, size.Y);
            }

            /// <summary>
            /// Draws the text with its top left corner at the position, where the position origin is the bottom left of the viewport
            /// </summary>
            /// <param name="text">Text to draw</param>
            /// <param name="position">Top left position of the text</param>
            /// <param name="color">Color of the text</param>
            /// <param name="flipped">How the text should be flipped</param>
            public void Draw(string text, Vec2 position, Color color, Flipped flipped = Flipped.None)
            {
                Renderer.Begin((float)position.X, (float)position.Y, flipped);
                SpriteFont.DrawText(Renderer, text, System.Numerics.Vector2.Zero, new FSColor(color.R, color.G, color.B, color.A));
                Renderer.End();

                var error = GL.GetError();
                if (error != ErrorCode.NoError && error != ErrorCode.InvalidEnum)
                {
                    Logger.Log(LogLevel.Error, $"Error drawing Font '{FilePath}' with the error: '{error.ToString()}'");
                }
            }
        }
    }
}