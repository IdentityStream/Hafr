using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Hafr.Expressions;

namespace Hafr.Evaluation
{
    public static class Evaluator
    {
        private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries;

        public static IEnumerable<string> Evaluate<TModel>(MultiTemplateExpression template, TModel model) where TModel : notnull
        {
            return Evaluate(template, model, typeof(TModel));
        }

        public static IEnumerable<string> Evaluate(MultiTemplateExpression template, object model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return Evaluate(template, model, model.GetType());
        }

        public static IEnumerable<string> Evaluate(MultiTemplateExpression template, object model, Type? modelType)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return Evaluate(template, GetProperties(model, modelType ?? model.GetType()));
        }

        public static IEnumerable<string> Evaluate(MultiTemplateExpression template, IReadOnlyDictionary<string, object> properties)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            foreach (var part in template.Parts)
            {
                yield return EvaluateTemplate(part, properties);
            }
        }

        public static void RegisterFunction(string name, Delegate func)
        {
            Functions[name] = func;
        }

        private static object? Evaluate(Expression expression, IReadOnlyDictionary<string, object> properties)
        {
            return expression switch
            {
                TextExpression text => text.Value,
                ConstantExpression constant => constant.Value,
                PipeExpression pipe => PipeValue(pipe, properties),
                TemplateExpression part => EvaluateTemplate(part, properties),
                PropertyExpression property => GetProperty(property, properties),
                FunctionCallExpression function => CallFunction(function, properties),
                MultiTemplateExpression => throw new NotSupportedException("Multi-template expressions are top-level expressions only."),
                _ => throw new NotImplementedException(string.Format(Strings.EvaluationNotImplemented, expression)),
            };
        }

        private static string EvaluateTemplate(TemplateExpression template, IReadOnlyDictionary<string, object> properties)
        {
            var builder = new StringBuilder();

            foreach (var expression in template.Parts)
            {
                builder.Append(GetString(Evaluate(expression, properties)));
            }

            return builder.ToString();
        }

        private static string? GetString(object? value)
        {
            if (value is null)
            {
                return "<null>";
            }

            if (value is string stringValue)
            {
                if (stringValue.Length == 0)
                {
                    return "<empty>";
                }

                return stringValue;
            }

            if (value is string[] stringArray)
            {
                if (stringArray.Length == 0)
                {
                    return "<empty>";
                }

                return string.Join(" ", stringArray);
            }

            return value.ToString();
        }

        private static IReadOnlyDictionary<string, object> GetProperties(object model, Type modelType)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in PropertyCache.GetProperties(modelType))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                // Skip indexers
                var indexParameters = property.GetIndexParameters();
                if (indexParameters?.Length > 0)
                {
                    continue;
                }

                values.Add(property.Name, property.GetValue(model) ?? string.Empty);
            }

            return values;
        }

        private static object? GetProperty(PropertyExpression property, IReadOnlyDictionary<string, object> properties)
        {
            if (!properties.TryGetValue(property.Name, out var value))
            {
                throw new TemplateEvaluationException(
                    string.Format(Strings.UnknownProperty, property.Name, GetList(properties.Keys)),
                    property.Position);
            }

            return Evaluate(Expression.Constant(value), properties);
        }

        private static readonly Dictionary<string, Delegate> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            {
                "split", Map<string>(
                    (value, separator) => value.SelectMany(x => x.Split(new[] { separator }, SplitOptions)).Select(y => y.Trim()).ToArray(),
                    (value, separator) => value.Split(new[] { separator }, SplitOptions).Select(y => y.Trim()).ToArray())
            },
            {
                "join", Map<string>(
                    (value, separator) => string.Join(separator, value),
                    (value, _) => value)
            },
            {
                "skip", Map<int>(
                    (value, count) => value.Skip(count).ToArray(),
                    (value, count) => value.Substring(count))
            },
            {
                "take", Map<int>(
                    (value, count) => value.Take(count).ToArray(),
                    (value, count) => value.Substring(0, count))
            },
            {
                "substr", Map<int>((s, i) => s.Substring(0, i))
            },
            {
                "replace", Map<string, string>(
                    (value, old, @new) => value.SelectMany(x => x.Replace(old, @new)),
                    (value, old, @new) => value.Replace(old, @new))
            },
            {
                "reverse", Map(Reverse, Reverse)
            },
        };

        private static Func<object, T, object> Map<T>(Func<string, T, string> transformer)
        {
            return (x, i) => x switch
            {
                string[] array => array.Select(y => transformer(y, i)).ToArray(),
                string value => transformer(value, i),
                _ => throw new NotSupportedException(),
            };
        }

        private static Func<object, object> Map(Func<string[], object> multi, Func<string, object> single)
        {
            return x => x switch
            {
                string[] array => multi(array),
                string value => single(value),
                _ => throw new NotSupportedException(),
            };
        }

        private static Func<object, T, object> Map<T>(Func<string[], T, object> multi, Func<string, T, object> single)
        {
            return (x, i) => x switch
            {
                string[] array => multi(array, i),
                string value => single(value, i),
                _ => throw new NotSupportedException(),
            };
        }

        private static Func<object, T1, T2, object> Map<T1, T2>(Func<string[], T1, T2, object> multi, Func<string, T1, T2, object> single) {
            return (x, a, b) => x switch {
                string[] array => multi(array, a, b),
                string value => single(value, a, b),
                _ => throw new NotSupportedException(),
            };
        }

        private static string Reverse(string value)
        {
            var chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static string[] Reverse(string[] value)
        {
            Array.Reverse(value);
            return value;
        }

        private static object? CallFunction(FunctionCallExpression function, IReadOnlyDictionary<string, object> properties)
        {
            var arguments = function.Arguments;

            if (!Functions.TryGetValue(function.Name, out var func))
            {
                throw GetUnknownFunctionException(function);
            }

            var values = new object?[arguments.Length];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Evaluate(arguments[i], properties);
            }

            try
            {
                return func.DynamicInvoke(values);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                throw new TemplateEvaluationException(
                    string.Format(Strings.FunctionError, function.Name, baseException.Message),
                    function.Position);
            }
        }

        private static object? PipeValue(PipeExpression pipe, IReadOnlyDictionary<string, object> properties)
        {
            if (TryGetFunctionCall(pipe.Right, out var functionCall))
            {
                return Evaluate(functionCall.PipeArgument(pipe.Left), properties);
            }

            throw GetUnknownFunctionException(pipe.Right);
        }

        private static bool TryGetFunctionCall(Expression expression, [NotNullWhen(true)] out FunctionCallExpression? functionCall)
        {
            if (expression is FunctionCallExpression function)
            {
                functionCall = function;
                return true;
            }

            if (expression is PropertyExpression property)
            {
                functionCall = new FunctionCallExpression(property.Position, property.Name, Array.Empty<Expression>());
                return true;
            }

            functionCall = default;
            return false;
        }

        private static TemplateEvaluationException GetUnknownFunctionException(Expression expression)
        {
            return new(string.Format(Strings.UnknownFunction, expression, GetList(Functions.Keys)),  expression.Position);
        }

        private static string GetList<T>(IEnumerable<T> source) => string.Join(", ", source);

        private static class PropertyCache
        {
            private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> _propertyCache = new();

            public static IReadOnlyCollection<PropertyInfo> GetProperties(Type type) =>
                _propertyCache.GetOrAdd(type, ReadProperties);

            private static List<PropertyInfo> ReadProperties(Type type) =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }
    }
}
