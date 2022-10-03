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
            public int VertexArrayID { get; private set; }
            public int VertexBufferID { get; private set; }

            public Primitive(int vertexCount)
            {
                Vertices = new Vertex[vertexCount];

                VertexArrayID = GL.GenVertexArray();
                VertexBufferID = GL.GenBuffer();
            }

            //todo: SHOULD BE INTERNAL
            public void InternalDraw()
            {
                PreDraw();
                Draw();
                PostDraw();
            }

            protected virtual void PreDraw()
            {
                GL.BindVertexArray(VertexArrayID);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferID);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, Vertex.Size, 0);
                GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * Vertex.Size, Vertices, BufferUsageHint.DynamicDraw);
            }

            protected virtual void Draw()
            {
                GL.DrawArrays(PrimitiveType.Lines, 0, Vertices.Length);
            }

            protected virtual void PostDraw()
            {
                GL.DisableVertexAttribArray(0);
            }
        }
    }
}