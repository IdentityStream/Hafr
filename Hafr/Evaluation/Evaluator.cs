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
        private const StringSplitOptions SplitOptions = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;

        public static string Evaluate<TModel>(TemplateExpression template, TModel model)
        {
            var builder = new StringBuilder();

            foreach (var expression in template.Parts)
            {
                builder.Append(GetString(Evaluate(expression, model)));
            }

            return builder.ToString();
        }

        private static object Evaluate<TModel>(Expression expression, TModel model)
        {
            return expression switch
            {
                TextExpression text => text.Value,
                ConstantExpression constant => constant.Value,
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
            { "split", new Func<string, string, IEnumerable<string>>((x, s) => x.Split(s, SplitOptions)) },
            { "join", new Func<IEnumerable<string>, string, string>((x, s) => string.Join(s, x)) },
            { "take", CreateFunc((s, i) => s.Substring(0, i)) },
            { "first", new Func<object, string>(First) },
        };

        private static Func<object, int, object> CreateFunc(Func<string, int, string> transformer)
        {
            return (x, i) => x switch
            {
                IEnumerable<string> c => c.Select(y => transformer(y, i)),
                string s => transformer(s, i),
                _ => throw new NotSupportedException(),
            };
        }

        private static string First(object value) => value switch
        {
            IEnumerable<string> c => c.First(),
            string s => s[0].ToString(),
            _ => throw new NotSupportedException(),
        };

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

        private static class PropertyCache<TModel>
        {
            public static readonly Dictionary<string, PropertyInfo> All = typeof(TModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
