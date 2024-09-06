using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Contents
    {
        /// <summary>
        /// OpenTK implementation of Texture
        /// </summary>
        public class TKTexture : Texture
        {
            private int Handle { get; set; }
            private Image<Rgba32> RgbaImage { get; set; }

            /// <inheritdoc/>
            protected override void Load(string path)
            {
                Handle = GL.GenTexture();
                Use();

                Load(Image.Load<Rgba32>(path));
            }

            /// <inheritdoc/>
            protected override void Create(Vec2i size, ReadOnlySpan<byte> data)
            {
                Handle = GL.GenTexture();
                Use();

                Load(Image.LoadPixelData<Rgba32>(data, size.X, size.Y));
            }

            private void Load(Image<Rgba32> image)
            {
                RgbaImage = image;
                Size = new Vec2i(RgbaImage.Width, RgbaImage.Height);

                //Convert ImageSharp's format into a byte array, so we can use it with OpenGL.
                var pixels = new List<byte>(4 * RgbaImage.Width * RgbaImage.Height);
                for (int y = 0; y < RgbaImage.Height; y++)
                {
                    for (int x = 0; x < RgbaImage.Width; x++)
                    {
                        pixels.Add(RgbaImage[x, y].R);
                        pixels.Add(RgbaImage[x, y].G);
                        pixels.Add(RgbaImage[x, y].B);
                        pixels.Add(RgbaImage[x, y].A);
                    }
                }

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, RgbaImage.Width, RgbaImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }

            /// <inheritdoc/>
            protected override void Cleanup()
            {
                RgbaImage.Dispose();
            }

            /// <summary>
            /// Activate the usage of the texture in OpenGL
            /// </summary>
            /// <param name="unit">Shader TextureUnit to bind the texture to</param>
            public void Use(TextureUnit unit = TextureUnit.Texture0)
            {
                GL.ActiveTexture(unit);
                GL.BindTexture(TextureTarget.Texture2D, Handle);
            }

            /// <inheritdoc/>
            public override void Save(string path)
            {
                if(!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                RgbaImage.Save(path);
            }

            /// <inheritdoc/>
            public override Color PixelAt(int x, int y)
            {
                var pixel = RgbaImage[x, y];
                return new Color(pixel.A, pixel.R, pixel.G, pixel.B);
            }

            /// <inheritdoc/>
            public override void FlipVertical()
            {
                RgbaImage.Mutate(x => x.Flip(FlipMode.Vertical));
            }

            /// <inheritdoc/>
            public override void FlipHorizontal()
            {
                RgbaImage.Mutate(x => x.Flip(FlipMode.Horizontal));
            }
        }
    }
}