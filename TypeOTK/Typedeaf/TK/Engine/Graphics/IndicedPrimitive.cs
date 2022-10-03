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
            /// <summary>
            /// Draw Primitive as wireframe, default to false
            /// </summary>
            public bool Wireframe { get; set; }
            private int ElementBufferID { get; set; }

            public IndicedPrimitive(int vertexCount, int indiceCount) : base(vertexCount)
            {
                Indices = new uint[indiceCount];

                ElementBufferID = GL.GenBuffer();
                Wireframe = false;
            }

            protected override void PreDraw()
            {
                base.PreDraw();

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.DynamicDraw);
            }

            protected override void InternalDraw()
            {
                if(Wireframe)
                {
                    GL.DrawElements(PrimitiveType.LineLoop, Indices.Length, DrawElementsType.UnsignedInt, 0);
                }
                else if(PrimitiveType == PrimitiveType.LineLoop)
                {
                    base.InternalDraw();
                }
                else
                {
                    GL.DrawElements(PrimitiveType, Indices.Length, DrawElementsType.UnsignedInt, 0);
                }
            }
        }
    }
}