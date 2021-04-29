using Hafr.Evaluation;
using Hafr.Parsing;
using Xunit;

namespace Hafr.Tests
{
    public class EvaluationTests
    {
        [Theory]
        [InlineData("{firstName | split(' ') | substr(1) | join('')}{lastName | split(' ') | substr(1) | join('')}", "tok")]
        [InlineData("{firstName | split(' ') | join('.')}.{lastName | split(' ') | join('.')}", "tore.olav.kristiansen")]
        [InlineData("{firstName | split(' ') | join('')}.{lastName | split(' ') | join('.')}", "toreolav.kristiansen")]
        [InlineData("{firstName | split(' ') | take(1)}{lastName | take(1)}", "torek")]
        [InlineData("{firstName | substr(2)}{lastName | substr(1)}", "tok")]
        public void Evaluation_Outputs_Correct_Result(string template, string expected)
        {
            var result = Parser.TryParse(template, out var expression, out var errorMessage, out var errorPosition);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Tore Olav", "Kristiansen");

            var actual = expression!.Evaluate(model).ToLower();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("{firstName | split}", "An error occurred while calling function 'split': Parameter count mismatch.")]
        [InlineData("{firstName | blah}", "Unknown function 'blah'. Available functions: split, join, take, substr")]
        [InlineData("{firstName | 2}", "Unknown function '2'. Available functions: split, join, take, substr")]
        [InlineData("{substr(2, 2)}", "An error occurred while calling function 'substr': Specified method is not supported.")]
        [InlineData("{unknown}", "Invalid property 'unknown'. Available properties: FirstName, LastName")]
        public void Evaluation_Outputs_Correct_ErrorMessage(string template, string expected)
        {
            var result = Parser.TryParse(template, out var expression, out var errorMessage, out var errorPosition);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Tore Olav", "Kristiansen");

            var exception = Assert.Throws<TemplateEvaluationException>(() => expression!.Evaluate(model));

            Assert.Equal(expected, exception.Message);
        }

        private record Person(string FirstName, string LastName);
    }
}
