using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.TK.Engine.Graphics;
using TypeOEngine.Typedeaf.TK.Engine.Graphics.Primitives;

namespace TypeOTKTest.TypeOTK;

public class VertexTest
{
    [Fact]
    public void Vertex_Constructors_SetPosition()
    {
        Assert.Equal(new Vec3(1, 2, 3), new Vertex(new Vec3(1, 2, 3)).Position);
        Assert.Equal(new Vec3(1, 2, 3), new Vertex(1, 2, 3).Position);
        Assert.Equal(new Vec3(4, 5, 0), new Vertex(new Vec2(4, 5)).Position);
        Assert.Equal(new Vec3(4, 5, 0), new Vertex(4, 5).Position);
    }

    [Fact]
    public void Vertex_Size_IsThreeDoubles()
    {
        Assert.Equal(3 * sizeof(double), new Vertex().Size);
    }

    [Fact]
    public void TexturedVertex_Constructors_SetPositionAndTexCoord()
    {
        var fromVectors3 = new TexturedVertex(new Vec3(1, 2, 3), new Vec2(0.5, 0.25));
        var fromScalars3 = new TexturedVertex(1, 2, 3, 0.5, 0.25);
        var fromVectors2 = new TexturedVertex(new Vec2(1, 2), new Vec2(0.5, 0.25));
        var fromScalars2 = new TexturedVertex(1, 2, 0.5, 0.25);

        Assert.Equal(new Vec3(1, 2, 3), fromVectors3.Position);
        Assert.Equal(new Vec3(1, 2, 3), fromScalars3.Position);
        Assert.Equal(new Vec3(1, 2, 0), fromVectors2.Position);
        Assert.Equal(new Vec3(1, 2, 0), fromScalars2.Position);
        Assert.Equal(new Vec2(0.5, 0.25), fromVectors3.TexCoord);
        Assert.Equal(new Vec2(0.5, 0.25), fromScalars3.TexCoord);
        Assert.Equal(new Vec2(0.5, 0.25), fromVectors2.TexCoord);
        Assert.Equal(new Vec2(0.5, 0.25), fromScalars2.TexCoord);
    }

    [Fact]
    public void TexturedVertex_Size_IsFiveDoubles()
    {
        Assert.Equal(5 * sizeof(double), new TexturedVertex().Size);
    }

    [Fact]
    public void Primitive_AllocatesVerticesAndDefaultsToLines()
    {
        var primitive = new Primitive<Vertex>(5);

        Assert.Equal(5, primitive.Vertices.Length);
        Assert.Equal(PrimitiveDrawType.Line, primitive.PrimitiveDrawType);
    }

    [Fact]
    public void Primitive_AcceptsDrawType()
    {
        var primitive = new Primitive<Vertex>(1, PrimitiveDrawType.Point);

        Assert.Equal(PrimitiveDrawType.Point, primitive.PrimitiveDrawType);

        primitive.PrimitiveDrawType = PrimitiveDrawType.Triangle;
        Assert.Equal(PrimitiveDrawType.Triangle, primitive.PrimitiveDrawType);
    }

    [Fact]
    public void IndicedPrimitive_AllocatesVerticesAndIndices()
    {
        var primitive = new IndicedPrimitive<Vertex>(4, 6);

        Assert.Equal(4, primitive.Vertices.Length);
        Assert.Equal(6, primitive.Indices.Length);
        Assert.False(primitive.Wireframe);
    }

    [Fact]
    public void RectanglePrimitive_DefaultsToUnitSquare()
    {
        var rectangle = new RectanglePrimitive();

        Assert.Equal(new Vec2(1), rectangle.Size);
        Assert.Equal(new[] { new Vec3(1, 0, 0), new Vec3(1, 1, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 0) },
            rectangle.Vertices.Select(v => v.Position));
        Assert.Equal(new uint[] { 0, 1, 3, 1, 2, 3 }, rectangle.Indices);
    }

    [Fact]
    public void RectanglePrimitive_Size_MovesVertices()
    {
        var rectangle = new RectanglePrimitive(new Vec2(10, 20));

        Assert.Equal(new Vec2(10, 20), rectangle.Size);
        Assert.Equal(new[] { new Vec3(10, 0, 0), new Vec3(10, 20, 0), new Vec3(0, 20, 0), new Vec3(0, 0, 0) },
            rectangle.Vertices.Select(v => v.Position));

        rectangle.Size = new Vec2(3, 4);
        Assert.Equal(new Vec3(3, 4, 0), rectangle.Vertices[1].Position);
    }

    [Fact]
    public void TexturedPrimitive_DefaultsToUnitSquareWithTexCoords()
    {
        var textured = new TexturedPrimitive();

        Assert.Equal(new Vec2(1), textured.Size);
        Assert.Equal(new[] { new Vec3(1, 0, 0), new Vec3(1, 1, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 0) },
            textured.Vertices.Select(v => v.Position));
        Assert.Equal(new[] { new Vec2(1, 0), new Vec2(1, 1), new Vec2(0, 1), new Vec2(0, 0) },
            textured.Vertices.Select(v => v.TexCoord));
        Assert.Equal(new uint[] { 0, 1, 3, 1, 2, 3 }, textured.Indices);
    }

    [Fact]
    public void TexturedPrimitive_Size_MovesVerticesButKeepsTexCoords()
    {
        var textured = new TexturedPrimitive(new Vec2(8, 16));

        Assert.Equal(new Vec3(8, 16, 0), textured.Vertices[1].Position);
        Assert.Equal(new Vec2(1, 1), textured.Vertices[1].TexCoord);
    }
}
