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
using TypeOTK.Typedeaf.TK.Engine.Graphics;
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
            private sealed class StencilBuffer
            {
                public int RenderbufferId { get; set; }
                public int Width { get; set; }
                public int Height { get; set; }
                public int Samples { get; set; }
                public bool UsesPackedDepthStencil { get; set; }
                public FramebufferAttachment Attachment { get; set; } = FramebufferAttachment.StencilAttachment;
                public int LeaseCount { get; set; }
                public DepthAttachment DepthAttachmentToRestore { get; set; }
            }

            private sealed class DepthAttachment
            {
                public int ObjectType { get; set; }
                public int ObjectName { get; set; }
                public int TextureLevel { get; set; }
                public int TextureCubeMapFace { get; set; }
                public int TextureLayer { get; set; }
                public bool IsLayered { get; set; }
            }

            private readonly Dictionary<int, StencilBuffer> StencilBuffers = new Dictionary<int, StencilBuffer>();

            /// <summary>
            /// TKGame attached to the Canvas
            /// </summary>
            public TKGameWindow TKGame { get; set; }

            /// <summary>Gets the shader used for untextured primitives.</summary>
            public Shader Shader { get; private set; }
            /// <summary>Gets the shader used for textured drawing.</summary>
            public Shader TextureShader { get; private set; }

            /// <summary>Gets or sets the view matrix.</summary>
            public Matrix4 ViewMatrix { get; set; }
            /// <summary>Gets or sets the projection matrix.</summary>
            public Matrix4 ProjectionMatrix { get; set; }

            /// <summary>Gets or sets the game this canvas belongs to.</summary>
            public Game Game { get; set; }

            /// <inheritdoc/>
            public TKCanvas(IWindow window, Rectangle viewport, TKGameWindow tKGame) : base(window, viewport)
            {
                Window = window;
                Viewport = viewport;
                TKGame = tKGame;
            }

            /// <inheritdoc/>
            protected override void Initialize()
            {
                TKGLHelper.ContextGeneration++;

                Shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
                TextureShader = new Shader("Shaders/tshader.vert", "Shaders/tshader.frag");
                
                ViewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f);
                UpdateProjection();

                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                TKGLHelper.CheckGLError();
            }

            /// <inheritdoc/>
            protected override void Cleanup()
            {
                Shader.Cleanup();
                TextureShader.Cleanup();
                foreach (var stencilBuffer in StencilBuffers.Values)
                    GL.DeleteRenderbuffer(stencilBuffer.RenderbufferId);
                StencilBuffers.Clear();

                TKGLHelper.CheckGLError();
            }

            /// <summary>
            /// Temporarily ensures the currently bound framebuffer has a stencil attachment.
            /// </summary>
            /// <returns><see langword="true"/> if the framebuffer has stencil storage; otherwise, <see langword="false"/>.</returns>
            /// <remarks>Pair a successful call with <see cref="ReleaseStencilBuffer"/>.</remarks>
            public bool EnsureStencilBuffer()
            {
                GL.GetInteger(GetPName.FramebufferBinding, out var framebuffer);
                if (framebuffer == 0)
                    return false;

                GL.GetFramebufferAttachmentParameter(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    FramebufferParameterName.FramebufferAttachmentObjectType,
                    out var stencilAttachmentType);
                if (stencilAttachmentType != (int)FramebufferAttachmentObjectType.None)
                {
                    if (StencilBuffers.TryGetValue(framebuffer, out var attachedBuffer)
                        && stencilAttachmentType == (int)FramebufferAttachmentObjectType.Renderbuffer)
                    {
                        GL.GetFramebufferAttachmentParameter(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.StencilAttachment,
                            FramebufferParameterName.FramebufferAttachmentObjectName,
                            out var attachedRenderbuffer);
                        if (attachedRenderbuffer == attachedBuffer.RenderbufferId)
                            attachedBuffer.LeaseCount++;
                    }
                    return true;
                }

                var viewport = new int[4];
                GL.GetInteger(GetPName.Viewport, viewport);
                var width = viewport[2];
                var height = viewport[3];
                if (width <= 0 || height <= 0)
                    return false;
                GL.GetInteger(GetPName.Samples, out var samples);

                var hasCachedBuffer = StencilBuffers.TryGetValue(framebuffer, out var stencilBuffer);
                if (!hasCachedBuffer)
                {
                    stencilBuffer = new StencilBuffer
                    {
                        RenderbufferId = GL.GenRenderbuffer()
                    };
                    StencilBuffers.Add(framebuffer, stencilBuffer);
                }

                GL.GetInteger(GetPName.RenderbufferBinding, out var previousRenderbuffer);
                try
                {
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, stencilBuffer.RenderbufferId);
                    if (stencilBuffer.Width != width || stencilBuffer.Height != height || stencilBuffer.Samples != samples)
                    {
                        AllocateStencilStorage(stencilBuffer, width, height, samples);
                        stencilBuffer.Width = width;
                        stencilBuffer.Height = height;
                        stencilBuffer.Samples = samples;
                    }

                    if (stencilBuffer.UsesPackedDepthStencil)
                    {
                        GL.GetFramebufferAttachmentParameter(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.DepthAttachment,
                            FramebufferParameterName.FramebufferAttachmentObjectType,
                            out var depthAttachmentType);
                        stencilBuffer.Attachment = depthAttachmentType == (int)FramebufferAttachmentObjectType.None
                            ? FramebufferAttachment.DepthStencilAttachment
                            : FramebufferAttachment.StencilAttachment;
                    }

                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        stencilBuffer.Attachment,
                        RenderbufferTarget.Renderbuffer,
                        stencilBuffer.RenderbufferId);

                    var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (status == FramebufferErrorCode.FramebufferUnsupported)
                    {
                        if (!stencilBuffer.UsesPackedDepthStencil)
                        {
                            stencilBuffer.UsesPackedDepthStencil = true;
                            AllocateStencilStorage(stencilBuffer, width, height, samples);
                        }

                        GL.GetFramebufferAttachmentParameter(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.DepthAttachment,
                            FramebufferParameterName.FramebufferAttachmentObjectType,
                            out var depthAttachmentType);
                        stencilBuffer.Attachment = depthAttachmentType == (int)FramebufferAttachmentObjectType.None
                            ? FramebufferAttachment.DepthStencilAttachment
                            : FramebufferAttachment.StencilAttachment;
                        GL.FramebufferRenderbuffer(
                            FramebufferTarget.Framebuffer,
                            stencilBuffer.Attachment,
                            RenderbufferTarget.Renderbuffer,
                            stencilBuffer.RenderbufferId);
                        status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

                        if (status == FramebufferErrorCode.FramebufferUnsupported
                            && stencilBuffer.Attachment == FramebufferAttachment.StencilAttachment)
                        {
                            stencilBuffer.DepthAttachmentToRestore = CaptureDepthAttachment();
                            if (stencilBuffer.DepthAttachmentToRestore != null)
                            {
                                stencilBuffer.Attachment = FramebufferAttachment.DepthStencilAttachment;
                                GL.FramebufferRenderbuffer(
                                    FramebufferTarget.Framebuffer,
                                    stencilBuffer.Attachment,
                                    RenderbufferTarget.Renderbuffer,
                                    stencilBuffer.RenderbufferId);
                                status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                            }
                        }
                    }

                    if (status != FramebufferErrorCode.FramebufferComplete)
                    {
                        GL.FramebufferRenderbuffer(
                            FramebufferTarget.Framebuffer,
                            stencilBuffer.Attachment,
                            RenderbufferTarget.Renderbuffer,
                            0);
                        RestoreDepthAttachment(stencilBuffer.DepthAttachmentToRestore);
                        stencilBuffer.DepthAttachmentToRestore = null;
                        throw new InvalidOperationException($"Could not add stencil storage to the canvas framebuffer: {status}.");
                    }

                    stencilBuffer.LeaseCount++;
                }
                finally
                {
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, previousRenderbuffer);
                }

                return true;
            }

            /// <summary>
            /// Releases the stencil attachment acquired by <see cref="EnsureStencilBuffer"/>.
            /// </summary>
            public void ReleaseStencilBuffer()
            {
                GL.GetInteger(GetPName.FramebufferBinding, out var framebuffer);
                if (!StencilBuffers.TryGetValue(framebuffer, out var stencilBuffer)
                    || stencilBuffer.LeaseCount == 0)
                    return;

                stencilBuffer.LeaseCount--;
                if (stencilBuffer.LeaseCount > 0)
                    return;

                GL.GetFramebufferAttachmentParameter(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    FramebufferParameterName.FramebufferAttachmentObjectType,
                    out var stencilAttachmentType);
                if (stencilAttachmentType != (int)FramebufferAttachmentObjectType.Renderbuffer)
                    return;

                GL.GetFramebufferAttachmentParameter(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    FramebufferParameterName.FramebufferAttachmentObjectName,
                    out var attachedRenderbuffer);
                if (attachedRenderbuffer != stencilBuffer.RenderbufferId)
                    return;

                GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    stencilBuffer.Attachment,
                    RenderbufferTarget.Renderbuffer,
                    0);
                RestoreDepthAttachment(stencilBuffer.DepthAttachmentToRestore);
                stencilBuffer.DepthAttachmentToRestore = null;

                var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status != FramebufferErrorCode.FramebufferComplete)
                    throw new InvalidOperationException($"Could not restore the canvas framebuffer after clipping: {status}.");
            }

            private static DepthAttachment CaptureDepthAttachment()
            {
                GL.GetFramebufferAttachmentParameter(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    FramebufferParameterName.FramebufferAttachmentObjectType,
                    out var objectType);
                if (objectType == (int)FramebufferAttachmentObjectType.None)
                    return null;

                GL.GetFramebufferAttachmentParameter(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    FramebufferParameterName.FramebufferAttachmentObjectName,
                    out var objectName);
                var attachment = new DepthAttachment { ObjectType = objectType, ObjectName = objectName };
                if (objectType == (int)FramebufferAttachmentObjectType.Texture)
                {
                    GL.GetFramebufferAttachmentParameter(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        FramebufferParameterName.FramebufferAttachmentTextureLevel,
                        out var textureLevel);
                    GL.GetFramebufferAttachmentParameter(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        FramebufferParameterName.FramebufferAttachmentTextureCubeMapFace,
                        out var textureCubeMapFace);
                    GL.GetFramebufferAttachmentParameter(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        FramebufferParameterName.FramebufferAttachmentTextureLayer,
                        out var textureLayer);
                    GL.GetFramebufferAttachmentParameter(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        FramebufferParameterName.FramebufferAttachmentLayered,
                        out var isLayered);
                    attachment.TextureLevel = textureLevel;
                    attachment.TextureCubeMapFace = textureCubeMapFace;
                    attachment.TextureLayer = textureLayer;
                    attachment.IsLayered = isLayered != 0;
                }

                return attachment;
            }

            private static void RestoreDepthAttachment(DepthAttachment attachment)
            {
                if (attachment == null)
                    return;

                if (attachment.ObjectType == (int)FramebufferAttachmentObjectType.Renderbuffer)
                {
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        RenderbufferTarget.Renderbuffer,
                        attachment.ObjectName);
                    return;
                }

                if (attachment.ObjectType != (int)FramebufferAttachmentObjectType.Texture)
                    throw new InvalidOperationException($"Cannot restore framebuffer depth attachment of type {attachment.ObjectType}.");

                if (attachment.IsLayered)
                {
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        attachment.ObjectName,
                        attachment.TextureLevel);
                }
                else if (attachment.TextureCubeMapFace != 0)
                {
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        (TextureTarget)attachment.TextureCubeMapFace,
                        attachment.ObjectName,
                        attachment.TextureLevel);
                }
                else if (attachment.TextureLayer > 0)
                {
                    GL.FramebufferTextureLayer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        attachment.ObjectName,
                        attachment.TextureLevel,
                        attachment.TextureLayer);
                }
                else
                {
                    GL.FramebufferTexture(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        attachment.ObjectName,
                        attachment.TextureLevel);
                }
            }

            private static void AllocateStencilStorage(StencilBuffer stencilBuffer, int width, int height, int samples)
            {
                var format = stencilBuffer.UsesPackedDepthStencil
                    ? RenderbufferStorage.Depth24Stencil8
                    : RenderbufferStorage.StencilIndex8;
                if (samples > 0)
                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, format, width, height);
                else
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, width, height);
            }

            /// <inheritdoc/>
            public override void Clear(Color clearColor)
            {
                GL.ClearColor(clearColor.Rf, clearColor.Gf, clearColor.Bf, clearColor.Af);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                TKGLHelper.CheckGLError("", ErrorCode.InvalidEnum);
            }

            /// <inheritdoc/>
            public override void Present()
            {
                GL.Finish();
                TKGame?.Context.SwapBuffers();

                TKGLHelper.CheckGLError();
            }

            /// <inheritdoc/>
            public override void PreDraw()
            {
                ViewMatrix = Matrix4.CreateTranslation(-(float)WorldTranslation.X, -(float)WorldTranslation.Y, -(float)WorldTranslation.Z);
                if (WorldScale != 1)
                    ViewMatrix *= Matrix4.CreateScale((float)WorldScale, (float)WorldScale, 1);
            }

            /// <summary>
            /// Rebuilds the projection so one world unit maps to one viewport unit, e.g. after the viewport was resized.
            /// </summary>
            public void UpdateProjection()
            {
                //TODO: Take a second look at this, shouldn't have to but left and top on -1
                ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(-1, (float)Viewport.Size.X - 1, (float)Viewport.Size.Y, -1, -1.0f, 100.0f);
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

                TKGLHelper.CheckGLError();

                var texture = Game.ContentLoader.CreateTexture<TKTexture>(new Vec2i((int)screenRect.Size.X, (int)screenRect.Size.Y), data);
                texture.FlipVertical();

                return texture;
            }
        }
    }
}
