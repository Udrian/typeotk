using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Text;
using TypeOEngine.Typedeaf.Core.Common;

namespace TypeOEngine.Typedeaf.TK
{
    namespace Engine.Graphics
    {
        public class Shader
        {
            public string VertexShaderSourcePath { get; private set; }
            public string VertexShaderSource { get; private set; }
            public string FragmentShaderSourcePath { get; private set; }
            public string FragmentShaderSource { get; private set; }

            public int Handle { get; private set; }

            private readonly Dictionary<string, int> UniformLocations;

            public Shader(string vertexShaderSourcePath, string fragmentShaderSourcePath)
            {
                Handle = GL.CreateProgram();
                var vertexshader = LoadVertexShader(vertexShaderSourcePath);
                var fragmentshader = LoadFragmentShader(fragmentShaderSourcePath);

                GL.AttachShader(Handle, vertexshader);
                GL.AttachShader(Handle, fragmentshader);
                GL.LinkProgram(Handle);
                GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int success);
                if (success == 0)
                {
                    throw new Exception("Error: ");
                    return;
                }
                GL.DetachShader(Handle, vertexshader);
                GL.DetachShader(Handle, fragmentshader);
                GL.DeleteShader(fragmentshader);
                GL.DeleteShader(vertexshader);

                UniformLocations = new Dictionary<string, int>();
                GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
                for (var i = 0; i < numberOfUniforms; i++)
                {
                    var key = GL.GetActiveUniform(Handle, i, out _, out _);
                    var location = GL.GetUniformLocation(Handle, key);
                    UniformLocations.Add(key, location);
                }
            }

            private int LoadVertexShader(string vertexShaderSourcePath)
            {
                VertexShaderSourcePath = vertexShaderSourcePath;
                var source = "";
                var shader = LoadShader(VertexShaderSourcePath, ref source, ShaderType.VertexShader);
                VertexShaderSource = source;
                return shader;
            }

            private int LoadFragmentShader(string fragmentShaderSourcePath)
            {
                FragmentShaderSourcePath = fragmentShaderSourcePath;
                var source = "";
                var shader = LoadShader(FragmentShaderSourcePath, ref source, ShaderType.FragmentShader);
                FragmentShaderSource = source;
                return shader;
            }

            private int LoadShader(string path, ref string source, ShaderType shaderType)
            {
                if (string.IsNullOrEmpty(path)) return -1;

                using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
                {
                    source = reader.ReadToEnd();
                }
                if (string.IsNullOrEmpty(source)) return -1;

                var shader = GL.CreateShader(shaderType);
                GL.ShaderSource(shader, source);

                GL.CompileShader(shader);
                GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(shader);
                    throw new Exception("Error: " + infoLog);
                    return -1;
                }

                return shader;
            }

            public void Set(string name, int value)
            {
                Use();
                GL.Uniform1(UniformLocations[name], value);
                CheckError();
            }

            public void Set(string name, float value)
            {
                Use();
                GL.Uniform1(UniformLocations[name], value);
                CheckError();
            }

            public void Set(string name, Color value)
            {
                Use();
                GL.Uniform4(UniformLocations[name], new Color4(value.Rf, value.Gf, value.Bf, value.Af));
                CheckError();
            }

            public void Set(string name, double x, double y, double z, double w)
            {
                Use();
                GL.Uniform4(UniformLocations[name], x, y, z, w);
                CheckError();
            }

            public void Set(string name, Vec3 value)
            {
                Use();
                GL.Uniform3(UniformLocations[name], value.X, value.Y, value.Z);
                CheckError();
            }

            public void Set(string name, Matrix4 matrix)
            {
                Use();
                GL.UniformMatrix4(UniformLocations[name], true, ref matrix);
                CheckError();
            }

            private void CheckError()
            {
                var error = GL.GetError();
                if (error != ErrorCode.NoError)
                {
                    throw new Exception("Error: " + error.ToString());
                }
            }

            public void Use()
            {
                GL.UseProgram(Handle);
            }
        }
    }
}