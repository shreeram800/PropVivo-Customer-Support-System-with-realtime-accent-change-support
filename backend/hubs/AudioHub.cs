using Microsoft.AspNetCore.SignalR;
using RealtimeAccentTransformer.Interfaces;
using System.Collections.Concurrent;

namespace RealtimeAccentTransformer.Hubs
{
    public class AudioHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> userConnections = new();
        private readonly IVoskProcessor _voskProcessor;
        private readonly IPiperTtsService _piperTtsService;

        public AudioHub(IVoskProcessor voskProcessor, IPiperTtsService piperTtsService)
        {
            _voskProcessor = voskProcessor;
            _piperTtsService = piperTtsService;
        }

        public async Task Register(string userId)
        {
            userConnections[userId] = Context.ConnectionId;
            Console.WriteLine($"✅ User '{userId}' registered with connection ID {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // Agent sends audio, which is transformed and sent to the user.
        // Audio is expected as base64 encoded µ-law bytes.
        public async Task SendAgentAudio(string base64UlawAudio, string targetUserId)
        {
            if (!userConnections.TryGetValue(targetUserId, out var targetConnectionId))
            {
                Console.WriteLine($"⚠️ Target user '{targetUserId}' not found.");
                return;
            }
            
            Console.WriteLine($"🎙️ Received agent audio for user '{targetUserId}'. Processing...");

            try
            {
                // 1. Decode Base64 and convert µ-law to PCM
                var ulawBytes = Convert.FromBase64String(base64UlawAudio);
                using var ulawStream = new MemoryStream(ulawBytes);
                using var pcmStream = _voskProcessor.ConvertUlawToPcm(ulawStream);

                // 2. Transcribe the PCM audio to text
                var transcript = await _voskProcessor.TranscribeAsync(pcmStream);
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    Console.WriteLine("📝 Transcription resulted in empty text. Nothing to synthesize.");
                    return;
                }
                Console.WriteLine($"📝 Transcript: '{transcript}'");
                await Clients.Caller.SendAsync("ReceiveTranscript", transcript); // Send transcript back to agent for UI

                // 3. Synthesize new audio from the text
                var synthesizedAudioBytes = await _piperTtsService.SynthesizeAsync(transcript);
                if (synthesizedAudioBytes == null || synthesizedAudioBytes.Length == 0)
                {
                    Console.WriteLine("❌ TTS synthesis failed or produced empty audio.");
                    return;
                }
                Console.WriteLine($"🔊 Synthesis successful ({synthesizedAudioBytes.Length} bytes). Sending to user.");

                // 4. Send the synthesized audio to the target user
                var synthesizedAudioBase64 = Convert.ToBase64String(synthesizedAudioBytes);
                await Clients.Client(targetConnectionId).SendAsync("ReceiveSynthesizedAudio", synthesizedAudioBase64);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred in SendAgentAudio: {ex.Message}");
                await Clients.Caller.SendAsync("ProcessingError", "An error occurred during audio processing.");
            }
        }

        // User sends audio, which is relayed directly to the agent.
        public async Task SendUserAudio(string base64RawAudio, string targetAgentId)
        {
            if (!userConnections.TryGetValue(targetAgentId, out var targetConnectionId))
            {
                Console.WriteLine($"⚠️ Target agent '{targetAgentId}' not found.");
                return;
            }

            // Directly relay the raw audio without processing
            await Clients.Client(targetConnectionId).SendAsync("ReceiveRawAudio", base64RawAudio);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var item = userConnections.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (!item.Equals(default(KeyValuePair<string, string>)))
            {
                userConnections.TryRemove(item.Key, out _);
                Console.WriteLine($"🚫 User '{item.Key}' disconnected.");
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}