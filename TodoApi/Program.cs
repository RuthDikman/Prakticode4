using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TodoApi;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql("name=ToDoDB", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.36-mysql")));

builder.Services.AddScoped<ItemService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Items",
        Description = "An ASP.NET Core Web API for managing ToDo items",
        TermsOfService = new Uri("http://localhost:5171/"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("http://localhost:5171/")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("http://localhost:5171/")
        }
    });
});

var app = builder.Build();

app.UseSwagger(options =>
{
    options.SerializeAsV2 = true;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseCors();
app.MapGet("/items", ([FromServices] ItemService service) =>
{
    var items = service.GetAllItems();
    return Results.Ok(items);
});
app.MapPost("/items", async ([FromBody] Item newItem, ItemService service) =>
{
    if (newItem == null)
    {
        return Results.BadRequest("Invalid item data");
    }

    await service.AddItemAsync(newItem);
    return Results.Created($"/items/{newItem.Id}", newItem);
});
app.MapPut("/items/{id}", async ([FromBody] Item updatedItem,int id, ItemService service) =>
{
    if (updatedItem == null)
    {
        return Results.BadRequest("Invalid item data");
    }

    var existingItem = await service.GetItemByIdAsync(id);
    if (existingItem == null)
    {
        return Results.NotFound("Item not found");
    }

    existingItem.IsComplete = updatedItem.IsComplete;

    await service.UpdateItemAsync(existingItem);

    return Results.Ok(existingItem);
});
app.MapDelete("/items/{id}", async (int id, [FromServices] ItemService service) =>
{
    var existingItem = await service.GetItemByIdAsync(id);
    
    if (existingItem == null)
    {
        return Results.NotFound("Item not found");
    }

    await service.DeleteItemAsync(existingItem);

    return Results.Ok($"Item with ID {id} deleted successfully");
});
app.Run();
class ItemService { 

    private readonly ToDoDbContext _context;

    public ItemService(ToDoDbContext context)
    {
        _context = context;
    }
    public IEnumerable<Item> GetAllItems()
    {
        return _context.Items.ToList();
    }
    public async Task<Item> AddItemAsync(Item newItem)
    {
        _context.Items.Add(newItem);
        await _context.SaveChangesAsync();
        return newItem;
    }
    public async Task UpdateItemAsync(Item item)
    {
        _context.Items.Update(item);
        await _context.SaveChangesAsync();
    }
    public async Task<Item> GetItemByIdAsync(int id)
    {
        return await _context.Items.FindAsync(id);
    }
        public async Task DeleteItemAsync(Item item)
    {
        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
    }
}