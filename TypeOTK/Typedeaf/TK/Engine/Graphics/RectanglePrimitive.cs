using TypeOEngine.Typedeaf.Core.Common;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Rectanglugar GL Primitive
        /// </summary>
        public class RectanglePrimitive : IndicedPrimitive
        {
            private Vec2 size;
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

            public RectanglePrimitive() : this(new Vec2(1))
            {
            }

            public RectanglePrimitive(Vec2 size) : base(4, 0)
            {
                Size = size;

                Indices = new uint[]{
                    0, 1, 3,
                    1, 2, 3
                };
            }
        }
    }
}