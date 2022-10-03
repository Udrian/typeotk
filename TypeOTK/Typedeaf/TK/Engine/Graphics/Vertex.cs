using TypeOEngine.Typedeaf.Core.Common;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Default Vertex implementation, contains Position
        /// </summary>
        public struct Vertex
        {
            /// <inheritdoc/>
            public static int Size { get { return sizeof(double) * 3; } }

            /// <summary>
            /// Vertex Position
            /// </summary>
            public Vec3 Position { get; set; }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="position">Vertex Position</param>
            public Vertex(Vec3 position)
            {
                Position = position;
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            public Vertex(double x, double y, double z)
            {
                Position = new Vec3(x, y, z);
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="position">Vertex Position</param>
            public Vertex(Vec2 position)
            {
                Position = new Vec3(position);
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public Vertex(double x, double y)
            {
                Position = new Vec3(x, y, 0);
            }
        }
    }
}