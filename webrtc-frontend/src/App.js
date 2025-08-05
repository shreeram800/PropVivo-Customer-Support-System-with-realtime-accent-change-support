import React, { useEffect, useState, useRef } from 'react';
import Janus from 'janus-gateway';
import adapter from 'webrtc-adapter'; // Ensures adapter is globally patched
import './index.css';

window.adapter = adapter;


export default function App() {
  const [janus, setJanus] = useState(null);
  const [pluginHandle, setPluginHandle] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const localAudioRef = useRef(null);
  const remoteAudioRef = useRef(null);

  useEffect(() => {
    Janus.init({
      debug: 'all',
      callback: () => {
        const janusInstance = new Janus({
          server: 'ws://localhost:8188', // Janus WebSocket server
          success: () => {
            janusInstance.attach({
              plugin: 'janus.plugin.audiobridge',
              success: (handle) => {
                console.log('Plugin attached:', handle);
                setPluginHandle(handle);
                setIsConnected(true);

                const register = {
                  request: 'join',
                  room: 1234,
                  display: 'ReactUser',
                };
                handle.send({ message: register });
              },
              error: (err) => {
                console.error('Plugin attach error:', err);
              },
              consentDialog: (on) => {
                console.log('Consent dialog:', on ? 'shown' : 'hidden');
              },
              webrtcState: (on) => {
                console.log('WebRTC PeerConnection is', on ? 'up' : 'down');
              },
              onmessage: (msg, jsep) => {
                console.log('Received message:', msg);
                if (jsep) {
                  handleRemoteJsep(jsep);
                }
              },
              onlocalstream: (stream) => {
                if (localAudioRef.current) {
                  localAudioRef.current.srcObject = stream;
                }
              },
              onremotestream: (stream) => {
                if (remoteAudioRef.current) {
                  remoteAudioRef.current.srcObject = stream;
                }
              },
              oncleanup: () => {
                console.log('Cleanup notification');
              },
            });
          },
          error: (err) => {
            console.error('Janus error:', err);
          },
          destroyed: () => {
            console.log('Janus instance destroyed');
          },
        });

        setJanus(janusInstance);
      },
    });
  }, []);

  const handleRemoteJsep = (jsep) => {
    if (!pluginHandle) return;

    pluginHandle.createAnswer({
      jsep,
      media: { audio: true, video: false },
      success: (jsepAnswer) => {
        const body = { request: 'start', room: 1234 };
        pluginHandle.send({ message: body, jsep: jsepAnswer });
      },
      error: (err) => {
        console.error('WebRTC createAnswer error:', err);
      },
    });
  };

  const startPublishing = () => {
    if (!pluginHandle) return;
    setIsRecording(true);

    pluginHandle.createOffer({
      media: { audio: true, video: false },
      success: (jsep) => {
        const publish = {
          request: 'configure',
          muted: false,
        };
        pluginHandle.send({ message: publish, jsep });
      },
      error: (err) => {
        console.error('WebRTC createOffer error:', err);
      },
    });
  };

  const stopPublishing = () => {
    if (!pluginHandle) return;
    setIsRecording(false);

    pluginHandle.send({
      message: {
        request: 'configure',
        muted: true,
      },
    });
  };

  return (
    <div className="app">
      <div className="container">
        <h1 className="title">Janus AudioBridge Realtime</h1>
        <p className="subtitle">Connects two users in realtime audio using Janus AudioBridge</p>

        {isConnected ? (
          <div className="button-group">
            <button className="button" onClick={isRecording ? stopPublishing : startPublishing}>
              {isRecording ? 'Stop Audio' : 'Start Audio'}
            </button>
          </div>
        ) : (
          <p className="message">Connecting to Janus...</p>
        )}

        <div className="audio-section">
          <div>
            <h3>Local Audio</h3>
            <audio ref={localAudioRef} autoPlay controls muted className="audio-player" />
          </div>
          <div>
            <h3>Remote Audio</h3>
            <audio ref={remoteAudioRef} autoPlay controls className="audio-player" />
          </div>
        </div>
      </div>
    </div>
  );
}
