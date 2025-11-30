using FaultDiagnosis.Core.Configuration;
using FaultDiagnosis.Core.Interfaces;
using FaultDiagnosis.Infrastructure.Ollama;
using FaultDiagnosis.Infrastructure.OpenAI;
using FaultDiagnosis.Infrastructure.Qdrant;
using Polly;
using Qdrant.Client;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace FaultDiagnosis.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

            // Enable gRPC over HTTP
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddValidatorsFromAssemblyContaining<FaultDiagnosis.API.Validators.DiagnosisRequestValidator>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configuration
            builder.Services.Configure<FaultDiagnosisSettings>(builder.Configuration.GetSection("FaultDiagnosis"));
            var settings = builder.Configuration.GetSection("FaultDiagnosis").Get<FaultDiagnosisSettings>() ?? new FaultDiagnosisSettings();

            // Infrastructure Services
            if (settings.LLMProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddHttpClient<ILLMClient, OpenAIClient>(client =>
                {
                    client.BaseAddress = new Uri(settings.OpenAIEndpoint ?? "https://api.openai.com/");
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
                .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
            }
            else
            {
                builder.Services.AddHttpClient<ILLMClient, OllamaClient>(client =>
                {
                    client.BaseAddress = new Uri(settings.OllamaUrl);
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
                .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
            }

            builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(new Uri(settings.QdrantUrl)));
            builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Only enable HTTPS redirection if NOT running in a container
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
