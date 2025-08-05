using RealtimeAccentTransformer.Hubs;
using RealtimeAccentTransformer.Interfaces;
using RealtimeAccentTransformer.Services;


var builder = WebApplication.CreateBuilder(args);

// Register controllers
builder.Services.AddControllers();

// Register SignalR
builder.Services.AddSignalR();

// Register VoskProcessor as Singleton (loads model once)
builder.Services.AddSingleton<IVoskProcessor, VoskProcessor>();

// Register PiperTTS as Scoped (recreated per request)
builder.Services.AddScoped<IPiperTtsService, PiperTtsService>();

// Optional: load Piper config from appsettings.json
builder.Services.AddOptions();

// CORS for frontend (adjust origin as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); // Change to specific frontend origin if needed
    });
});

var app = builder.Build();

// Use developer exception page in dev environment
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();

// Map routes
app.MapControllers();
app.MapHub<AudioHub>("/audiohub"); // WebSocket route


app.Run();
