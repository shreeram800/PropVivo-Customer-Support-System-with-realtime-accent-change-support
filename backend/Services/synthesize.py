# synthesize.py
import pyttsx3
import sys
import io

# The first argument is the text to synthesize
text_to_speak = sys.argv[1]

engine = pyttsx3.init()

# Optional: Set properties like voice. You may need to find the ID for a US accent.
# voices = engine.getProperty('voices')
# engine.setProperty('voice', voices[0].id) # Find an en-US voice

# Instead of saving to a file, we write to a memory stream (BytesIO)
# and then print the raw bytes to standard output.
# This avoids disk I/O and is faster.
# Note: pyttsx3 doesn't directly support this. This is a conceptual workaround.
# A more robust library like Coqui TTS is better for this.
# For simplicity here, we save to a file and the C# app will read it.

output_file = "output.wav"
engine.save_to_file(text_to_speak, output_file)
engine.runAndWait()

# Print the file path to stdout so C# knows where to find the audio
print(output_file, flush=True)