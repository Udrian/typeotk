using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TypeOEngine.Typedeaf.Core;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using TypeOEngine.Typedeaf.Core.Engine.Graphics;
using TypeOEngine.Typedeaf.Core.Engine.Graphics.Interfaces;
using TypeOEngine.Typedeaf.Core.Interfaces;
using TypeOEngine.Typedeaf.TK.Contents;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;
using Rectangle = TypeOEngine.Typedeaf.Core.Common.Rectangle;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// TK Implementation of Canvas, contains the supplied TKGame object
        /// </summary>
        public class TKCanvas : Canvas, IHasGame
        {
            /// <summary>
            /// TKGame attached to the Canvas
            /// </summary>
            public TKGameWindow TKGame { get; set; }

            public Shader Shader { get; private set; }
            public Shader TextureShader { get; private set; }

            public Matrix4 ViewMatrix { get; set; }
            public Matrix4 ProjectionMatrix { get; set; }

            public Game Game { get; set; }

            /// <inheritdoc/>
            public TKCanvas(IWindow window, Rectangle viewport, TKGameWindow tKGame) : base(window, viewport)
            {
                Window = window;
                Viewport = viewport;
                TKGame = tKGame;

                Shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
                TextureShader = new Shader("Shaders/tshader.vert", "Shaders/tshader.frag");

                ViewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
                //TODO: Take a second look at this, shouldn't have to but left and top on -1
                ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(-1, (float)viewport.Size.X-1, (float)viewport.Size.Y, -1, -1.0f, 100.0f);

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
                GL.Finish();
                TKGame?.Context.SwapBuffers();
            }

            /// <inheritdoc/>
            public override void PreDraw()
            {
                ViewMatrix = Matrix4.CreateTranslation(-(float)WorldTranslation.X, -(float)WorldTranslation.Y, -(float)WorldTranslation.Z);
                TKFont.Drawing.ProjectionMatrix = ProjectionMatrix * Matrix4.CreateScale(1, -1, 1);
            }

            /// <inheritdoc/>
            public override void PostDraw()
            {
            }

            /// <inheritdoc/>
            public override Texture Screenshot(Rectangle screenRect)
            {
                byte[] data = new byte[(int)screenRect.Size.X * (int)screenRect.Size.Y * 4];
                GL.ReadBuffer(ReadBufferMode.Front);
                GL.ReadPixels((int)screenRect.Pos.X, (int)screenRect.Pos.Y, (int)screenRect.Size.X, (int)screenRect.Size.Y, PixelFormat.Rgba, PixelType.UnsignedByte, data);

                var texture = Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i((int)screenRect.Size.X, (int)screenRect.Size.Y), new ReadOnlySpan<byte>(data));
                texture.FlipVertical();

                return texture;
            }
        }
    }
}
