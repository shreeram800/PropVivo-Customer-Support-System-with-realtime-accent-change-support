using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NAudio.Wave;
using Whisper.net;


namespace RealtimeAccentTransformer.Middleware;

public class AudioStreamMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly WhisperProcessor _whisperProcessor;
    private readonly MemoryStream _audioBuffer = new();
    private readonly IServiceScopeFactory _scopeFactory;

    private const int SampleRate = 16000;
    private const int BitDepth = 16;
    private const int Channels = 1;
    private const int BufferTriggerSize = SampleRate * 2 * 5; // 5 seconds

    public AudioStreamMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;

        var modelPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "ggml-base.en.bin");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Whisper model not found.", modelPath);

        var whisperFactory = WhisperFactory.FromPath(modelPath);
        _whisperProcessor = whisperFactory.CreateBuilder()
            .WithLanguage("en")
            .Build();

        Console.WriteLine("Whisper processor initialized.");
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path == "/audiostream")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("WebSocket connection established.");
                await ProcessAudioStream(webSocket, context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ProcessAudioStream(WebSocket socket, HttpContext context)
    {
        var buffer = new byte[1024 * 4];

        // Optionally extract CallId from query or headers
        var callIdString = context.Request.Query["callId"];
        int.TryParse(callIdString, out int callId);

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                Console.WriteLine("WebSocket connection closed.");
                break;
            }

            var jsonMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            using var jsonDoc = JsonDocument.Parse(jsonMessage);

            if (jsonDoc.RootElement.TryGetProperty("event", out var eventElement) &&
                eventElement.GetString() == "media")
            {
                string audioPayload = jsonDoc.RootElement.GetProperty("media").GetProperty("payload").GetString();
                byte[] audioBytes = Convert.FromBase64String(audioPayload);
                await _audioBuffer.WriteAsync(audioBytes, 0, audioBytes.Length);

                if (_audioBuffer.Length > BufferTriggerSize)
                {
                    Console.WriteLine("Buffer full, processing audio...");
                    _audioBuffer.Position = 0;

                    var convertedPcmStream = ConvertUl_awToPcm(_audioBuffer);
                    string transcript = await TranscribeAudio(convertedPcmStream);
                    _audioBuffer.SetLength(0);

                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        Console.WriteLine($"Transcript: {transcript}");
                        byte[] americanAccentAudio = await SynthesizeSpeech(transcript);

                        if (americanAccentAudio.Length > 0)
                        {
                            Console.WriteLine($"Generated {americanAccentAudio.Length} bytes of audio.");

                            // Save audio to a public file (optional)
                            string tempFile = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid()}.wav");
                            await File.WriteAllBytesAsync(tempFile, americanAccentAudio);

                            // Save to DB
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            var modulation = new VoiceModulation
                            {
                                CallId = callId > 0 ? callId : 0,
                                FromAccent = "Indian",
                                ToAccent = "American",
                                Transcript = transcript,
                                SynthesizedAudioPath = tempFile,
                                IsRealTime = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            db.VoiceModulations.Add(modulation);
                            await db.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }

    private MemoryStream ConvertUl_awToPcm(MemoryStream ulawStream)
    {
        var outStream = new MemoryStream();
        var inFormat = new WaveFormat(8000, 8, 1);
        var outFormat = new WaveFormat(SampleRate, BitDepth, Channels);

        using var reader = new RawSourceWaveStream(ulawStream, inFormat);
        var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);
        using var resampler = new MediaFoundationResampler(pcmStream, outFormat);
        WaveFileWriter.WriteWavFileToStream(outStream, resampler);

        outStream.Position = 0;
        return outStream;
    }

    private async Task<string> TranscribeAudio(Stream pcmAudioStream)
    {
        var transcriptBuilder = new StringBuilder();
        await foreach (var segment in _whisperProcessor.ProcessAsync(pcmAudioStream))
        {
            transcriptBuilder.Append(segment.Text);
        }
        return transcriptBuilder.ToString();
    }

    private async Task<byte[]> SynthesizeSpeech(string text)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"synthesize.py \"{text}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return Array.Empty<byte>();

        string outputFilePath = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"TTS script error: {errors}");
            return Array.Empty<byte>();
        }

        outputFilePath = outputFilePath.Trim();
        if (File.Exists(outputFilePath))
        {
            var audioBytes = await File.ReadAllBytesAsync(outputFilePath);
            File.Delete(outputFilePath);
            return audioBytes;
        }

        return Array.Empty<byte>();
    }

    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _audioBuffer?.Dispose();
    }
}
