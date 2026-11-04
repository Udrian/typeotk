using OpenTK.Mathematics;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Contents.ContentExtensions;
using TypeOEngine.Typedeaf.TK.Contents;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;
using TypeOTK.Typedeaf.TK.Engine.Graphics;
using TypeOTKTest.TypeTest;
using Color = TypeOEngine.Typedeaf.Core.Common.Color;
using GL = OpenTK.Graphics.OpenGL4.GL;

namespace TypeOTKTest.TypeOTK;

public class GraphicsTest
{
    [Fact]
    public void Shader_LoadsProgramAndSources()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;

            Assert.NotEqual(0, canvas.Shader.Handle);
            Assert.Equal("Shaders/shader.vert", canvas.Shader.VertexShaderSourcePath);
            Assert.Equal("Shaders/shader.frag", canvas.Shader.FragmentShaderSourcePath);
            Assert.Contains("void main", canvas.Shader.VertexShaderSource);
            Assert.Contains("void main", canvas.Shader.FragmentShaderSource);

            Assert.NotEqual(0, canvas.TextureShader.Handle);
            Assert.NotEqual(canvas.Shader.Handle, canvas.TextureShader.Handle);
            Assert.Equal("Shaders/tshader.frag", canvas.TextureShader.FragmentShaderSourcePath);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Shader_Set_AcceptsDeclaredUniforms()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;

            canvas.Shader.Set("ourColor", Color.SkyBlue);
            canvas.Shader.Set("model", Matrix4.Identity);
            canvas.Shader.Set("view", Matrix4.Identity);
            canvas.Shader.Set("projection", Matrix4.Identity);
            canvas.TextureShader.Set("texture1", 0);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Shader_Set_UnknownUniform_Throws()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;

            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", 1));
            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", 1f));
            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", Color.White));
            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", Matrix4.Identity));
            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", new Vec3(1, 2, 3)));
            Assert.Throws<KeyNotFoundException>(() => canvas.Shader.Set("doesNotExist", 1, 2, 3, 4));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Shader_WithMissingSourceFile_Throws()
    {
        Utility.DrawTest((context) =>
        {
            Assert.ThrowsAny<Exception>(() => new Shader("Shaders/missing.vert", "Shaders/missing.frag"));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_Initialize_BumpsContextGenerationAndBuildsMatrices()
    {
        Utility.DrawTest((context) =>
        {
            var generation = TKGLHelper.ContextGeneration;

            var canvas = Utility.CreateTKCanvas(context);

            Assert.Equal(generation + 1, TKGLHelper.ContextGeneration);
            var viewport = context.Game.MainWindow.Size;
            Assert.Equal(Matrix4.CreateOrthographicOffCenter(-1, viewport.X - 1, viewport.Y, -1, -1f, 100f), canvas.ProjectionMatrix);
            Assert.Equal(Matrix4.Identity, canvas.ViewMatrix);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_PreDraw_ScalesViewAfterTranslatingByWorldScale()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;
            canvas.WorldTranslation = new Vec3(10, 20, 0);
            canvas.WorldScale = 2;

            canvas.PreDraw();

            // A world point at the translation ends up at the view origin, and distances from it are doubled
            Assert.Equal(new Vector4(0, 0, 0, 1), new Vector4(10, 20, 0, 1) * canvas.ViewMatrix);
            Assert.Equal(new Vector4(10, 20, 0, 1), new Vector4(15, 30, 0, 1) * canvas.ViewMatrix);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_UpdateProjection_FollowsTheViewport()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;
            canvas.Viewport = new Rectangle(0, 0, 320, 240);

            canvas.UpdateProjection();

            Assert.Equal(Matrix4.CreateOrthographicOffCenter(-1, 319, 240, -1, -1f, 100f), canvas.ProjectionMatrix);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_PreDraw_TranslatesViewByNegatedWorldTranslation()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = (TKCanvas)context.Game.Scenes.Canvas;
            canvas.WorldTranslation = new Vec3(5, 6, 7);

            canvas.PreDraw();

            Assert.Equal(Matrix4.CreateTranslation(-5, -6, -7), canvas.ViewMatrix);
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_Screenshot_OfSubRectangle_HasThatSize()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = context.Game.Scenes.Canvas;
            canvas.Clear(Color.DarkCyan);
            canvas.Present();

            var texture = canvas.Screenshot(new Rectangle(new Vec2(2, 3), new Vec2(10, 6)));

            Assert.IsType<TKTexture>(texture);
            Assert.Equal(new Vec2i(10, 6), texture.Size);
            Assert.Equal(Color.DarkCyan, texture.PixelAt(0, 0));
            Assert.Equal(Color.DarkCyan, texture.PixelAt(9, 5));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_PresentWithoutWindow_DoesNotThrow()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = new TKCanvas(context.Game.MainWindow, new Rectangle(new Vec2(), new Vec2(10, 10)), null);

            canvas.Present();
            context.Exit();
        }).Start();
    }

    [Fact]
    public void Canvas_Clear_UsesGivenColor()
    {
        Utility.DrawTest((context) =>
        {
            var canvas = context.Game.Scenes.Canvas;

            canvas.Clear(Color.Orange);
            canvas.Present();
            Assert.Equal(Color.Orange, canvas.Screenshot().PixelAt(0, 0));

            canvas.Clear(Color.SkyBlue);
            canvas.Present();
            Assert.Equal(Color.SkyBlue, canvas.Screenshot().PixelAt(0, 0));
            context.Exit();
        }).Start();
    }

    [Fact]
    public void GLHelper_NoError_DoesNotThrow()
    {
        Utility.DrawTest((context) =>
        {
            GL.GetError();

            TKGLHelper.CheckGLError();
            TKGLHelper.CheckGLError("message");
            context.Exit();
        }).Start();
    }

#if DEBUG
    [Fact]
    public void GLHelper_PendingError_ThrowsWithMessage()
    {
        Utility.DrawTest((context) =>
        {
            GL.GetError();
            GL.Enable((OpenTK.Graphics.OpenGL4.EnableCap)0);

            var exception = Assert.Throws<Exception>(() => TKGLHelper.CheckGLError("while testing"));

            Assert.Contains("InvalidEnum", exception.Message);
            Assert.Contains("while testing", exception.Message);
            // The error flag is consumed by the check
            TKGLHelper.CheckGLError();
            context.Exit();
        }).Start();
    }

    [Fact]
    public void GLHelper_ExpectedError_IsIgnored()
    {
        Utility.DrawTest((context) =>
        {
            GL.GetError();
            GL.Enable((OpenTK.Graphics.OpenGL4.EnableCap)0);

            TKGLHelper.CheckGLError("", OpenTK.Graphics.OpenGL4.ErrorCode.InvalidEnum);
            context.Exit();
        }).Start();
    }
#endif
}
