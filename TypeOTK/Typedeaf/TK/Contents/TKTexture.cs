using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using StbImageWriteSharp;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents;
using TypeOTK.Typedeaf.TK.Engine.Graphics;
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
            private ImageResult RgbaImage { get; set; }
            private byte[] PixelData { get; set; }

            /// <inheritdoc/>
            protected override void Load(string path)
            {
                Handle = GL.GenTexture();
                TKGLHelper.CheckGLError();
                Use();

                using (var stream = File.OpenRead(path))
                {
                    ImageResult image = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                    Load(image);
                }

            }

            /// <inheritdoc/>
            protected override void Create(Vec2i size, byte[] data)
            {
                var expectedLength = checked(size.X * size.Y * 4);
                if (size.X <= 0 || size.Y <= 0 || data.Length != expectedLength)
                    throw new ArgumentException("Pixel data must contain four bytes per texture pixel.", nameof(data));

                Handle = GL.GenTexture();
                Use();

                Size = size;
                PixelData = data;
                UploadPixels();

                TKGLHelper.CheckGLError($"Error creating Texture '{FilePath}'");
            }

            private void Load(ImageResult image)
            {
                RgbaImage = image;
                PixelData = image.Data;
                Size = new Vec2i(RgbaImage.Width, RgbaImage.Height);

                UploadPixels();
            }

            private void UploadPixels()
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Size.X, Size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, PixelData);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                TKGLHelper.CheckGLError($"Error loading Texture '{FilePath}'");
            }

            /// <inheritdoc/>
            protected override void Cleanup()
            {
            }

            /// <summary>
            /// Activate the usage of the texture in OpenGL
            /// </summary>
            /// <param name="unit">Shader TextureUnit to bind the texture to</param>
            public void Use(TextureUnit unit = TextureUnit.Texture0)
            {
                GL.ActiveTexture(unit);
                GL.BindTexture(TextureTarget.Texture2D, Handle);

                TKGLHelper.CheckGLError($"Error binding Texture '{FilePath}'");
            }

            /// <inheritdoc/>
            public override void Save(string path)
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using var stream = File.Create(path);
                new ImageWriter().WritePng(PixelData, Size.X, Size.Y, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
            }

            /// <inheritdoc/>
            public override Color PixelAt(int x, int y)
            {
                var pixelR = PixelData[(y * Size.X + x) * 4 + 0];
                var pixelG = PixelData[(y * Size.X + x) * 4 + 1];
                var pixelB = PixelData[(y * Size.X + x) * 4 + 2];
                var pixelA = PixelData[(y * Size.X + x) * 4 + 3];

                return new Color(pixelA, pixelR, pixelG, pixelB);
            }

            /// <inheritdoc/>
            public override void FlipVertical()
            {
                var rowLength = Size.X * 4;
                var row = new byte[rowLength];
                for (var y = 0; y < Size.Y / 2; y++)
                {
                    var oppositeY = Size.Y - y - 1;
                    System.Buffer.BlockCopy(PixelData, y * rowLength, row, 0, rowLength);
                    System.Buffer.BlockCopy(PixelData, oppositeY * rowLength, PixelData, y * rowLength, rowLength);
                    System.Buffer.BlockCopy(row, 0, PixelData, oppositeY * rowLength, rowLength);
                }
            }

            /// <inheritdoc/>
            public override void FlipHorizontal()
            {
                for (var y = 0; y < Size.Y; y++)
                {
                    for (var x = 0; x < Size.X / 2; x++)
                    {
                        var left = (y * Size.X + x) * 4;
                        var right = (y * Size.X + Size.X - x - 1) * 4;
                        for (var channel = 0; channel < 4; channel++)
                            (PixelData[left + channel], PixelData[right + channel]) = (PixelData[right + channel], PixelData[left + channel]);
                    }
                }
            }
        }
    }
}