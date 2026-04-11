using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Registry;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Defining a Standard Resilience Pipeline (Retry + Circuit Breaker + Timeout)
builder.Services.AddResiliencePipeline("default", pipeline =>
{
    pipeline.AddRetry(new()
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromSeconds(2)
    });

    pipeline.AddCircuitBreaker(new()
    {
        FailureRatio = 0.5, // 50% failures trigger the circuit
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    pipeline.AddTimeout(TimeSpan.FromSeconds(15));
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRouting();
app.UseCors("AllowAll");

// Applying the Resilience Pipeline to the proxy 
// Note: We'll use a custom middleware to apply the 'default' pipeline to all requests
app.MapReverseProxy(proxyPipeline => 
{
    proxyPipeline.Use((context, next) => 
    {
        var resiliencePipelineProvider = context.RequestServices.GetRequiredService<ResiliencePipelineProvider<string>>();
        var pipeline = resiliencePipelineProvider.GetPipeline("default");
        
        return pipeline.ExecuteAsync(async ct => await next().ConfigureAwait(false), context.RequestAborted).AsTask();
    });
});

try
{
    Log.Information("Starting Gateway Service...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
