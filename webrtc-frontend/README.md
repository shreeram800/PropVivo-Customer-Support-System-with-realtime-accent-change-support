# 🎙️ Accent Transformer – Real-Time Indian to American Accent Converter

This is a full-stack application that allows users to **upload or record audio**, converts **Indian-accented speech to American-accented voice** using **speech-to-text (Vosk)** and **text-to-speech (Piper)**, and plays back the synthesized audio.

---

## ✨ Features

- Upload or record `.WAV` files
- Transcribe speech using [Whisper](https://github.com/openai/whisper) or [Vosk](https://alphacephei.com/vosk/)
- Convert transcript to American-accented audio using [Piper TTS](https://github.com/rhasspy/piper)
- Play original and converted audio in-browser
- Real-time capable via WebSocket/SignalR (optional)

---

## 📦 Tech Stack

| Frontend              | Backend             | Audio Processing   |
|----------------------|---------------------|---------------------|
| React.js              | ASP.NET Core (C#)   | Whisper/Vosk (STT)  |
| HTML5 Audio           | SignalR/WebSocket   | Piper (TTS)         |

---

## 🔧 Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/accent-transformer.git
cd accent-transformer
2. Install Backend Dependencies (C# ASP.NET Core)
.NET SDK 7.0 or later

Python (for Whisper or Piper if invoked via subprocess)

Install NuGet packages:

NAudio

Whisper.net

SignalR

Swashbuckle.AspNetCore (for Swagger UI, optional)

3. Download Voice and STT Models
🔊 Piper US English Small Model
Model: en_US-small

Download: en_US-small.onnx

Config: config.json

Place both files in a folder like: voices/en_US-small/

🧠 Vosk STT Model
Model: vosk-model-small-en-us-0.15

Download: https://alphacephei.com/vosk/models

Direct ZIP: Download ZIP

Extract and place in: stt-models/en-us/

4. Install Frontend
bash
Copy
Edit
cd client
npm install
Ensure tailwindcss, react, and any audio libraries (like react-media-recorder) are installed.

5. Run the Project
Backend (C#)
bash
Copy
Edit
dotnet run
Frontend (React)
bash
Copy
Edit
npm run dev
🧪 Demo Usage
Visit the frontend page: http://localhost:3000

Upload a .wav file or record your voice.

Click Transform Accent

Listen to your original and the transformed American-accented version!

📁 File Structure (Simplified)
bash
Copy
Edit
accent-transformer/
│
├── client/                 # React Frontend
│   └── index.css
│
├── RealtimeAccentTransformer/   # .NET Core Backend
│   ├── Services/
│   ├── Middleware/
│   └── SignalR/
│
├── voices/
│   └── en_US-small/
│       ├── en_US-small.onnx
│       └── config.json
│
├── stt-models/
│   └── en-us/
│       └── vosk-model-small-en-us-0.15/
✅ Dependencies
Piper

Vosk

Whisper

SignalR

NAudio

React Media Recorder

📄 License
MIT License

🙋‍♂️ Author
Shree Ram
Passionate Full Stack Developer – Java | React | .NET | Audio AI