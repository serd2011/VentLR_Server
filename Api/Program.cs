using Microsoft.AspNetCore.Mvc;

using API.Configurations;
using API.Middleware.Filters;

var builder = WebApplication.CreateBuilder(args);

// Configurations

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORS_POLICY",
                          policy =>
                          {
                              policy.WithOrigins("http://localhost",
                                                  "https://localhost")
                                                  .AllowAnyHeader()
                                                  .AllowAnyMethod();
                          });
});
builder.Services.ConfigureVersioning();
builder.Services.ConfigureHeaders();
builder.Services.ConfigureDependancies(builder.Configuration);

// Controllers setup
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelAttributesValidationFilter>();
}).AddNewtonsoftJson();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Logging.ClearProviders();

builder.Services.AddLogging(
    builder =>
    {
        builder.AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("NToastNotify", LogLevel.Warning)
               .AddConsole();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseForwardedHeaders();
app.UseAuthorization();
app.MapControllers();
app.Run();
