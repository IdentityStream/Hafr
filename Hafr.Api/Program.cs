using Hafr.Api;
using Hafr.Evaluation;
using Hafr.Parsing;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapPost("evaluate", (EvaluationModel model) =>
{
    if (!Parser.TryParse(model.Template, out var expression, out var error, out var errorPosition))
    {
        return Results.Problem(new ProblemDetails
        {
            Title = "Failed to parse template.",
            Detail = error,
            Status = StatusCodes.Status400BadRequest,
            Extensions =
            {
                { "position", errorPosition }
            }
        });
    }

    try
    {
        return Results.Json(expression.EvaluateProperties(model.Data).ToList());
    }
    catch (TemplateEvaluationException tee)
    {
        return Results.Problem(new ProblemDetails
        {
            Title = "Failed to evaluate template.",
            Detail = tee.Message,
            Status = StatusCodes.Status422UnprocessableEntity,
            Extensions =
            {
                { "position", tee.Position }
            }
        });
    }
});

app.Run();