# Real-time Accent Transformer 🗣️➡️🇺🇸

**Real-time Accent Transformer** is an ASP.NET Core 8 application that enables live speech accent transformation during a phone call. It listens to an incoming Twilio Voice call, transcribes the audio using a local Whisper model, and then re-synthesizes the text into audio with an American accent using a Python-based TTS engine.

---

## 🔧 Technologies Used

* **Backend Framework**: ASP.NET Core 8
* **WebSockets**: For real-time audio streaming
* **Twilio Voice**: To receive live phone calls
* **Speech-to-Text**: Whisper via Whisper.net (.NET wrapper for whisper.cpp)
* **Text-to-Speech**: Python (pyttsx3 TTS library)
* **Audio Conversion**: NAudio for audio format manipulation

---

## ⚖️ How It Works

1. **Twilio Connects**: Your Twilio number is set up to forward the call audio to this application via WebSockets.
2. **Audio Buffering & Conversion**: Incoming 8kHz µ-law audio is converted to 16kHz PCM format using NAudio.
3. **Speech Transcription**: Converted audio is transcribed to text via Whisper.
4. **Accent Synthesis**: Transcribed text is passed to a Python script which generates American-accented audio.
5. **(Optional) Playback**: The synthesized audio can be played back to the caller (conceptual stage).

---

## ⚡ Prerequisites

Ensure the following tools and frameworks are installed:

* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* [Python 3.9+](https://www.python.org/)
* [ngrok](https://ngrok.com/)
* A [Twilio Account](https://www.twilio.com/)

---

## 📁 Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/shreeram800/PropVivo-Customer-Support-System-with-realtime-accent-change-support.git
cd PropVivo-Customer-Support-System-with-realtime-accent-change-support
```

### 2. Backend Configuration

#### A. Restore NuGet Packages

```bash
dotnet restore
```

#### B. Download Whisper Model

* Download `ggml-base.en.bin` from [Whisper.cpp](https://huggingface.co/ggerganov/whisper.cpp)
* Place it in a new `Models` folder at the project root: `./Models/ggml-base.en.bin`

### 3. Python TTS Setup

#### A. Ensure `synthesize.py` Exists

* Confirm that `synthesize.py` is in your root directory.

#### B. Install Dependencies

```bash
pip install pyttsx3
```

---

## ✨ Twilio Configuration

### A. Run the Application

```bash
dotnet run
```

### B. Start ngrok

```bash
ngrok http https://localhost:5001
```

Copy the `wss://<code>.ngrok-free.app` URL.

### C. Twilio Phone Number Setup

* Go to Twilio Console > Phone Numbers > Manage Numbers
* Set the **Voice & Fax** webhook for "A CALL COMES IN" to the following:

```xml
<Response>
    <Connect>
        <Stream url="wss://<your-ngrok-code>.ngrok-free.app/audiostream" />
    </Connect>
</Response>
```

---

## ▶️ Run the App

1. Launch the backend:

```bash
dotnet run
```

2. Start ngrok in a separate terminal:

```bash
ngrok http https://localhost:5001
```

3. Call your Twilio number and start speaking. You should see real-time transcriptions in your console.

---

## 🔊 Playback (Conceptual)

To return the synthesized voice to the caller:

1. Upload the generated `.wav` to a public URL (e.g., AWS S3).
2. Use Twilio's REST API to send a `<Play>` TwiML command:

```xml
<Play>https://your-hosted-url.com/converted.wav</Play>
```

*Note: This playback step introduces latency and is not yet implemented in the sample code.*

---

## 🌍 Folder Structure

```
/Models
  └── ggml-base.en.bin
/synthesize.py
/Controllers/AudioStreamController.cs
/Services/WhisperTranscriber.cs
/Program.cs
```

---

## 📊 Project Goals

* Enable real-time speech accent conversion for use cases like customer support and accessibility.
* Demonstrate audio streaming, STT, and TTS integration in .NET with Twilio.

---

## 🧱 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

---

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

---

## 🔗 Resources

* [Whisper.cpp on GitHub](https://github.com/ggerganov/whisper.cpp)
* [Twilio Media Streams](https://www.twilio.com/docs/voice/twiml/stream)
* [pyttsx3 TTS](https://pyttsx3.readthedocs.io/en/latest/)
* [NAudio](https://github.com/naudio/NAudio)
* [ngrok Docs](https://ngrok.com/docs)
