using BillingService.Application.Interfaces;
using BillingService.Application.Services;
using BillingService.Infrastructure.External;
using BillingService.Infrastructure.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/billing-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add API Explorer and Swagger with explicit configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Billing Service API",
        Version = "v1",
        Description = "Microservices Billing System with Lago Integration"
    });
});

// Register HttpClient for Lago
builder.Services.AddHttpClient<ILagoService, LagoService>();

// Register application services
builder.Services.AddScoped<IBillingService, BillingApplicationService>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for debugging
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing Service API V1");
    c.RoutePrefix = "swagger"; // This makes Swagger available at /swagger
});

// Add a root redirect to swagger for easier access
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Add some logging to see what URLs the app is listening on
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started. Swagger UI available at: /swagger");
});

app.Run();