export function constructAudioContext(contextOptions = null) {
    console.log("Creating device");
    return new AudioContext(contextOptions)
}

export function getBuffers(audioContext, channels, length, sampleRate) {
    console.log("creating buffer");
    const buffer = audioContext.createBuffer(channels, length, sampleRate);
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    source.connect(audioContext.destination);
    source.start();
    return buffer;
}

export function decodeAudioData(audioBuffer, audioData) {
    for (let channel = 0; channel < audioBuffer.numberOfChannels; channel++) {
        // This gives us the actual ArrayBuffer that contains the data
        const nowBuffering = audioBuffer.getChannelData(channel);
        for (let i = 0; i < audioBuffer.length; i++) {
            // Math.random() is in [0; 1.0]
            // audio needs to be in [-1.0; 1.0]
            nowBuffering[i] = Math.random() * 2 - 1;
        }
    }
}

export async function stopAudioContext(audioContext) {
    audioContext.close();
}

export function isStoppedAudioContext(audioContext) {
    return audioContext.state === "stopped";
}

export async function resumeAudioContext(audioContext) {
    audioContext.resume();
}

export async function suspendAudioContext(audioContext) {
    audioContext.suspend();
}
