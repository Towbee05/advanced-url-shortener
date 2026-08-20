using System.Data;
using System.Reflection;
using UrlShortener.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
.AddUserSecrets(Assembly.GetExecutingAssembly())
.AddEnvironmentVariables();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IConnectionFactory, NpgsqlConnectionFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
