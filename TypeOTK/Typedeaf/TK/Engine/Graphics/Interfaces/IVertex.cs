namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics.Interfaces
    {
        /// <summary>
        /// Interface for Vertices
        /// </summary>
        public interface IVertex
        {
            /// <summary>
            /// Calculate the memory size of each Vertex
            /// </summary>
            public int Size { get; }
        }
    }
}