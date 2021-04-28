<div align="center">
    <img src="logo.png" width="320" height="320">
    <p>
        <b>A tiny templating language for writing email generation conventions.</b>    
    </p>
</div>

## Writing a Template

### The Template Structure

A Hafr template consists of two different parts: text and holes.
Text is just literal text that will be output verbatim when evaluating the template against a model.
A hole will be filled in based on its contents and the model during evaluation. Here's an example:

```
Hello {name}!
```

The first and last parts, `Hello ` and `!` are literal text, while `{name}` is a hole.
Inside holes, you can access public properties of the model you're evaluating against. If you have the following class:

```csharp
public record Person(string Name);
```

And evaluate the template against the following instance:

```csharp
var person = new Person("John Doe");
```

The template above will yield:

```
Hello John Doe!
```

Any public property of the model will be exposed to the template.

### Calling Functions

Hafr has a few built-in methods that you can call to transform properties inside holes:

| Method   | Description                                                                       |
|----------|-----------------------------------------------------------------------------------|
| `split`  | Takes a separator argument to split its input into separate parts.                |
| `join`   | Takes a separator argument to join several parts into one.                        |
| `substr` | Takes a count argument to extract a specified number of characters for each part. |
| `take`   | Takes a count argument to pick a specified number of parts.                       |

These methods can be called in one of two ways; using C-style function calls:

```
Hello {join(split(name, ' '), '.')}!
```

...or using piping:

```
Hello {name | split(' ') | join('.')}!
```

## Parsing a Template

To parse a template, use the `Parser.TryParse` method:

```csharp
var template = "{firstName | split(' ') | join('.')}.{lastName}@company.com";

if (Parser.TryParse(template, out var expression, out var errorMessage, out var errorPosition))
{
    // Parsing succeeded, it's safe to access the expression in here...
}
else
{
    // Parsing failed. You can use errorMessage and errorPosition in here...
}
```

## Evaluating a Template

After the template has been successfully parsed, you can use it to evaluate against an input model:

```csharp
public record Person(string FirstName, string LastName);

var model = new Person("John Michael", "Doe");

var result = expression.Evaluate(model).ToLower(); // john.michael.doe@company.com 
```