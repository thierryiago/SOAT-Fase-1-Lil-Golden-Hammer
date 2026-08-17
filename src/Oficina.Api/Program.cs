using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Oficina.Application;
using Oficina.Application.Common;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Database"));
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina API",
        Version = "v1",
        Description = "MVP RESTful para gestao de clientes, pecas e ordens de servico."
    });
    var xmlDocumentation = Path.Combine(AppContext.BaseDirectory, "Oficina.Api.xml");
    options.IncludeXmlComments(xmlDocumentation);
});
var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title = "It was not possible to process the request.",
            status = statusCode,
            detail = statusCode == StatusCodes.Status500InternalServerError
                ? null
                : exception?.Message,
            traceId = context.TraceIdentifier
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina API v1");
});

app.UseHttpsRedirection();
app.MapHealthChecks("/health", new HealthCheckOptions { AllowCachingResponses = false });
app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();

public partial class Program;
