using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NAudio.Wave;
using Whisper.net;


namespace RealtimeAccentTransformer.Middleware;

public class AudioStreamMiddleware : IDisposable
{
    private readonly RequestDelegate _next;

    // AI Components - Initialize once and reuse
    private readonly WhisperProcessor _whisperProcessor;
    private readonly MemoryStream _audioBuffer = new();

    // Configuration
    private const int SampleRate = 16000; // 16kHz
    private const int BitDepth = 16;      // 16-bit
    private const int Channels = 1;       // Mono
    private const int BufferTriggerSize = SampleRate * 2 * 5; // Process every 5 seconds of audio

    public AudioStreamMiddleware(RequestDelegate next)
    {
        _next = next;

        var modelPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "ggml-base.en.bin");
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Whisper model not found.", modelPath);
        }

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
                await ProcessAudioStream(webSocket);
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

    private async Task ProcessAudioStream(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];

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

            if (jsonDoc.RootElement.TryGetProperty("event", out var eventElement) && eventElement.GetString() == "media")
            {
                string audioPayload = jsonDoc.RootElement.GetProperty("media").GetProperty("payload").GetString();
                byte[] audioBytes = Convert.FromBase64String(audioPayload);

                // Add incoming audio to our buffer
                await _audioBuffer.WriteAsync(audioBytes, 0, audioBytes.Length);

                // If buffer is large enough, process it
                if (_audioBuffer.Length > BufferTriggerSize)
                {
                    Console.WriteLine("Buffer full, processing audio...");
                    
                    // Reset buffer position for reading
                    _audioBuffer.Position = 0;

                    var convertedPcmStream = ConvertUl_awToPcm(_audioBuffer);
                    string transcript = await TranscribeAudio(convertedPcmStream);

                    // Clear buffer for next chunks
                    _audioBuffer.SetLength(0);
                    
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        Console.WriteLine($"Transcript: {transcript}");
                        
                        // 2. TRANSFORM (Text-to-Speech with American Accent)
                        byte[] americanAccentAudio = await SynthesizeSpeech(transcript);
                        
                        if (americanAccentAudio.Length > 0)
                        {
                             Console.WriteLine($"Generated {americanAccentAudio.Length} bytes of audio.");
                            // 3. PLAYBACK TO CUSTOMER (Conceptual)
                            // This requires using the Twilio REST API to update the call.
                            // You would host the 'americanAccentAudio' at a public URL
                            // and use the API to tell Twilio to <Play> that URL.
                            // Example: await PlayAudioToCall(callSid, publicUrlToAudio);
                        }
                    }
                    // =================================================================
                    // 🚀 AI PIPELINE ENDS HERE 🚀
                    // =================================================================
                }
            }
        }
    }

    private MemoryStream ConvertUl_awToPcm(MemoryStream ulawStream)
    {
        var outStream = new MemoryStream();
        // Twilio format: 8kHz, 8-bit, 1-channel, MuLaw
        var  inFormat = new WaveFormat(8000, 8, 1);
        // Whisper format: 16kHz, 16-bit, 1-channel, PCM
        var outFormat = new WaveFormat(SampleRate, BitDepth, Channels);

        using (var reader = new RawSourceWaveStream(ulawStream, inFormat))
        {
            // First, decode from MuLaw to PCM (at 8kHz)
            var pcmStream = WaveFormatConversionStream.CreatePcmStream(reader);
            // Then, resample from 8kHz to 16kHz
            using (var resampler = new MediaFoundationResampler(pcmStream, outFormat))
            {
                WaveFileWriter.WriteWavFileToStream(outStream, resampler);
            }
        }
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
            FileName = "python", // or "python3" depending on your system
            Arguments = $"synthesize.py \"{text}\"", // Pass text as an argument
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return Array.Empty<byte>();

        // Read the file path from the script's output
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
            File.Delete(outputFilePath); // Clean up the temp file
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