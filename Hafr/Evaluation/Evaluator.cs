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

        public static IEnumerable<string> Evaluate(MultiTemplateExpression template, object model, Type modelType)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            foreach (var part in template.Parts)
            {
                yield return EvaluateTemplate(part, model, modelType);
            }
        }

        public static void RegisterFunction(string name, Delegate func)
        {
            Functions[name] = func;
        }

        private static object? Evaluate(Expression expression, object model, Type modelType)
        {
            return expression switch
            {
                TextExpression text => text.Value,
                ConstantExpression constant => constant.Value,
                PipeExpression pipe => PipeValue(pipe, model, modelType),
                TemplateExpression part => EvaluateTemplate(part, model, modelType),
                PropertyExpression property => GetProperty(property, model, modelType),
                FunctionCallExpression function => CallFunction(function, model, modelType),
                MultiTemplateExpression => throw new NotSupportedException("Multi-template expressions are top-level expressions only."),
                _ => throw new NotImplementedException(string.Format(Strings.EvaluationNotImplemented, expression)),
            };
        }

        private static string EvaluateTemplate(TemplateExpression template, object model, Type modelType)
        {
            var builder = new StringBuilder();

            foreach (var expression in template.Parts)
            {
                builder.Append(GetString(Evaluate(expression, model, modelType)));
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

        private static object? GetProperty(PropertyExpression property, object model, Type modelType)
        {
            if (!PropertyCache.TryGetProperty(modelType, property.Name, out var propertyInfo) || !propertyInfo.CanRead)
            {
                throw new TemplateEvaluationException(
                    string.Format(Strings.UnknownProperty, property.Name, GetList(PropertyCache.GetNames(modelType))),
                    property.Position);
            }

            var result = propertyInfo.GetValue(model);
            var resultExpression = Expression.Constant(result);

            return Evaluate(resultExpression, model, modelType);
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

        private static object? CallFunction(FunctionCallExpression function, object model, Type modelType)
        {
            var arguments = function.Arguments;

            if (!Functions.TryGetValue(function.Name, out var func))
            {
                throw GetUnknownFunctionException(function);
            }

            var values = new object?[arguments.Length];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Evaluate(arguments[i], model, modelType);
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

        private static object? PipeValue(PipeExpression pipe, object model, Type modelType)
        {
            if (TryGetFunctionCall(pipe.Right, out var functionCall))
            {
                return Evaluate(functionCall.PipeArgument(pipe.Left), model, modelType);
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
            private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new();

            public static bool TryGetProperty(Type type, string name, [NotNullWhen(true)] out PropertyInfo? property) =>
                _propertyCache.GetOrAdd(type, GetProperties).TryGetValue(name, out property);

            public static IEnumerable<string> GetNames(Type type)
            {
                return _propertyCache.GetOrAdd(type, GetProperties).Keys;
            }

            private static Dictionary<string, PropertyInfo> GetProperties(Type type) =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
