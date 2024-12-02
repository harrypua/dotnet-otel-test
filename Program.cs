using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService("TestOTel"));
    
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true; //include this so that otel evaluates the object within the msg
    options.ParseStateValues = true;
    
    options.AddOtlpExporter(exporterOptions =>
    {
        exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        exporterOptions.Endpoint = new Uri("http://localhost:3100/otlp/v1/logs"); // Loki OTLP endpoint
    });
    options.AddConsoleExporter(config =>{
        //config.Format = OpenTelemetry.Exporter.ConsoleExporterFormat.Json;
    }); // Add this for testing/debugging
});

builder.Logging.AddFilter("OpenTelemetry",LogLevel.Debug);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    app.Logger.LogInformation("Generating random weather forecast");
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    app.Logger.LogInformation("Returning weather forecast: {@WeatherForecast}",forecast);
    //StackTrace t = new StackTrace(true);
    //app.Logger.LogError("Testing Error log with forecast: {@StackTrace}",t);
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
