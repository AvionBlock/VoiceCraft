navigator.getUserMedia = navigator.getUserMedia || 
                 navigator.webkitGetUserMedia || 
                 navigator.mozGetUserMedia || 
                 navigator.msGetUserMedia;

let device_list = [];

function refreshDeviceList() {
    device_list = [];
    navigator.mediaDevices.enumerateDevices()
        .then((devices) => {
          devices.forEach((device) => {
              if (device.kind == "audioinput") {
                  if (device.label !== "Default") {
                      device_list.push(`${device.label}`);
                      device_list.push(`${device.deviceId}`);
                  }
              }
          });
        })
}

refreshDeviceList();

export function constructStream() {
    return new MediaStream();
}

export function applyTo(mediaStream, deviceId, numOfChannels, sampleRate, bufferSize) {
    navigator.mediaDevices.getUserMedia({audio: { deviceId: deviceId }})
        .then(function(result) {
            const tracks = result.getTracks();
            for (let i = 0; i < tracks.length; i++) {
                const constraints = {
                    channelCount: { exact: numOfChannels },
                    sampleRate: { exact: sampleRate },
                    sampleSize: { ideal: bufferSize },
                };
                tracks[i].applyConstraints(constraints).then(() => {
                    console.log("we are adding track");
                    mediaStream.addTrack(tracks[i]);
                }).catch((e) => {
                    console.log("we are closing track");
                    tracks[i].close();
                });
            }
        });
}

export function constructRec(mediaStream) {
    return new MediaRecorder(mediaStream, {
        mimeType: 'audio/webm;codecs=pcm'
    });
}

export function startRec(mediaRecorder) {
    mediaRecorder.start();
}

export function deconstruct(audioRecord, audioStream) {
    audioRecord.stop();
    const tracks = audioStream.getTracks();
    for (let i = 0; i < tracks.length; i++) {
        console.log("we are closing track");
        tracks[i].stop();
    }
}

export function getDevices() {
    let return_list = device_list;
    refreshDeviceList();

    return return_list;
}
