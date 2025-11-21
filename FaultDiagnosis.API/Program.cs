using FaultDiagnosis.Core.Interfaces;
using FaultDiagnosis.Infrastructure.Ollama;
using FaultDiagnosis.Infrastructure.Qdrant;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure Services
builder.Services.AddHttpClient<ILLMClient, OllamaClient>(client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient(new Uri("http://127.0.0.1:6334")));
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
