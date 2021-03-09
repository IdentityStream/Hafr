using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Hafr.Expressions;

namespace Hafr.Evaluation
{
    public static class Evaluator
    {
        public static string Evaluate<TModel>(Expression expression, TModel model)
        {
            return expression switch
            {
                ConstantExpression constant => GetString(constant),
                PropertyExpression property => GetProperty(property, model),
                FunctionCallExpression function => CallFunction(function, model),
                TemplateExpression template => EvaluateTemplate(template, model),
                _ => throw new NotImplementedException($"Evaluation of {expression} has not been implemented."),
            };
        }

        private static string GetString(ConstantExpression constant)
        {
            var value = constant.Value?.ToString();

            if (value is null)
            {
                return "<null>";
            }

            if (value.Length == 0)
            {
                return "<empty>";
            }

            return value;
        }

        private static string GetProperty<TModel>(PropertyExpression property, TModel model)
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

        private static readonly Dictionary<string, Func<string, string>> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "upper", s => s.ToUpper() },
            { "lower", s => s.ToLower() },
            { "first", s => s.Substring(0, 1) },
            { "last", s => s[^1..] },
        };

        private static string CallFunction<TModel>(FunctionCallExpression function, TModel model)
        {
            var arguments = function.Arguments;

            if (arguments.Length != 1)
            {
                throw new ArgumentException($"Invalid argument count: {arguments.Length}. Expected a single argument.");
            }

            if (Functions.TryGetValue(function.Name, out var func))
            {
                return func(Evaluate(arguments[0], model));
            }

            throw new MissingMethodException($"Unknown function '{function.Name}'. Available functions: {string.Join(", ", Functions.Keys)}");
        }

        private static string EvaluateTemplate<TModel>(TemplateExpression template, TModel model)
        {
            var builder = new StringBuilder();

            foreach (var expression in template.Parts)
            {
                builder.Append(Evaluate(expression, model));
            }

            return builder.ToString();
        }
    }
}
