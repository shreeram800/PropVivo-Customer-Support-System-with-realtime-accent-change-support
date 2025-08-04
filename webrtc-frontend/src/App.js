// Updated App.jsx with plain CSS classes instead of Tailwind
import React, { useState, useRef } from 'react';
import './index.css';

const Spinner = () => <div className="spinner"></div>;

const createWavFile = (audioBuffer) => {
  const numChannels = audioBuffer.numberOfChannels;
  const sampleRate = audioBuffer.sampleRate;
  const format = 1;
  const bitDepth = 16;

  let interleaved;
  if (numChannels === 2) {
    const left = audioBuffer.getChannelData(0);
    const right = audioBuffer.getChannelData(1);
    interleaved = new Float32Array(left.length + right.length);
    for (let i = 0, j = 0; i < left.length; i++) {
      interleaved[j++] = left[i];
      interleaved[j++] = right[i];
    }
  } else {
    interleaved = audioBuffer.getChannelData(0);
  }

  const dataLength = interleaved.length * (bitDepth / 8);
  const buffer = new ArrayBuffer(44 + dataLength);
  const view = new DataView(buffer);

  writeString(view, 0, 'RIFF');
  view.setUint32(4, 36 + dataLength, true);
  writeString(view, 8, 'WAVE');
  writeString(view, 12, 'fmt ');
  view.setUint32(16, 16, true);
  view.setUint16(20, format, true);
  view.setUint16(22, numChannels, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * numChannels * (bitDepth / 8), true);
  view.setUint16(32, numChannels * (bitDepth / 8), true);
  view.setUint16(34, bitDepth, true);
  writeString(view, 36, 'data');
  view.setUint32(40, dataLength, true);

  let offset = 44;
  for (let i = 0; i < interleaved.length; i++, offset += 2) {
    let s = Math.max(-1, Math.min(1, interleaved[i]));
    view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7FFF, true);
  }

  return new Blob([view], { type: 'audio/wav' });
};

const writeString = (view, offset, string) => {
  for (let i = 0; i < string.length; i++) {
    view.setUint8(offset + i, string.charCodeAt(i));
  }
};

