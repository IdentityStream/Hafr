using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Hafr.Expressions;

namespace Hafr.Evaluation
{
    public static class Evaluator
    {
        private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries;

        public static IEnumerable<string> EvaluateModel<TModel>(MultiTemplateExpression template, TModel model) where TModel : notnull
        {
            return EvaluateModel(template, model, typeof(TModel));
        }

        public static IEnumerable<string> EvaluateModel(MultiTemplateExpression template, object model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return EvaluateModel(template, model, model.GetType());
        }

        public static IEnumerable<string> EvaluateModel(MultiTemplateExpression template, object model, Type? modelType)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var caseInsensitiveProperties = GetModelProperties(model, modelType ?? model.GetType());

            return EvaluateCaseInsensitiveProperties(template, caseInsensitiveProperties);
        }

        public static IEnumerable<string> EvaluateProperties(MultiTemplateExpression template, IDictionary<string, object?> properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            var caseInsensitiveProperties = new Dictionary<string, object?>(properties, StringComparer.OrdinalIgnoreCase);

            return EvaluateCaseInsensitiveProperties(template, caseInsensitiveProperties);
        }

        private static IEnumerable<string> EvaluateCaseInsensitiveProperties(MultiTemplateExpression template, IDictionary<string, object?> properties)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return TemplateIterator(template.Parts, properties);

            static IEnumerable<string> TemplateIterator(IEnumerable<TemplateExpression> templates, IDictionary<string, object?> properties)
            {
                foreach (var template in templates)
                {
                    yield return EvaluateTemplate(template, properties);
                }
            }
        }

        public static void RegisterFunction(string name, Delegate func)
        {
            Functions[name] = func;
        }

        private static object? Evaluate(Expression expression, IDictionary<string, object?> properties)
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

        private static string EvaluateTemplate(TemplateExpression template, IDictionary<string, object?> properties)
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

        private static IDictionary<string, object?> GetModelProperties(object model, Type modelType)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in ReflectionCache.GetProperties(modelType))
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

        private static object? GetProperty(PropertyExpression property, IDictionary<string, object?> properties)
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
                    (value, old, @new) => value.Select(x => x.Replace(old, @new)),
                    (value, old, @new) => value.Replace(old, @new))
            },
            { "reverse", Map(Reverse, Reverse) },
            { "upper", Map(x => x.ToUpper()) },
            { "lower", Map(x => x.ToLower()) },
            { "trim", Map(x => x.Trim()) },
            { "truncate", Map<int>((x, i) => x.Truncate(i)) },
        };

        private static Func<object, T, object> Map<T>(Func<string, T, string> transform)
        {
            return (x, i) => x switch
            {
                string[] array => array.Select(y => transform(y, i)).ToArray(),
                string value => transform(value, i),
                _ => throw new NotSupportedException(),
            };
        }

        private static Func<object, object> Map(Func<string, string> transform)
        {
            return x => x switch
            {
                string[] array => array.Select(transform).ToArray(),
                string value => transform(value),
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

        private static Func<object, T1, T2, object> Map<T1, T2>(Func<string[], T1, T2, object> multi, Func<string, T1, T2, object> single)
        {
            return (x, a, b) => x switch
            {
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

        private static object? CallFunction(FunctionCallExpression function, IDictionary<string, object?> properties)
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

        private static object? PipeValue(PipeExpression pipe, IDictionary<string, object?> properties)
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
            return new(string.Format(Strings.UnknownFunction, expression, GetList(Functions.Keys)), expression.Position);
        }

        private static string GetList<T>(IEnumerable<T> source) => string.Join(", ", source);

        private static class ReflectionCache
        {
            private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> PropertyCache = new();

            public static IReadOnlyCollection<PropertyInfo> GetProperties(Type type) =>
                PropertyCache.GetOrAdd(type, ReadProperties);

            private static List<PropertyInfo> ReadProperties(Type type) =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
        }
    }
}
