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
            var result = Parser.TryParse(template, out var expression, out var error, out var errorPosition);

            Assert.True(result, $"Parsing template expression failed: {error}");

            var model = new Person("Tore Olav", "Kristiansen");

            var actual = Evaluator.Evaluate(expression, model).ToLower();

            Assert.Equal(expected, actual);
        }

        private record Person(string FirstName, string LastName);
    }
}
