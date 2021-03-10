using Superpower.Display;

namespace Hafr.Parsing
{
    public enum TemplateToken
    {
        [Token(Example = "{")]
        OpenCurly,

        [Token(Example = "}")]
        CloseCurly,

        [Token(Example = "(")]
        OpenParen,

        [Token(Example = ")")]
        CloseParen,

        [Token(Example = ",")]
        Comma,

        [Token(Example = "|")]
        Pipe,

        String,

        Number,

        Identifier,

        Text,
    }
}
