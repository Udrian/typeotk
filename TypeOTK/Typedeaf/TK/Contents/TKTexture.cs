using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;

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
            public override void Load(string path, ContentLoader contentLoader)
            {
                Handle = GL.GenTexture();
                Use();

                RgbaImage = Image.Load<Rgba32>(path);
                Size = new Vec2(RgbaImage.Width, RgbaImage.Height);

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
        }
    }
}