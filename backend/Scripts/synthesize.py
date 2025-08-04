import argparse
import logging
import wave
import sys
import os

try:
    from piper.voice import PiperVoice
except ImportError:
    print("Error: The 'piper-phonemize' package is not installed.")
    print("Please install it using: pip install piper-phonemize")
    sys.exit(1)

# --- HARDCODED MODEL PATH ---
# The script will now ONLY use this model.
MODEL_PATH = os.path.join("piper-voices", "en", "en_US", "libritts", "high", "en_US-libritts-high.onnx")

# Set up logging to stderr, so C# can capture it.
logging.basicConfig(level=logging.INFO, stream=sys.stderr, format='[Python] %(levelname)s: %(message)s')

def synthesize(text, model_path, output_path):
    """
    Synthesizes audio from text using a Piper model and saves it to a WAV file.
    """
    try:
        logging.info(f"Attempting to load model from: {model_path}")
        voice = PiperVoice.load(model_path)
        logging.info("Model loaded successfully.")

        with wave.open(output_path, "wb") as wav_file:
            # Set WAV parameters
            wav_file.setnchannels(1)
            wav_file.setsampwidth(2)
            wav_file.setframerate(voice.config.sample_rate)
            
            logging.info(f"Synthesizing text and writing final audio bytes: '{text}'")
            
            # --- FINAL, EVIDENCE-BASED FIX ---
            # The generator yields 'AudioChunk' objects. We access the
            # '.audio_int16_bytes' property to get the raw bytes for each chunk.
            for chunk in voice.synthesize(text):
                wav_file.writeframes(chunk.audio_int16_bytes)
            
            logging.info(f"Synthesis complete. Audio should be at: {output_path}")

    except Exception as e:
        logging.error(f"An error occurred during synthesis: {e}", exc_info=True)
        if os.path.exists(output_path):
            os.remove(output_path)
        sys.exit(1)
        
if __name__ == "__main__":
    # Note: We removed the --model argument from here.
    parser = argparse.ArgumentParser(description="Synthesize audio from text using Piper TTS.")
    parser.add_argument("text", type=str, help="The text to synthesize.")
    parser.add_argument("--output", required=True, help="Path to save the output WAV file.")
    
    args = parser.parse_args()

    if not os.path.exists(MODEL_PATH):
        logging.error(f"Hardcoded model path not found: {MODEL_PATH}")
        sys.exit(1)
        
    # Use the hardcoded MODEL_PATH instead of one from arguments.
    synthesize(args.text, MODEL_PATH, args.output)