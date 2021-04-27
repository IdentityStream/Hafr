using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Hafr.Expressions;

namespace Hafr.Evaluation
{
    public static class Evaluator
    {
        private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries;

        public static string Evaluate<TModel>(TemplateExpression template, TModel model)
        {
            var builder = new StringBuilder();

            foreach (var expression in template.Parts)
            {
                builder.Append(GetString(Evaluate(expression, model)));
            }

            return builder.ToString();
        }

        public static void RegisterFunction(string name, Delegate func)
        {
            Functions[name] = func;
        }

        private static object Evaluate<TModel>(Expression expression, TModel model)
        {
            return expression switch
            {
                TextExpression text => text.Value,
                ConstantExpression constant => constant.Value,
                PipeExpression pipe => PipeValue(pipe, model),
                PropertyExpression property => GetProperty(property, model),
                FunctionCallExpression function => CallFunction(function, model),
                TemplateExpression => throw new NotSupportedException("Template expressions are top-level expressions only."),
                _ => throw new NotImplementedException($"Evaluation of {expression} has not been implemented."),
            };
        }

        private static string GetString(object value)
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

        private static object GetProperty<TModel>(PropertyExpression property, TModel model)
        {
            if (!PropertyCache<TModel>.All.TryGetValue(property.Name, out var propertyInfo) || !propertyInfo.CanRead)
            {
                var propertyList = string.Join(", ", PropertyCache<TModel>.All.Keys);
                throw new TemplateEvaluationException(
                    $"Invalid property '{property.Name}'. Available properties: {propertyList}",
                    property.Position);
            }

            var result = propertyInfo.GetValue(model);
            var resultExpression = Expression.Constant(result);

            return Evaluate(resultExpression, model);
        }

        private static readonly Dictionary<string, Delegate> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "split", Map<string>(
                (value, separator) => value.SelectMany(x => x.Split(new[] { separator }, SplitOptions)).Select(y => y.Trim()).ToArray(),
                (value, separator) => value.Split(new[] { separator }, SplitOptions).Select(y => y.Trim()).ToArray()) },
            { "join", Map<string>(
                (value, separator) => string.Join(separator, value),
                (value, _) => value) },
            { "take", Map<int>(
                (value, count) => value.Take(count).ToArray(),
                (value, count) => value.Substring(0, count)) },
            { "substr", Map<int>((s, i) => s.Substring(0, i)) },
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

        private static Func<object, T, object> Map<T>(Func<string[], T, object> multi, Func<string, T, object> single)
        {
            return (x, i) => x switch
            {
                string[] array => multi(array, i),
                string value => single(value, i),
                _ => throw new NotSupportedException(),
            };
        }

        private static object CallFunction<TModel>(FunctionCallExpression function, TModel model)
        {
            var arguments = function.Arguments;

            if (!Functions.TryGetValue(function.Name, out var func))
            {
                var functionList = string.Join(", ", Functions.Keys);
                throw new TemplateEvaluationException(
                    $"Unknown function '{function.Name}'. Available functions: {functionList}",
                    function.Position);
            }

            var values = new object[arguments.Length];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Evaluate(arguments[i], model);
            }

            try
            {
                return func.DynamicInvoke(values);
            }
            catch (Exception e)
            {
                throw new TemplateEvaluationException(
                    $"An error occurred while calling function '{function.Name}': {e.Message}",
                    function.Position);
            }
        }

        private static object PipeValue<TModel>(PipeExpression pipe, TModel model)
        {
            if (pipe.Right is FunctionCallExpression functionCall)
            {
                return Evaluate(functionCall.PipeArgument(pipe.Left), model);
            }

            throw new TemplateEvaluationException($"Values can only be piped into a function. '{pipe.Right}' is not a function.", pipe.Position);
        }

        private static class PropertyCache<TModel>
        {
            public static readonly Dictionary<string, PropertyInfo> All = typeof(TModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
