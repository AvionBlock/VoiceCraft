export function createAudioContext(contextOptions = null) {
    return new AudioContext(contextOptions);
}

let captureContext = null;
let captureStream = null;
let captureProcessor = null;
let captureQueue = [];

let playbackContext = null;
let playbackProcessor = null;
let playbackQueue = [];
let toneOscillator = null;
let toneGain = null;

export function getInputDevices() {
    return "[]";
}

export function getOutputDevices() {
    return "[]";
}

export function startCapture(sampleRate, channels, frameSize, deviceId) {
    stopCapture();
    captureQueue = [];

    const constraints = {
        audio: {
            channelCount: channels,
            sampleRate,
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true
        }
    };

    if (deviceId)
        constraints.audio.deviceId = { exact: deviceId };

    navigator.mediaDevices.getUserMedia(constraints).then(stream => {
        captureStream = stream;
        captureContext = new AudioContext({ sampleRate });
        const source = captureContext.createMediaStreamSource(stream);
        captureProcessor = captureContext.createScriptProcessor(frameSize, channels, channels);
        captureProcessor.onaudioprocess = event => {
            const frame = new Float32Array(frameSize * channels);
            for (let channel = 0; channel < channels; channel++) {
                const input = event.inputBuffer.getChannelData(channel);
                for (let i = 0; i < frameSize; i++)
                    frame[i * channels + channel] = input[i] || 0;
            }
            captureQueue.push(frame);
            if (captureQueue.length > 32)
                captureQueue.shift();
        };
        source.connect(captureProcessor);
        captureProcessor.connect(captureContext.destination);
    }).catch(error => {
        console.error("VoiceCraft capture start failed", error);
    });
}

export function pollCapture() {
    const frame = captureQueue.shift();
    if (!frame)
        return "";

    return JSON.stringify(Array.from(frame));
}

export function stopCapture() {
    if (captureProcessor) {
        captureProcessor.disconnect();
        captureProcessor.onaudioprocess = null;
        captureProcessor = null;
    }
    if (captureStream) {
        for (const track of captureStream.getTracks())
            track.stop();
        captureStream = null;
    }
    if (captureContext) {
        captureContext.close();
        captureContext = null;
    }
    captureQueue = [];
}

export function startPlayback(sampleRate, channels, frameSize, deviceId) {
    stopPlayback();
    playbackQueue = [];
    playbackContext = new AudioContext({ sampleRate });
    playbackProcessor = playbackContext.createScriptProcessor(frameSize, 0, channels);
    playbackProcessor.onaudioprocess = event => {
        const outputLength = event.outputBuffer.length;
        const frame = playbackQueue.shift();
        for (let channel = 0; channel < channels; channel++) {
            const output = event.outputBuffer.getChannelData(channel);
            for (let i = 0; i < outputLength; i++) {
                const sourceIndex = i * channels + channel;
                output[i] = frame && sourceIndex < frame.length ? frame[sourceIndex] : 0;
            }
        }
    };
    playbackProcessor.connect(playbackContext.destination);

    if (deviceId && typeof playbackContext.destination.setSinkId === "function")
        playbackContext.destination.setSinkId(deviceId).catch(console.warn);
}

export function enqueuePlayback(samplesJson) {
    if (!playbackContext || !playbackProcessor)
        return;

    const samples = JSON.parse(samplesJson);
    playbackQueue.push(new Float32Array(samples));
    if (playbackQueue.length > 32)
        playbackQueue.shift();
}

export function stopPlayback() {
    if (toneOscillator) {
        toneOscillator.stop();
        toneOscillator.disconnect();
        toneOscillator = null;
    }
    if (toneGain) {
        toneGain.disconnect();
        toneGain = null;
    }
    if (playbackProcessor) {
        playbackProcessor.disconnect();
        playbackProcessor.onaudioprocess = null;
        playbackProcessor = null;
    }
    if (playbackContext) {
        playbackContext.close();
        playbackContext = null;
    }
    playbackQueue = [];
}

export function playTone(durationMs, frequency) {
    if (!playbackContext)
        return;

    const now = playbackContext.currentTime;
    const gain = playbackContext.createGain();
    const oscillator = playbackContext.createOscillator();
    oscillator.frequency.value = frequency;
    gain.gain.setValueAtTime(0.08, now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + durationMs / 1000);
    oscillator.connect(gain);
    gain.connect(playbackContext.destination);
    oscillator.start(now);
    oscillator.stop(now + durationMs / 1000);
    toneOscillator = oscillator;
    toneGain = gain;
}

export async function getInputDevicesAsync() {
    const deviceList = [];
    const devices = await navigator.mediaDevices.enumerateDevices();
    for (const device of devices) {
        //IsInput, Is Not Default, Is Not label Empty.
        if (device.kind === 'audioinput' && device.deviceId !== 'default' && device.label !== '')
            deviceList.push(device);
    }

    return JSON.stringify(deviceList);
}

export async function getOutputDevicesAsync() {
    const deviceList = [];
    const devices = await navigator.mediaDevices.enumerateDevices();
    for (const device of devices) {
        if (device.kind === 'audiooutput' && device.deviceId !== 'default')
            deviceList.push(device);
    }

    return JSON.stringify(deviceList);
}