export default function App() {
  const [selectedFile, setSelectedFile] = useState(null);
  const [recordedAudio, setRecordedAudio] = useState({ url: null, blob: null });
  const [transcript, setTranscript] = useState('');
  const [synthesizedAudioUrl, setSynthesizedAudioUrl] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [statusMessage, setStatusMessage] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  const mediaRecorderRef = useRef(null);
  const audioChunksRef = useRef([]);
  const fileInputRef = useRef(null);
  const audioContextRef = useRef(window.AudioContext || window.webkitAudioContext ? new (window.AudioContext || window.webkitAudioContext)() : null);

  const resetAudioSources = () => {
    setSelectedFile(null);
    setRecordedAudio({ url: null, blob: null });
    setTranscript('');
    setSynthesizedAudioUrl(null);
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file && file.type === 'audio/wav') {
      resetAudioSources();
      setSelectedFile(file);
      setStatusMessage(`File selected: ${file.name}`);
      setErrorMessage('');
    } else {
      setSelectedFile(null);
      setErrorMessage('Please select a valid .wav file.');
    }
  };

  const handleUploadButtonClick = () => {
    fileInputRef.current.click();
  };

  const startRecording = async () => {
    if (!audioContextRef.current) {
      setErrorMessage("Web Audio API is not supported by this browser.");
      return;
    }
    resetAudioSources();
    setErrorMessage('');
    setStatusMessage('Requesting microphone access...');

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      setStatusMessage('Recording...');
      setIsRecording(true);

      mediaRecorderRef.current = new MediaRecorder(stream);
      audioChunksRef.current = [];

      mediaRecorderRef.current.ondataavailable = (event) => {
        audioChunksRef.current.push(event.data);
      };

      mediaRecorderRef.current.onstop = async () => {
        const rawBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
        const arrayBuffer = await rawBlob.arrayBuffer();

        try {
          const audioBuffer = await audioContextRef.current.decodeAudioData(arrayBuffer);
          const audioBlob = createWavFile(audioBuffer);
          const audioUrl = URL.createObjectURL(audioBlob);
          setRecordedAudio({ url: audioUrl, blob: audioBlob });
          setStatusMessage('Recording finished. Ready to transform.');
        } catch (decodeError) {
          console.error("Error decoding audio data:", decodeError);
          setErrorMessage("Failed to process the recorded audio. Please try again.");
          setStatusMessage('');
        } finally {
          stream.getTracks().forEach(track => track.stop());
        }
      };

      mediaRecorderRef.current.start();
    } catch (err) {
      console.error("Error accessing microphone:", err);
      setErrorMessage('Microphone access was denied. Please enable it in your browser settings.');
      setStatusMessage('');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
    }
  };

  const handleRecordButtonClick = () => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    const audioSource = selectedFile || recordedAudio.blob;
    if (!audioSource) {
      setErrorMessage('Please select a file or record audio to transform.');
      return;
    }

    setIsLoading(true);
    setErrorMessage('');
    setStatusMessage('Uploading and processing...');
    setTranscript('');
    setSynthesizedAudioUrl(null);

    const formData = new FormData();
    formData.append('audioFile', audioSource, selectedFile ? selectedFile.name : 'recording.wav');

    try {
      const response = await fetch('/api/audiotest/transform-accent', { method: 'POST', body: formData });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || `Server responded with status: ${response.status}`);
      }

      const contentType = response.headers.get('content-type');

      if (contentType.includes('application/json')) {
        const data = await response.json();
        setStatusMessage(data.message || 'Processing complete!');
        setTranscript(data.transcript || 'No transcript available.');
        if (data.audioBase64) {
          const byteArray = Uint8Array.from(atob(data.audioBase64), c => c.charCodeAt(0));
          const blob = new Blob([byteArray], { type: 'audio/wav' });
          setSynthesizedAudioUrl(URL.createObjectURL(blob));
        }
      } else if (contentType.includes('audio')) {
        const blob = await response.blob();
        setSynthesizedAudioUrl(URL.createObjectURL(blob));
        setStatusMessage('Processing complete!');
      } else {
        throw new Error(`Unexpected response type from server: ${contentType}`);
      }

    } catch (error) {
      console.error('Submission error:', error);
      setErrorMessage(error.message);
      setStatusMessage('');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="app">
      <div className="container">
        <div className="header">
          <h1 className="title">Accent Transformer</h1>
          <p className="subtitle">Upload a .WAV file or record your voice to convert it to a standard American accent.</p>
        </div>

        <form onSubmit={handleSubmit}>
          <input type="file" ref={fileInputRef} onChange={handleFileChange} accept=".wav" className="hidden" />

          <div className="upload-box">
            <div className="button-group">
              <button type="button" onClick={handleUploadButtonClick} disabled={isRecording} className="button button-upload">Choose a .WAV File</button>
              <span className="or">OR</span>
              <button type="button" onClick={handleRecordButtonClick} className={`button ${isRecording ? 'button-recording' : 'button-record'}`}>
                {isRecording && <span className="record-indicator"></span>}
                <span>{isRecording ? 'Stop Recording' : 'Record Audio'}</span>
              </button>
            </div>
            <p className="message">{statusMessage || 'No audio source selected'}</p>
          </div>

          {recordedAudio.url && (
            <div>
              <h3 className="section-title">Your Recording:</h3>
              <audio controls src={recordedAudio.url} className="audio-player" />
            </div>
          )}

          <button type="submit" disabled={isLoading || (!selectedFile && !recordedAudio.blob)} className="button button-submit">
            {isLoading ? <Spinner /> : 'Transform Accent'}
          </button>
        </form>

        {errorMessage && (
          <div className="error-box">
            <pre>{errorMessage}</pre>
          </div>
        )}

        {(transcript || synthesizedAudioUrl) && (
          <div className="results">
            {transcript && (
              <div>
                <h3 className="section-title">Transcribed Text:</h3>
                <p className="transcript-box">"{transcript}"</p>
              </div>
            )}
            {synthesizedAudioUrl && (
              <div>
                <h3 className="section-title">Synthesized Audio:</h3>
                <audio controls src={synthesizedAudioUrl} className="audio-player" />
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
