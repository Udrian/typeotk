using OpenTK.Graphics.OpenGL4;
using TypeOEngine.Typedeaf.Core.Common;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics.Primitives
    {
        /// <summary>
        /// Textured GL primitive
        /// </summary>
        public class TexturedPrimitive : IndicedPrimitive<TexturedVertex>
        {
            private Vec2 size;
            /// <summary>
            /// Width and Height of the Rectangle
            /// </summary>
            public Vec2 Size
            {
                get { return size; }
                set
                {
                    size = value;

                    Vertices[0].Position = Vertices[0].Position.SetX(size.X);
                    Vertices[1].Position = Vertices[1].Position.SetX(size.X);
                    Vertices[1].Position = Vertices[1].Position.SetY(size.Y);
                    Vertices[2].Position = Vertices[2].Position.SetY(size.Y);
                }
            }

            /// <summary>
            /// Rectangle primitive with a width and height of 1
            /// </summary>
            public TexturedPrimitive() : this(new Vec2(1))
            {
            }

            /// <summary>
            /// Rectangle primitive with a specified size
            /// </summary>
            /// <param name="size">Width and Height of the Rectangle</param>
            public TexturedPrimitive(Vec2 size) : base(4, 0)
            {
                Size = size;

                Vertices[0].TexCoord = new Vec2(1, 0);
                Vertices[1].TexCoord = new Vec2(1, 1);
                Vertices[2].TexCoord = new Vec2(0, 1);
                Vertices[3].TexCoord = new Vec2(0, 0);

                Indices = new uint[]{
                    0, 1, 3,
                    1, 2, 3
                };
            }

            /// <inheritdoc/>
            protected override void PreDraw()
            {
                base.PreDraw();
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Double, false, Vertices[0].Size, 3 * sizeof(double));
            }

            /// <inheritdoc/>
            protected override void PostDraw()
            {
                base.PostDraw();
                GL.DisableVertexAttribArray(1);
            }
        }
    }
}