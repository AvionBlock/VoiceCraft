let peerConnection = null;
let signalingSocket = null;
let dataChannel = null;
let incomingPackets = [];
let closeReason = null;

export async function connectAsync(signalingUrl) {
    close();
    closeReason = null;

    if (location.protocol === "https:" && signalingUrl.startsWith("ws://") && !signalingUrl.includes("127.0.0.1") && !signalingUrl.includes("localhost"))
        signalingUrl = "wss://" + signalingUrl.substring("ws://".length);

    console.info("[VoiceCraft WebRTC] connecting", signalingUrl);
    peerConnection = new RTCPeerConnection({
        iceServers: [
            // TODO add config
            { urls: "stun:stun.l.google.com:19302" }
        ]
    });
    dataChannel = peerConnection.createDataChannel("voicecraft");
    dataChannel.binaryType = "arraybuffer";

    signalingSocket = new WebSocket(signalingUrl);

    const signalingOpen = new Promise((resolve, reject) => {
        signalingSocket.onopen = () => {
            console.info("[VoiceCraft WebRTC] signaling open");
            resolve();
        };
        signalingSocket.onerror = event => {
            console.error("[VoiceCraft WebRTC] signaling error", event);
            reject(event);
        };
        signalingSocket.onclose = event => {
            console.info("[VoiceCraft WebRTC] signaling closed", event.code, event.reason);
            const reason = event.reason || "VoiceCraft.DisconnectReason.ConnectionClosed";
            closeReason ??= reason;
            reject(new Error(reason));
        };
    });

    const dataChannelOpen = new Promise((resolve, reject) => {
        dataChannel.onopen = () => {
            console.info("[VoiceCraft WebRTC] data channel open");
            resolve();
        };
        dataChannel.onerror = event => {
            console.error("[VoiceCraft WebRTC] data channel error", event);
            reject(event);
        };
        dataChannel.onclose = () => {
            console.info("[VoiceCraft WebRTC] data channel closed");
            const reason = "VoiceCraft.DisconnectReason.ConnectionClosed";
            closeReason ??= reason;
            reject(new Error(reason));
        };
    });

    dataChannel.onmessage = event => {
        incomingPackets.push(new Uint8Array(event.data));
    };

    signalingSocket.onmessage = async event => {
        const message = JSON.parse(event.data);
        switch (message.type) {
            case 1:
            case "Answer":
            case "answer":
                console.info("[VoiceCraft WebRTC] answer received");
                await peerConnection.setRemoteDescription({
                    type: "answer",
                    sdp: message.sdp
                });
                break;
            case 2:
            case "Candidate":
            case "candidate":
                if (message.candidate) {
                    console.info("[VoiceCraft WebRTC] candidate received");
                    await peerConnection.addIceCandidate({
                        candidate: message.candidate,
                        sdpMid: message.sdpMid ?? null,
                        sdpMLineIndex: message.sdpMLineIndex ?? 0
                    });
                }
                break;
        }
    };

    peerConnection.onicecandidate = event => {
        if (!event.candidate || signalingSocket.readyState !== WebSocket.OPEN) return;
        signalingSocket.send(JSON.stringify({
            type: 2,
            candidate: event.candidate.candidate,
            sdpMid: event.candidate.sdpMid,
            sdpMLineIndex: event.candidate.sdpMLineIndex
        }));
    };

    await signalingOpen;
    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    signalingSocket.send(JSON.stringify({
        type: 0,
        sdp: offer.sdp
    }));
    await dataChannelOpen;
}

export function send(data) {
    if (!dataChannel || dataChannel.readyState !== "open") return;
    dataChannel.send(data);
}

export function receive() {
    return incomingPackets.shift() ?? null;
}

export function consumeCloseReason() {
    const reason = closeReason;
    closeReason = null;
    return reason ?? "";
}

export function close() {
    incomingPackets = [];
    closeReason = null;

    if (dataChannel) {
        dataChannel.onopen = null;
        dataChannel.onerror = null;
        dataChannel.onmessage = null;
        dataChannel.onclose = null;
        dataChannel.close();
        dataChannel = null;
    }

    if (peerConnection) {
        peerConnection.onicecandidate = null;
        peerConnection.close();
        peerConnection = null;
    }

    if (signalingSocket) {
        signalingSocket.onopen = null;
        signalingSocket.onerror = null;
        signalingSocket.onmessage = null;
        signalingSocket.onclose = null;
        signalingSocket.close();
        signalingSocket = null;
    }
}
