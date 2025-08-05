using Microsoft.AspNetCore.SignalR;
using RealtimeAccentTransformer.Interfaces;

namespace RealtimeAccentTransformer.Hubs
{
    public class AudioHub : Hub
    {
        private readonly IVoskProcessor _voskProcessor;
        private readonly IPiperTtsService _piperTtsService;

        public AudioHub(IVoskProcessor voskProcessor, IPiperTtsService piperTtsService)
        {
            _voskProcessor = voskProcessor;
            _piperTtsService = piperTtsService;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"❌ Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        // ✅ Accepts base64-encoded string
        public async Task SendAudio(string base64Audio)
        {
            try
            {
                Console.WriteLine("📥 Received base64 audio from client");

                byte[] audioData = Convert.FromBase64String(base64Audio);

                await using var stream = new MemoryStream(audioData);

                // Transcribe using Vosk
                var transcript = await _voskProcessor.TranscribeAsync(stream);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    await Clients.Caller.SendAsync("ReceiveTranscript", "", "No speech detected.");
                    return;
                }

                // Synthesize using Piper
                var synthesizedAudio = await _piperTtsService.SynthesizeAsync(transcript);

                if (synthesizedAudio == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to synthesize audio.");
                    return;
                }

                // ✅ Return transcript and synthesized audio (base64)
                await Clients.Caller.SendAsync("ReceiveTranscript", transcript, null);
                await Clients.Caller.SendAsync("ReceiveSynthesizedAudio", synthesizedAudio);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error in SendAudio: " + ex.Message);
                await Clients.Caller.SendAsync("Error", $"Internal error: {ex.Message}");
            }
        }
    }
}
