let showRationale = true;

export async function checkStatusAsync() {
    const status = await navigator.permissions.query({name: "microphone"});
    return status.state === "granted";
}

export async function requestAsync() {
    try {
        if(await checkStatusAsync()) return true; //If granted, return true.
        await navigator.mediaDevices.getUserMedia({audio: true, video: false}); //Request it.
        return true;
    }
    catch (error) {
        return false; //We know it's been blocked so false.
    }
}

export function shouldShowRationale() {
    if(showRationale) {
        showRationale = false;
        return true;
    }
    return false;
}