using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<ToDo>();

app.MapGet("/todos", () => TypedResults.Ok(todos));

app.MapGet("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id) =>
{

    var todo = todos.SingleOrDefault(t => t.Id == id);

    return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);

});

app.MapPost("/todos", (ToDo todo) =>
{
    todos.Add(todo);
    return TypedResults.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("todos/{id}", Results<Ok<ToDo>, NotFound> (int id, ToDo todoParams) =>
{

    var todo = todos.SingleOrDefault(t => t.Id == id);

    if (todo is null)
    {
        return TypedResults.NotFound();
    }

    todos.Remove(todo);
    todos.Add(todoParams);
    return TypedResults.Ok(todo);
});

app.MapDelete("todos/{id}", Results<NoContent, NotFound> (int id) =>
{
    var todo = todos.SingleOrDefault(t => t.Id == id);

    if (todo is null)
    {
        return TypedResults.NotFound();
    }
    todos.Remove(todo);
    return TypedResults.NoContent();
});

app.Run();

public record ToDo(int Id, string Name, DateTime DueDate, bool IsCompleted);
