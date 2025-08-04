using NAudio.Wave;
using RealtimeAccentTransformer.Interfaces;
using System.Text;
using System.Text.Json;
using Vosk;

namespace RealtimeAccentTransformer.Services
{
    public class VoskProcessor : IVoskProcessor, IDisposable
    {
        private readonly Model _voskModel;
        private const int SampleRate = 16000;
        private const int BitDepth = 16;
        private const int Channels = 1;

        public VoskProcessor()
        {
            // Load the model once when the service is instantiated.
            var modelPath = Path.Combine(AppContext.BaseDirectory, "AiModels", "vosk-model-small-en-in-0.4");
            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException($"Vosk model not found at path: {modelPath}");
            }
            Vosk.Vosk.SetLogLevel(-1); // Suppress verbose logs
            _voskModel = new Model(modelPath);

            Console.WriteLine("✅ Vosk Model Initialized (Singleton).");
        }

        public Stream ConvertUlawToPcm(Stream ulawAudioStream)
        {
            var outStream = new MemoryStream();
            var muLawFormat = WaveFormat.CreateMuLawFormat(8000, 1);
            var pcmFormat = new WaveFormat(SampleRate, BitDepth, Channels);

            using var reader = new RawSourceWaveStream(ulawAudioStream, muLawFormat);
            using var converter = new WaveFormatConversionStream(pcmFormat, reader);
            WaveFileWriter.WriteWavFileToStream(outStream, converter);
            
            outStream.Position = 0;
            return outStream;
        }

        public Task<string> TranscribeAsync(Stream wavAudioStream)
{
    // This is a CPU-bound and synchronous operation.
    // We wrap it in Task.Run to avoid blocking the main thread.
    return Task.Run(() =>
    {
        using var waveReader = new WaveFileReader(wavAudioStream);

        var requiredFormat = new WaveFormat(SampleRate, BitDepth, Channels);
        using var resampler = new MediaFoundationResampler(waveReader, requiredFormat);
        
        var resultBuilder = new StringBuilder();
        using var recognizer = new VoskRecognizer(_voskModel, SampleRate);
        recognizer.SetMaxAlternatives(0);
        recognizer.SetWords(false);

        var buffer = new byte[4096];
        int bytesRead;

        // CORRECTED LINE: Use the synchronous .Read() method
        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
        {
            recognizer.AcceptWaveform(buffer, bytesRead);
        }

        var finalResultJson = recognizer.FinalResult();
        resultBuilder.Append(ExtractTextFromJson(finalResultJson));

        Console.WriteLine($"📝 Transcription completed: {resultBuilder.ToString().Trim()}");

        return resultBuilder.ToString().Trim();
    });
}
                
        private string ExtractTextFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("text", out var textProp))
                {
                    return textProp.GetString() ?? "";
                }
            }
            catch (JsonException) { /* Ignore invalid JSON */ }
            return string.Empty;
        }

        public void Dispose()
        {
            _voskModel?.Dispose();
        }
    }
}