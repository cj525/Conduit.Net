using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pipes.Example.PipelineAdjuncts
{
    class ReflectionCache<T> where T:class,new()
    {
        private readonly Dictionary<string, Type> _fieldTypes;
        private readonly Dictionary<string, Action<T,object>> _setters;

        public ReflectionCache()
        {
            _fieldTypes =new Dictionary<string, Type>();
            _setters = new Dictionary<string, Action<T, object>>();

            var type = typeof (T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {
                var name = property.Name;
                _fieldTypes.Add( name, property.PropertyType );
                _setters.Add(name, (instance, value) => property.SetValue(instance, value));
            }
        }

        public T Inflate(IEnumerable<string> fields, Func<string, string> deserializeField)
        {
            var result = new T();

            foreach (var field in fields)
            {
                var value = deserializeField(field);
                var type = _fieldTypes[field];
                var typedValue = JsonConvert.DeserializeObject(value, type);
                _setters[field](result, typedValue);
            }

            return result;
        }
    }
}
