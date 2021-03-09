using System;
using System.Text;
using Hafr.Evaluation;
using Hafr.Parsing;
using Superpower.Model;

namespace Hafr
{
    public static class Program
    {
        private const string Prompt = "hafr> ";

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($@"ooooo ooooo              o888o            
 888   888   ooooooo   o888oo oo oooooo   
 888ooo888   ooooo888   888    888    888 
 888   888 888    888   888    888        
o888o o888o 88ooo88 8o o888o  o888o  v1.0");
            Console.ResetColor();
            Console.WriteLine();

            var model = new Person("Kristian", "Hellang");

            Evaluate(model, "{firstName}.{lastName}");
            Evaluate(model, "{start(firstName, 1)}{lastName}");
            Evaluate(model, "{start(firstName, 2)}{start(lastName, 2)}");

            Console.WriteLine();

            while (true)
            {
                Console.Write(Prompt);
                Evaluate(model, Console.ReadLine());
                Console.WriteLine();
            }
        }

        private static void Evaluate(Person model, string input)
        {
            var result = Tokenizer.Instance.TryTokenize(input);

            if (result.HasValue)
            {
                if (Parser.TryParse(result.Value, out var expr, out var error, out var position))
                {
                    try
                    {
                        var evaluated = Evaluator.Evaluate(expr, model);
                        Console.WriteLine($"{input} -> {evaluated.ToLower()}");
                        return;
                    }
                    catch (Exception e)
                    {
                        position = Position.Empty;
                        error = e.Message;
                    }
                }

                WriteError(error, position);
                return;
            }

            WriteError(result.ToString(), result.ErrorPosition);
        }

        private static void WriteError(string message, Position errorPosition)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (errorPosition.HasValue && errorPosition.Line == 1)
            {
                Console.WriteLine(new string(' ', Prompt.Length + errorPosition.Column - 1) + '^');
            }

            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public record Person(string FirstName, string LastName);
}
