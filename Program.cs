using AgenticBot.Plugins;
using AgenticBot.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add controllers
builder.Services.AddControllers();

// Add HttpClient for web plugins and external APIs
builder.Services.AddHttpClient();

// Register the External Incident API Service (fetches real data)
// Use Singleton to match DirectActionService scope
builder.Services.AddSingleton<IExternalIncidentApiService, ExternalIncidentApiService>();

// Register the Direct Action Service (works without LLM!)
builder.Services.AddSingleton<IDirectActionService>(sp =>
{
    var externalApiService = sp.GetRequiredService<IExternalIncidentApiService>();
    var logger = sp.GetRequiredService<ILogger<DirectActionService>>();
    return new DirectActionService(externalApiService, logger);
});

// Configure Semantic Kernel with Ollama (FREE local LLM)
// Make sure Ollama is running: https://ollama.ai
// Pull a model: ollama pull llama3.2 (or mistral, phi3, etc.)
builder.Services.AddSingleton<Kernel>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Ollama default endpoint
    var ollamaEndpoint = new Uri(builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434");
    
    // Model to use (llama3.2 is recommended for tool calling, but you can use others)
    var modelId = builder.Configuration["Ollama:Model"] ?? "llama3.2";

    var kernelBuilder = Kernel.CreateBuilder();

    // Add Ollama chat completion
    #pragma warning disable SKEXP0070 // Ollama connector is experimental
    kernelBuilder.AddOllamaChatCompletion(
        modelId: modelId,
        endpoint: ollamaEndpoint
    );
    #pragma warning restore SKEXP0070

    // Build the kernel
    var kernel = kernelBuilder.Build();

    // Register plugins (tools) that make the agent "agentic"
    kernel.Plugins.AddFromType<TimePlugin>("Time");
    kernel.Plugins.AddFromType<MathPlugin>("Math");
    kernel.Plugins.AddFromObject(new WebSearchPlugin(httpClientFactory.CreateClient()), "Web");
    kernel.Plugins.AddFromType<TaskManagerPlugin>("Tasks");
    kernel.Plugins.AddFromType<IncidentManagementPlugin>("Incidents");

    return kernel;
});

// Register the agent service (used for complex queries requiring LLM)
builder.Services.AddSingleton<IAgentService, AgentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve static files (for the web UI)
app.UseStaticFiles();

// Enable default files (index.html)
app.UseDefaultFiles();

// Map controller routes
app.MapControllers();

// Fallback to index.html for SPA-style routing
app.MapFallbackToFile("index.html");

Console.WriteLine("========================================");
Console.WriteLine("🔔 Incident Management Agent starting...");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("✅ Direct actions work immediately!");
Console.WriteLine("   - Incident data from JSONPlaceholder API");
Console.WriteLine("   - No LLM required for incident queries");
Console.WriteLine();
Console.WriteLine("📡 API Integration:");
Console.WriteLine("   - Data Source: https://jsonplaceholder.typicode.com");
Console.WriteLine("   - Real-time incident fetching enabled");
Console.WriteLine("   - 5-minute cache for performance");
Console.WriteLine();
Console.WriteLine("🤖 For advanced AI features:");
Console.WriteLine("1. Install Ollama from https://ollama.ai");
Console.WriteLine("2. Run 'ollama serve' in a terminal");
Console.WriteLine("3. Pull a model: 'ollama pull llama3.2'");
Console.WriteLine();
Console.WriteLine("========================================");

app.Run();
