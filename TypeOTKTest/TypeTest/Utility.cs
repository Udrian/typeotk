using System.Reflection;
using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK;
using TypeOTKTest.TypeTest.Mock;

namespace TypeOTKTest.TypeTest
{
    public static partial class Utility
    {
        public static TypeO CreateTypeO(Action<Context> initialize = null, Action<Context> update = null, Action<Context> draw = null, Action<Context> cleanup = null)
        {
            var typeO = TypeO.Create<TestGameMock>(TestGameMock.GameName)
                    .LoadModule<TKModule>() as TypeO;
            Assert.NotNull(typeO);
            var game = typeO.Context.Game as TestGameMock;
            if (game != null)
            {
                game.InitializeAction = initialize;
                game.UpdateAction = update;
                game.DrawAction = draw;
                game.CleanUpAction = cleanup;
            }
            return typeO;
        }

        public static T GetProperty<T>(object obj, string property)
        {
            Type t = obj.GetType();
            return (T)t.InvokeMember(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance, null, obj, new object[] {});
        }
        public static void SetProperty(object obj, string property, object value)
        {
            Type t = obj.GetType();
            t.InvokeMember(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { value });
        }
    }
}
