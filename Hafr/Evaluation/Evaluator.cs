using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Hafr.Expressions;

namespace Hafr.Evaluation
{
    public static class Evaluator
    {
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
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            var propertyInfo = typeof(TModel).GetProperty(property.Name, bindingFlags);

            if (propertyInfo is null || !propertyInfo.CanRead)
            {
                throw new MissingMemberException($"Invalid property: {property.Name}");
            }

            var result = propertyInfo.GetValue(model);
            var resultExpression = Expression.Constant(result);

            return Evaluate(resultExpression, model);
        }

        private static readonly Dictionary<string, Delegate> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "upper", new Func<string, string>(ToUpper) },
            { "lower", new Func<string, string>(ToLower) },
            { "start", new Func<string, int, string>(Start) },
        };

        private static string ToUpper(string value) => value.ToUpper();

        private static string ToLower(string value) => value.ToLower();

        private static string Start(string value, int length) => value.Substring(0, length);

        private static object CallFunction<TModel>(FunctionCallExpression function, TModel model)
        {
            var arguments = function.Arguments;

            if (Functions.TryGetValue(function.Name, out var func))
            {
                var values = new object[arguments.Length];

                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = Evaluate(arguments[i], model);
                }

                return func.DynamicInvoke(values);
            }

            throw new MissingMethodException($"Unknown function '{function.Name}'. Available functions: {string.Join(", ", Functions.Keys)}");
        }
    }
}
