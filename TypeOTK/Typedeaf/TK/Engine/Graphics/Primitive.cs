using OpenTK.Graphics.OpenGL4;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Base class for GL Primitives
        /// </summary>
        public class Primitive
        {
            /// <summary>
            /// Verticies associated with the Primitive
            /// </summary>
            public Vertex[] Vertices { get; protected set; }
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

            public Primitive(int vertexCount)
            {
                Vertices = new Vertex[vertexCount];

                VertexArrayID = GL.GenVertexArray();
                VertexBufferID = GL.GenBuffer();

                PrimitiveDrawType = PrimitiveDrawType.Line;
            }

            //todo: SHOULD BE INTERNAL
            public void Draw()
            {
                PreDraw();
                InternalDraw();
                PostDraw();
            }

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

                GL.BindVertexArray(VertexArrayID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, Vertex.Size, 0);
                GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * Vertex.Size, Vertices, BufferUsageHint.DynamicDraw);
            }

            protected virtual void InternalDraw()
            {
                GL.DrawArrays(PrimitiveType, 0, Vertices.Length);
            }

            protected virtual void PostDraw()
            {
                GL.DisableVertexAttribArray(0);
            }
        }
    }
}