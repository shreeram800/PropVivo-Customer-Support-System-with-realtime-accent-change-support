using RealtimeAccentTransformer.Interfaces;
using System.Diagnostics;

namespace RealtimeAccentTransformer.Services
{
    public class PiperTtsService : IPiperTtsService
    {
        private readonly string _pythonExecutable;
        private readonly string _scriptPath;
        private readonly string _modelPath;

        public PiperTtsService(IConfiguration configuration)
        {
            // Best practice: Make paths configurable
            _pythonExecutable = configuration["Piper:PythonExecutable"] ?? "python";
            _scriptPath = Path.Combine(AppContext.BaseDirectory, configuration["Piper:ScriptPath"] ?? "Scripts/synthesize.py");
            _modelPath = Path.Combine(AppContext.BaseDirectory, configuration["Piper:ModelPath"] ?? "AiModels/en_US-lessac-medium.onnx");
        }

        public async Task<byte[]?> SynthesizeAsync(string text)


        
        {
            Console.WriteLine($"📝 Synthesizing audio for text: '{text}'");

            if (string.IsNullOrWhiteSpace(text)) return null;

            var tempOutputFile = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
            
            var psi = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = $"\"{_scriptPath}\" \"{text}\" --output \"{tempOutputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null) throw new Exception("Failed to start Piper TTS process.");

                // Asynchronously read stdout and stderr
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[PiperTTS Error] Process exited with code {process.ExitCode}: {error}");
                    return null;
                }

                if (!File.Exists(tempOutputFile))
                {
                    Console.WriteLine($"[PiperTTS Error] Output file was not created. Details: {error}");
                    return null;
                }

                return await File.ReadAllBytesAsync(tempOutputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PiperTTS Exception] {ex.Message}");
                return null;
            }
            finally
            {
                // Ensure the temp file is always deleted
                if (File.Exists(tempOutputFile))
                {
                    File.Delete(tempOutputFile);
                }
            }
        }
    }
}