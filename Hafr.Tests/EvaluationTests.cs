using System.Linq;
using Hafr.Evaluation;
using Hafr.Parsing;
using Superpower.Model;
using Xunit;

namespace Hafr.Tests
{
    public class EvaluationTests
    {
        [Theory]
        [InlineData("{firstName | split(' ') | substr(1) | join('')}{lastName | split(' ') | substr(1) | join('')}", "tok")]
        [InlineData("{firstName | split(' ') | reverse | substr(1) | join('')}{lastName | split(' ') | substr(1) | join('')}", "otk")]
        [InlineData("{firstName | split(' ') | join('.')}.{lastName | split(' ') | join('.')}", "tore.olav.kristiansen")]
        [InlineData("{firstName | split(' ') | skip(1) | join('')}.{lastName | split(' ') | join('.')}", "olav.kristiansen")]
        [InlineData("{firstName | split(' ') | take(1)}{lastName | take(1)}", "torek")]
        [InlineData("{firstName | substr(2)}{lastName | substr(1)}", "tok")]
        public void Evaluation_Outputs_Correct_Result(string template, string expected)
        {
            var result = Parser.TryParse(template, out var expression, out var errorMessage, out _);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Tore Olav", "Kristiansen");

            var actual = expression!.EvaluateModel(model).Single().ToLower();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Replace_Outputs_Correct_Result() {
            var result = Parser.TryParse("{lastName | replace('�', 'o')}", out var expression, out var errorMessage, out _);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Peder", "Kj�s");

            var actual = expression!.EvaluateModel(model).Single().ToLower();

            Assert.Equal("kjos", actual);
        }

        [Theory]
        [InlineData("{firstName}\n{firstName}\r\n{firstName}", "tore olav")]
        [InlineData("hello\nhello\r\nhello", "hello")]
        public void MultiLine_Evaluation_Outputs_Correct_Results(string template, string expected)
        {
            var result = Parser.TryParse(template, out var expression, out var errorMessage, out var errorPosition);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Tore Olav", "Kristiansen");

            var actual = expression!.EvaluateModel(model).Select(x => x.ToLower()).ToList();

            Assert.All(actual, x => Assert.Equal(expected, x));
        }

        [Fact]
        public void MultiLine_Parsing_Outputs_Correct_ErrorPosition()
        {
            const string template = "{firstName}\n{firstName}\r\n{";

            var result = Parser.TryParse(template, out _, out _, out var errorPosition);

            Assert.False(result, "Expected template parsing to fail.");

            Assert.Equal(new Position(26, 3, 2), errorPosition);
        }

        [Theory]
        [InlineData("{firstName | split}", "An error occurred while calling function 'split': Parameter count mismatch.")]
        [InlineData("{firstName | blah}", "Unknown function 'blah'. Available functions: split, join, skip, take, substr, replace, reverse")]
        [InlineData("{firstName | 2}", "Unknown function '2'. Available functions: split, join, skip, take, substr, replace, reverse")]
        [InlineData("{substr(2, 2)}", "An error occurred while calling function 'substr': Specified method is not supported.")]
        [InlineData("{unknown}", "Unknown property 'unknown'. Available properties: FirstName, LastName")]
        public void Evaluation_Outputs_Correct_ErrorMessage(string template, string expected)
        {
            var result = Parser.TryParse(template, out var expression, out var errorMessage, out var errorPosition);

            Assert.True(result, $"Parsing template expression failed: {errorMessage}");

            var model = new Person("Tore Olav", "Kristiansen");

            var exception = Assert.Throws<TemplateEvaluationException>(() => expression!.EvaluateModel(model).ToList());

            Assert.Equal(expected, exception.Message);
        }

        private record Person(string FirstName, string LastName);
    }
}
