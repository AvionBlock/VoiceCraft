export function createAudioContext(contextOptions = null) {
    return new AudioContext(contextOptions);
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