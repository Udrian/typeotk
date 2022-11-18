using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Graphics;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.TK.Contents;

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
            public Shader TextureShader { get; private set; }

            public Matrix4 ViewMatrix { get; set; }
            public Matrix4 ProjectionMatrix { get; set; }

            /// <inheritdoc/>
            public TKCanvas(IWindow window, Rectangle viewport, TKGameWindow tKGame) : base(window, viewport, new Matrix())
            {
                Window = window;
                Viewport = viewport;
                WorldMatrix = new Matrix();
                TKGame = tKGame;

                Shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
                TextureShader = new Shader("Shaders/tshader.vert", "Shaders/tshader.frag");

                ViewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
                ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, (float)viewport.Size.X, (float)viewport.Size.Y, 0, -1.0f, 100.0f);

                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
             
            /// <inheritdoc/>
            public override void Clear(Color clearColor)
            {
                GL.ClearColor(clearColor.Rf, clearColor.Gf, clearColor.Bf, clearColor.Af);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                TKFont.Drawing.DrawingPrimitives.Clear();
                var error = GL.GetError();
                if (error != ErrorCode.NoError && error != ErrorCode.InvalidEnum)
                {
                    throw new Exception("Error: " + error.ToString());
                }
            }

            /// <inheritdoc/>
            public override void Present()
            {

                TKFont.Drawing.RefreshBuffers();
                TKFont.Drawing.Draw();
                TKGame.Context.SwapBuffers();
            }
        }
    }
}
