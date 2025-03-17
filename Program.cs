using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

var app = builder.Build();

//Middlewares 
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} started!]");
    await next();
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow} finished!]");
});

app.MapGet("/", () => "Hello World!");

app.MapGet("/todos", (ITaskService service) => TypedResults.Ok(service.GetToDos()));

app.MapGet("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id, ITaskService service) =>
{

    var todo = service.GetToDoById(id);
    return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);

});

app.MapPost("/todos", (ToDo todo, ITaskService service) =>
{
    System.Console.WriteLine(todo);
    var task = service.AddToDo(todo);
    return TypedResults.Created($"/todos/{todo.Id}", task);

}).AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<ToDo>(0);
    var errors = new Dictionary<string, string[]>();

    // System.Console.WriteLine(taskArgument);

    // System.Console.WriteLine(nameof(taskArgument.DueDate));

    // System.Console.WriteLine(taskArgument.IsCompleted);

    if (taskArgument.DueDate < DateTime.UtcNow)
    {
        errors.Add(nameof(taskArgument.DueDate), ["Due date must be in the future"]);
    }

    if (taskArgument.IsCompleted)
    {
        errors.Add(nameof(taskArgument.IsCompleted), ["Cannot add completed todo"]);
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPut("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id, ToDo todoInput, ITaskService service) =>
{

    System.Console.WriteLine($"id: {id}");

    var todo = service.GetToDoById(id);

    if (todo is null)
    {
        return TypedResults.NotFound();
    }

    service.DeleteTodoById(id);
    service.AddToDo(todoInput);

    return TypedResults.Ok(todo);
});

app.MapDelete("/todos/{id}", Results<NoContent, NotFound> (int id, ITaskService service) =>
{
    var todo = service.GetToDoById(id);

    if (todo is null)
    {
        return TypedResults.NotFound();
    }

    service.DeleteTodoById(id);
    return TypedResults.NoContent();
});

app.Run();

public record ToDo(int Id, string Name, DateTime DueDate, bool IsCompleted);

interface ITaskService
{
    ToDo? GetToDoById(int id);

    List<ToDo> GetToDos();

    void DeleteTodoById(int id);

    ToDo AddToDo(ToDo task);
}

public class InMemoryTaskService : ITaskService
{
    private List<ToDo> todos = new List<ToDo>();

    public ToDo GetToDoById(int id)
    {
        var task = todos.SingleOrDefault(t => t.Id == id);
        return task!;
    }

    public List<ToDo> GetToDos()
    {
        return todos;
    }

    public void DeleteTodoById(int id)
    {
        var task = todos.SingleOrDefault(t => t.Id == id);
        todos.Remove(task!);
    }

    public ToDo AddToDo(ToDo task)
    {
        todos.Add(task);
        return task;
    }

}