using System;
using System.Text;
using Hafr.Evaluation;
using Hafr.Parsing;
using Redskap;
using Superpower.Model;

namespace Hafr.Repl
{
    public static class Program
    {
        private const string Prompt = "hafr> ";

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(@"ooooo ooooo              o888o            
 888   888   ooooooo   o888oo oo oooooo   
 888ooo888   ooooo888   888    888    888 
 888   888 888    888   888    888        
o888o o888o 88ooo88 8o o888o  o888o  v1.0");
            Console.ResetColor();
            Console.WriteLine();

            var model = new Person(Names.GenerateGivenName(), Names.GenerateFamilyName());

            for (var i = 0; i < 10; i++)
            {
                Evaluate(model = new Person(Names.GenerateGivenName(), Names.GenerateFamilyName()));
            }

            Console.WriteLine();

            while (true)
            {
                Console.Write(Prompt);
                Evaluate(model, Console.ReadLine());
                Console.WriteLine();
            }
        }

        private static void Evaluate(Person model)
        {
            Evaluate(model, "{ firstName }.{ lastName }");
            Evaluate(model, "{ firstName | take(1) }{ lastName }");
            Evaluate(model, "{ firstName | take(2) }{ lastName | take(2) }");
            Evaluate(model, "{ firstName | split(' ') | join('.') }-{ lastName | split(' ') | join('.') }");
            Console.WriteLine();
        }

        private static void Evaluate(Person model, string? input)
        {
            if (input is null)
            {
                return;
            }

            if (Parser.TryParse(input, out var expr, out var error, out var position))
            {
                try
                {
                    foreach (var result in Evaluator.EvaluateModel(expr, model))
                    {
                        Console.WriteLine(result.ToLower());
                    }
                    return;
                }
                catch (TemplateEvaluationException ee)
                {
                    position = ee.Position;
                    error = ee.Message;
                }
                catch (Exception e)
                {
                    position = Position.Empty;
                    error = e.Message;
                }
            }

            WriteError(error, position);
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
