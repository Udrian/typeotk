namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics.Primitives
    {
        /// <summary>
        /// Determines the way that primitives are drawn
        /// </summary>
        public enum PrimitiveDrawType
        {
            /// <summary>
            /// Draw vertices as points
            /// </summary>
            Point,
            /// <summary>
            /// Draw lines between vertices
            /// </summary>
            Line,
            /// <summary>
            /// Draw triangles between vertices, use indices to determine triangle order
            /// </summary>
            Triangle
        }
    }
}