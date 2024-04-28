using TypeD.Models.Interfaces;
using TypeD.Models.Providers.Interfaces;

namespace TypeOTKTest.Mock
{
    internal class ResourceModelMock : IResourceModel
    {
        private Dictionary<string, object> Resources { get; set; } = new Dictionary<string, object>();

        public void Add(List<object> values)
        {
            Add(values.Select(v => new Tuple<string, object>(v.GetType().Name, v)).ToList());
        }

        public void Add(object value)
        {
            Add(value.GetType().Name, value);
        }

        public void Add(List<Tuple<string, object>> keyValues)
        {
            foreach (var keyValue in keyValues)
            {
                if (!Resources.ContainsKey(keyValue.Item1))
                    Resources.Add(keyValue.Item1, keyValue.Item2);
                else
                    Resources[keyValue.Item1] = keyValue.Item2;
            }

            foreach (var keyValue in keyValues)
            {
                Init(keyValue.Item2);
            }
        }

        public void Add(string key, object value)
        {
            if (!Resources.ContainsKey(key))
                Resources.Add(key, value);
            else
                Resources[key] = value;

            Init(value);
        }

        private void Init(object value)
        {
            // If this is a Model or Provider then Init
            if (value is IModel)
            {
                (value as IModel).Init(this);
            }
            else if (value is IProvider)
            {
                (value as IProvider).Init(this);
            }
        }

        public void Remove(string key)
        {
            Resources.Remove(key);
        }

        public T Get<T>(string key) where T : class
        {
            if (!Resources.ContainsKey(key))
                return null;
            return Resources[key] as T;
        }

        public T Get<T>() where T : class
        {
            var type = typeof(T);
            if (type.IsInterface && type.Name.StartsWith("I"))
            {
                return Get<T>(type.Name.Substring(1));
            }
            return Get<T>(type.Name);
        }

        public void Init(IResourceModel resourceModel)
        {
            throw new NotImplementedException();
        }
    }
}