using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pipes.Implementation
{
    public sealed class TypeSwitch
    {
        private readonly Dictionary<Type, Func<object, object,Task>> _matches = new Dictionary<Type, Func<object, object, Task>>();
        private Func<object, object, Type> _typeAdaptor;
        private Func<object, object, object> _targetAdaptor;

        public TypeSwitch() { }

        /// <summary>
        /// Changes the Type of the value being matched
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adaptor"></param>
        /// <returns></returns>
        public TypeSwitch SourceAdaptor<T>(Func<T, Type> adaptor) where T : class
        {
            _typeAdaptor = (match, arguments) => adaptor((T)match);

            return this;
        }

        /// <summary>
        /// Changes the Type of the value being matched
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adaptor"></param>
        /// <returns></returns>
        public TypeSwitch SourceAdaptor<T>(Func<T, object, Type> adaptor) where T : class
        {
            _typeAdaptor = (match, arguments) => adaptor((T)match, arguments);

            return this;
        }


        /// <summary>
        /// Changes the value that will be passed to the switch on match
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adaptor"></param>
        /// <returns></returns>
        public TypeSwitch TargetAdaptor<T>(Func<T, object> adaptor) where T : class
        {
            _targetAdaptor = (match, arguments) => adaptor((T)match);

            return this;
        }

        /// <summary>
        /// Changes the value that will be passed to the switch on match
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adaptor"></param>
        /// <returns></returns>
        public TypeSwitch TargetAdaptor<T>(Func<T, object, object> adaptor) where T : class
        {
            _targetAdaptor = (match, arguments) => adaptor((T)match, arguments);

            return this;
        }

        public TypeSwitch Case<T>(Action action)
        {
            _matches.Add(typeof(T), (data, arguments) => { action(); return Task.FromResult(true); });

            return this;
        }

        public TypeSwitch Case<T>(Action<T> action)
        {
            _matches.Add(typeof(T), (data, arguments) => { action((T)data); return Task.FromResult(true); });

            return this;
        }

        public TypeSwitch Case<T>(Action<T, object> action)
        {
            _matches.Add(typeof(T), (data, arguments) => { action((T)data, arguments); return Task.FromResult(true); });

            return this;
        }

        public TypeSwitch Default(Action action)
        {
            _matches.Add(typeof(void), (data, arguments) => { action(); return Task.FromResult(true); });

            return this;
        }

        public TypeSwitch Default(Action<object> action)
        {
            _matches.Add(typeof(void), (data, arguments) => { action(data); return Task.FromResult(true); });

            return this;
        }

        public TypeSwitch Default(Action<object, object> action)
        {
            _matches.Add(typeof(void), (data, arguments) => { action(data, arguments); return Task.FromResult(true); });
            return this;
        }


        public TypeSwitch CaseAsync<T>(Func<Task> task)
        {
            _matches.Add(typeof(T), (data, arguments) => task());

            return this;
        }

        public TypeSwitch CaseAsync<T>(Func<T, Task> task)
        {
            _matches.Add(typeof(T), (data, arguments) => task((T)data));

            return this;
        }

        public TypeSwitch CaseAsync<T>(Func<T, object, Task> task)
        {
            _matches.Add(typeof(T), (data, arguments) => task((T)data, arguments));

            return this;
        }

        public TypeSwitch DefaultAsync(Func<Task> task)
        {
            _matches.Add(typeof(void), (data, arguments) => task());

            return this;
        }

        public TypeSwitch DefaultAsync(Func<object, Task> task)
        {
            _matches.Add(typeof(void), (data, arguments) => task(data));

            return this;
        }

        public TypeSwitch DefaultAsync(Func<object, object, Task> task)
        {
            _matches.Add(typeof(void), task);

            return this;
        }


        public TypeSwitch ThrowOnDefault(string message = null)
        {
            if (message == null)
                _matches.Add(typeof(void), (data, arguments) => { throw new Exception($"TypeSwitch did not handle {(data == null ? "NULL" : data.GetType().FullName)}"); });
            else
                _matches.Add(typeof(void), (data, arguments) => { throw new Exception(message); });
            return this;
        }

        public bool Switch(object match)
        {
            return Switch(match, null);
        }

        public async Task<bool> SwitchAsync(object match)
        {
            return await SwitchAsync(match, null);
        }

        public bool Switch(object match, params object[] arguments)
        {
            return SwitchAsync(match, arguments).Result;
        }
        public async Task<bool> SwitchAsync(object match, params object[] arguments)
        {
            var type = _typeAdaptor != null ? _typeAdaptor(match, arguments) : match.GetType();
            object target = _targetAdaptor != null ? _targetAdaptor(match, arguments) : match;

            var baseType = typeof(System.Object);

            while (type != baseType && type != null)
            {
                if (_matches.ContainsKey(type))
                {
                    await _matches[type](target, arguments);
                    return true;
                }
                else
                {
                    foreach (var test in _matches)
                        if (test.Key.IsAssignableFrom(type))
                        {
                            await test.Value(target, arguments);
                            return true;
                        }

                    type = type.BaseType;
                }
            }

            if (_matches.ContainsKey(baseType))
            {
                await _matches[baseType](target, arguments);
                return true;
            }

            type = typeof(void);

            if (_matches.ContainsKey(type))
            {
                await _matches[type](target, arguments);
                return true;
            }

            return false;
        }
    }
}
