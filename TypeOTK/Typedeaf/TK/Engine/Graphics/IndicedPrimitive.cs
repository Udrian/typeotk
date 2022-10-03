using OpenTK.Graphics.OpenGL4;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        /// <summary>
        /// Base class for indiced GL primitives
        /// </summary>
        public class IndicedPrimitive : Primitive
        {
            /// <summary>
            /// Verticies indices associated with the Primitive
            /// </summary>
            public uint[] Indices { get; protected set; }
            public int ElementBufferID { get; private set; }

            public IndicedPrimitive(int vertexCount, int indiceCount) : base(vertexCount)
            {
                Indices = new uint[indiceCount];

                ElementBufferID = GL.GenBuffer();
            }

            protected override void PreDraw()
            {
                base.PreDraw();

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.DynamicDraw);
            }

            protected override void Draw()
            {
                GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
            }
        }
    }
}