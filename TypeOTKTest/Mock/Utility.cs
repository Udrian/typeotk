using System.Reflection;

namespace TypeOTKTest.Mock
{
    internal class Utility
    {
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
