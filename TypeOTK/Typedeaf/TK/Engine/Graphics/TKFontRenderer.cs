using FontStashSharp.Interfaces;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TypeOEngine.Typedeaf.Core.Entities;
using TypeOTK.Typedeaf.TK.Engine.Graphics;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// OpenTK renderer and texture manager used by FontStashSharp to draw glyph quads
        /// </summary>
        internal sealed class TKFontRenderer : IFontStashRenderer2, ITexture2DManager, IDisposable
        {
            private const string VertexSource = @"#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;
out vec2 texCoord;
out vec4 color;
uniform mat4 projection;
void main(void)
{
    texCoord = aTexCoord;
    color = aColor;
    gl_Position = projection * vec4(aPosition, 0.0, 1.0);
}";

            private const string FragmentSource = @"#version 330 core
in vec2 texCoord;
in vec4 color;
uniform sampler2D texture1;
out vec4 outColor;
void main(void)
{
    outColor = texture(texture1, texCoord) * color;
}";

            private const int FloatsPerVertex = 8;

            private readonly List<int> textures = new List<int>();
            private readonly float[] vertexData = new float[4 * FloatsPerVertex];
            private int program;
            private int projectionLocation;
            private int vao;
            private int vbo;
            private bool resourcesCreated;
            private float offsetX;
            private float offsetY;
            private Flipped currentFlipped;

            public ITexture2DManager TextureManager => this;

            /// <summary>
            /// Sets the origin the glyph quads are drawn relative to. Glyph space is y-down, the screen is y-up.
            /// </summary>
            public void Begin(float x, float y)
            {
                Begin(x, y, Flipped.None);
            }

            /// <summary>
            /// Sets the origin the glyph quads are drawn relative to. Glyph space is y-down, the screen is y-up.
            /// </summary>
            public void Begin(float x, float y, Flipped flipped)
            {
                CreateResources();

                offsetX = x;
                offsetY = y;
                currentFlipped = flipped;

                var viewport = new int[4];
                GL.GetInteger(GetPName.Viewport, viewport);
                var projection = Matrix4.CreateOrthographicOffCenter(0, viewport[2], 0, viewport[3], -1, 1);

                GL.UseProgram(program);
                GL.UniformMatrix4(projectionLocation, false, ref projection);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.BindVertexArray(vao);
            }

            public void End()
            {
                GL.BindVertexArray(0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            public object CreateTexture(int width, int height)
            {
                var handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, handle);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                textures.Add(handle);
                textureSizes[handle] = new System.Drawing.Point(width, height);

                TKGLHelper.CheckGLError("Error creating font texture");
                return handle;
            }

            private readonly Dictionary<int, System.Drawing.Point> textureSizes = new Dictionary<int, System.Drawing.Point>();

            public System.Drawing.Point GetTextureSize(object texture)
            {
                return textureSizes[(int)texture];
            }

            public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
            {
                GL.BindTexture(TextureTarget.Texture2D, (int)texture);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, PixelFormat.Rgba, PixelType.UnsignedByte, data);

                TKGLHelper.CheckGLError("Error uploading font texture data");
            }

            public void DrawQuad(object texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
            {
                // Apply flipping to texture coordinates if needed
                if (currentFlipped != Flipped.None)
                {
                    bool flipHorizontal = currentFlipped == Flipped.Horizontal || currentFlipped == Flipped.Both;
                    bool flipVertical = currentFlipped == Flipped.Vertical || currentFlipped == Flipped.Both;

                    if (flipHorizontal)
                    {
                        // Swap X texture coordinates
                        var tempX = topLeft.TextureCoordinate.X;
                        topLeft.TextureCoordinate.X = topRight.TextureCoordinate.X;
                        topRight.TextureCoordinate.X = tempX;

                        tempX = bottomLeft.TextureCoordinate.X;
                        bottomLeft.TextureCoordinate.X = bottomRight.TextureCoordinate.X;
                        bottomRight.TextureCoordinate.X = tempX;
                    }

                    if (flipVertical)
                    {
                        // Swap Y texture coordinates
                        var tempY = topLeft.TextureCoordinate.Y;
                        topLeft.TextureCoordinate.Y = bottomLeft.TextureCoordinate.Y;
                        bottomLeft.TextureCoordinate.Y = tempY;

                        tempY = topRight.TextureCoordinate.Y;
                        topRight.TextureCoordinate.Y = bottomRight.TextureCoordinate.Y;
                        bottomRight.TextureCoordinate.Y = tempY;
                    }
                }

                // Triangle strip order: top-left, top-right, bottom-left, bottom-right
                WriteVertex(0, ref topLeft);
                WriteVertex(1, ref topRight);
                WriteVertex(2, ref bottomLeft);
                WriteVertex(3, ref bottomRight);

                GL.BindTexture(TextureTarget.Texture2D, (int)texture);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.DynamicDraw);
                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            }

            private void WriteVertex(int index, ref VertexPositionColorTexture vertex)
            {
                var i = index * FloatsPerVertex;
                vertexData[i + 0] = offsetX + vertex.Position.X;
                vertexData[i + 1] = offsetY - vertex.Position.Y;
                vertexData[i + 2] = vertex.TextureCoordinate.X;
                vertexData[i + 3] = vertex.TextureCoordinate.Y;
                vertexData[i + 4] = vertex.Color.R / 255f;
                vertexData[i + 5] = vertex.Color.G / 255f;
                vertexData[i + 6] = vertex.Color.B / 255f;
                vertexData[i + 7] = vertex.Color.A / 255f;
            }

            private void CreateResources()
            {
                if (resourcesCreated) return;

                var vertexShader = CompileShader(ShaderType.VertexShader, VertexSource);
                var fragmentShader = CompileShader(ShaderType.FragmentShader, FragmentSource);

                program = GL.CreateProgram();
                GL.AttachShader(program, vertexShader);
                GL.AttachShader(program, fragmentShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
                if (success == 0)
                {
                    throw new Exception("Error linking font shader: " + GL.GetProgramInfoLog(program));
                }
                GL.DetachShader(program, vertexShader);
                GL.DetachShader(program, fragmentShader);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);

                projectionLocation = GL.GetUniformLocation(program, "projection");

                vao = GL.GenVertexArray();
                vbo = GL.GenBuffer();
                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                var stride = FloatsPerVertex * sizeof(float);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, 4 * sizeof(float));
                GL.BindVertexArray(0);

                resourcesCreated = true;
                TKGLHelper.CheckGLError("Error creating font renderer");
            }

            private static int CompileShader(ShaderType type, string source)
            {
                var shader = GL.CreateShader(type);
                GL.ShaderSource(shader, source);
                GL.CompileShader(shader);
                GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    throw new Exception("Error compiling font shader: " + GL.GetShaderInfoLog(shader));
                }
                return shader;
            }

            public void Dispose()
            {
                foreach (var texture in textures)
                {
                    GL.DeleteTexture(texture);
                }
                textures.Clear();
                textureSizes.Clear();

                if (resourcesCreated)
                {
                    GL.DeleteProgram(program);
                    GL.DeleteBuffer(vbo);
                    GL.DeleteVertexArray(vao);
                    resourcesCreated = false;
                }
            }
        }
    }
}
