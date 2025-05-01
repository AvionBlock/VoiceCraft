let showRationale = true;

export async function checkStatusAsync() {
    const status = await navigator.permissions.query({name: "microphone"});
    return status.state === "granted";
}

export async function requestAsync() {
    try {
        await navigator.mediaDevices.getUserMedia({audio: true, video: false});
        return true;
    }
    catch (error) {
        return false;
    }
}

export function shouldShowRationale() {
    if(showRationale) {
        showRationale = false;
        return true;
    }
    return false;
}