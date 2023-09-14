using Movies.Api.Mapping;
using Movies.Application;
using Movies.Application.Database;
using Movies.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"] ?? "Server=localhost;Port=5432;Database=movies;User ID=postgres;Password=postgres;");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ValidationMappingMiddleware>();
app.MapControllers();

var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync();

app.Run();