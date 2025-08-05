import React, { useEffect } from 'react';
import { Janus } from 'janus-gateway';

const JanusAudioBridge = () => {
  useEffect(() => {
    Janus.init({
      debug: 'all',
      callback: () => {
        const janus = new Janus({
          server: 'ws://localhost:8188', // Change to your backend IP if deployed

          success: () => {
            console.log('✅ Janus connected');

            janus.attach({
              plugin: 'janus.plugin.audiobridge',
              success: (pluginHandle) => {
                console.log('✅ Attached to AudioBridge plugin', pluginHandle);
                // You can now join a room, publish audio, etc.
              },
              error: (err) => {
                console.error('❌ Plugin attach error', err);
              },
              onmessage: (msg, jsep) => {
                console.log('📨 Message from AudioBridge:', msg);
                if (jsep) {
                  pluginHandle.handleRemoteJsep({ jsep });
                }
              },
              onlocalstream: (stream) => {
                console.log('🎙️ Local stream', stream);
              },
              onremotestream: (stream) => {
                console.log('🔈 Remote stream', stream);
              },
            });
          },

          error: (err) => {
            console.error('❌ Janus error', err);
          },

          destroyed: () => {
            console.warn('🚫 Janus session destroyed');
          }
        });
      }
    });
  }, []);

  return <div>🔗 Connecting to Janus AudioBridge...</div>;
};

export default JanusAudioBridge;
