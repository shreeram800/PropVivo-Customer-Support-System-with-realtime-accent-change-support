using Microsoft.AspNetCore.Mvc;
using RealtimeAccentTransformer.Interfaces;

namespace RealtimeAccentTransformer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioTestController : ControllerBase
    {
        private readonly IVoskProcessor _voskProcessor;
        private readonly IPiperTtsService _piperTtsService;

        public AudioTestController(IVoskProcessor voskProcessor, IPiperTtsService piperTtsService)
        {
            _voskProcessor = voskProcessor;
            _piperTtsService = piperTtsService;
        }

        [HttpPost("transform-accent")]
        public async Task<IActionResult> TransformAccent(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No audio file provided.");
            }

            try
            {
                // 1. Get audio stream
                await using var audioStream = audioFile.OpenReadStream();

                // 2. Transcribe (assuming the uploaded file is already PCM WAV)
                var transcript = await _voskProcessor.TranscribeAsync(audioStream);
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return Ok(new { transcript, message = "No speech detected." });
                }

                // 3. Synthesize
                var synthesizedAudio = await _piperTtsService.SynthesizeAsync(transcript);
                if (synthesizedAudio == null)
                {
                    return StatusCode(500, "Failed to synthesize audio.");
                }

                // 4. Return the new audio file
                return File(synthesizedAudio, "audio/wav", "synthesized_audio.wav");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal error occurred: {ex.Message}");
            }
        }
    }
}