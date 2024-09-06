using TypeD;
using TypeDTK;
using TypeOTKTest.Mock;

namespace TypeOTKTest.TypeTest
{
    public static class TypeDTKTestExtension
    {
        internal static TypeDTKInitializer CreateInitializer()
        {
            var initializer = new TypeDTKInitializer();

            var resourceModel = new ResourceModelMock();
            TypeDInit.Init(resourceModel);

            Utility.SetProperty(initializer, "Resources", resourceModel);

            return initializer;
        }

        internal static TypeDTKInitializer Initializer = CreateInitializer();
    }
}
