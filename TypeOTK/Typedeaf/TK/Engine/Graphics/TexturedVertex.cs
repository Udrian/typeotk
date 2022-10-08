using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.TK.Engine.Graphics.Interfaces;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Default Vertex implementation, contains Position
        /// </summary>
        public struct TexturedVertex : IVertex
        {
            /// <inheritdoc/>
            public int Size { get { return sizeof(double) * 5; } }

            /// <summary>
            /// Vertex Position
            /// </summary>
            public Vec3 Position { get; set; }

            /// <summary>
            /// Vertex Position
            /// </summary>
            public Vec2 TexCoord { get; set; }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="position">Vertex Position</param>
            /// <param name="texCoord">Texture coordinate</param>
            public TexturedVertex(Vec3 position, Vec2 texCoord)
            {
                Position = position;
                TexCoord = texCoord;
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="x">Vertex X pos</param>
            /// <param name="y">Vertex Y pos</param>
            /// <param name="z">Vertex Z pos</param>
            /// <param name="tx">Texture X coordinate</param>
            /// <param name="ty">Texture Y coordinate</param>
            public TexturedVertex(double x, double y, double z, double tx, double ty)
            {
                Position = new Vec3(x, y, z);
                TexCoord = new Vec2(tx, ty);
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="position">Vertex Position</param>
            /// <param name="texCoord">Texture coordinate</param>
            public TexturedVertex(Vec2 position, Vec2 texCoord)
            {
                Position = new Vec3(position);
                TexCoord = texCoord;
            }

            /// <summary>
            /// Vertex Constructor
            /// </summary>
            /// <param name="x">Vertex X pos</param>
            /// <param name="y">Vertex Y pos</param>
            /// <param name="tx">Texture X coordinate</param>
            /// <param name="ty">Texture Y coordinate</param>
            public TexturedVertex(double x, double y, double tx, double ty)
            {
                Position = new Vec3(x, y, 0);
                TexCoord = new Vec2(tx, ty);
            }
        }
    }
}