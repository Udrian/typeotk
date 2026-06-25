using OpenTK.Graphics.OpenGL4;

namespace TypeOTK.Typedeaf.TK.Engine.Graphics
{
    public class TKGLHelper
    {
        public static void CheckGLError(string message = null, ErrorCode? expectedError = null)
        {
#if DEBUG
            var error = GL.GetError();
            if (error != ErrorCode.NoError && error != expectedError)
            {
                throw new Exception($"OpenGL Error: {error.ToString()}{(string.IsNullOrEmpty(message) ? string.Empty : " - " + message)}");
            }
#endif
        }
    }
}
