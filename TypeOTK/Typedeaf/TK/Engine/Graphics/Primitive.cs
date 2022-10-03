using OpenTK.Graphics.OpenGL4;
using TypeOEngine.Typedeaf.TK.Engine.Graphics.Interfaces;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Base class for GL Primitives
        /// </summary>
        public class Primitive<T> where T : struct, IVertex
        {
            /// <summary>
            /// Verticies associated with the Primitive
            /// </summary>
            public T[] Vertices { get; protected set; }
            /// <summary>
            /// Determines the way that the vertexes will be drawn in the shader
            /// </summary>
            public PrimitiveDrawType PrimitiveDrawType { get; set; }
            /// <summary>
            /// OpenGL PrimitiveType set by PrimitiveDrawType in PreDraw
            /// </summary>
            protected PrimitiveType PrimitiveType { get; private set; }
            private int VertexArrayID { get; set; }
            private int VertexBufferID { get; set; }

            /// <summary>
            /// Create a uninitialized Primitive of specified length
            /// </summary>
            /// <param name="vertexCount">The number of vertices</param>
            public Primitive(int vertexCount)
            {
                Vertices = new T[vertexCount];

                VertexArrayID = GL.GenVertexArray();
                VertexBufferID = GL.GenBuffer();

                PrimitiveDrawType = PrimitiveDrawType.Line;
            }

            /// <summary>
            /// Draws the primitive by perforing: pre, draw and post methods
            /// </summary>
            public void Draw()
            {
                PreDraw();
                InternalDraw();
                PostDraw();
            }

            /// <summary>
            /// Called before InternalDraw() used to prepare the draw function
            /// </summary>
            protected virtual void PreDraw()
            {
                switch (PrimitiveDrawType)
                {
                    case PrimitiveDrawType.Point:
                        PrimitiveType = PrimitiveType.Points;
                        break;
                    case PrimitiveDrawType.Line:
                        PrimitiveType = PrimitiveType.LineLoop;
                        break;
                    case PrimitiveDrawType.Triangle:
                        PrimitiveType = PrimitiveType.Triangles;
                        break;
                    default:
                        PrimitiveType = PrimitiveType.LineLoop;
                        break;
                }

                if (Vertices.Length == 0) return;

                GL.BindVertexArray(VertexArrayID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, Vertices[0].Size, 0);
                GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * Vertices[0].Size, Vertices, BufferUsageHint.DynamicDraw);
            }

            /// <summary>
            /// Draws the primitive
            /// </summary>
            protected virtual void InternalDraw()
            {
                GL.DrawArrays(PrimitiveType, 0, Vertices.Length);
            }

            /// <summary>
            /// Called after InternalDraw() used to cleanup the draw function
            /// </summary>
            protected virtual void PostDraw()
            {
                GL.DisableVertexAttribArray(0);
            }
        }
    }
}