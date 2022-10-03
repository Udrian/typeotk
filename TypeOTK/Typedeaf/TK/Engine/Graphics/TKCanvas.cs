using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Graphics;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// TK Implementation of Canvas, contains the supplied TKGame object
        /// </summary>
        public class TKCanvas : Canvas
        {
            /// <summary>
            /// TKGame attached to the Canvas
            /// </summary>
            public TKGameWindow TKGame { get; set; }

            public Shader Shader { get; private set; }

            public Matrix4 ViewMatrix { get; set; }
            public Matrix4 ProjectionMatrix { get; set; }

            /// <inheritdoc/>
            public TKCanvas(IWindow window, Rectangle viewport, TKGameWindow tKGame) : base(window, viewport, new Matrix())
            {
                Window = window;
                Viewport = viewport;
                WorldMatrix = new Matrix();
                TKGame = tKGame;

                Shader = new Shader("shader.vert", "shader.frag");
                Shader.Use();

                ViewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
                ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, (float)viewport.Size.X, (float)viewport.Size.Y, 0, -1.0f, 100.0f);
            }
             
            /// <inheritdoc/>
            public override void Clear(Color clearColor)
            {
                GL.ClearColor(clearColor.Rf, clearColor.Gf, clearColor.Bf, clearColor.Af);
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }

            /// <inheritdoc/>
            public override void Present()
            {
                TKGame.Context.SwapBuffers();
            }
        }
    }
}
