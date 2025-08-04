using RealtimeAccentTransformer.Hubs;
using RealtimeAccentTransformer.Interfaces;
using RealtimeAccentTransformer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();

// IMPORTANT: Register VoskProcessor as a Singleton to load the model only once.
builder.Services.AddSingleton<IVoskProcessor, VoskProcessor>();

// Register PiperTtsService as Scoped or Transient, as it's lightweight.
builder.Services.AddScoped<IPiperTtsService, PiperTtsService>();

// Add configuration for Piper settings if you create an appsettings.json section
/* Example appsettings.json:
"Piper": {
  "PythonExecutable": "C:\\Users\\YourUser\\AppData\\Local\\Programs\\Python\\Python39\\python.exe",
  "ScriptPath": "Scripts/synthesize.py",
  "ModelPath": "AiModels/en_US-lessac-medium.onnx"
}
*/
builder.Services.AddOptions();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapHub<AudioHub>("/audiohub"); // Map the SignalR hub

app.Run();