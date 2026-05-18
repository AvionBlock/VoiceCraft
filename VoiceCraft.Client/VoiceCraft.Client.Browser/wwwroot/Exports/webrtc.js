let peerConnection = null;
let signalingSocket = null;
let dataChannel = null;
let incomingPackets = [];
let pendingCandidates = [];
let closeReason = null;

function isDebugEnabled() {
    try {
        return globalThis.localStorage?.getItem("voicecraft.webrtc.debug") === "true";
    } catch {
        return false;
    }
}

function debugLog(...args) {
    if (isDebugEnabled())
        console.info(...args);
}

function normalizeCandidate(candidate) {
    if (!candidate)
        return null;

    return candidate.startsWith("candidate:") ? candidate : `candidate:${candidate}`;
}

function createTimeout(ms, reason) {
    let timeoutId = null;
    const promise = new Promise((_, reject) => {
        timeoutId = setTimeout(() => {
            closeReason ??= reason;
            reject(new Error(reason));
        }, ms);
    });

    return {
        promise,
        clear: () => clearTimeout(timeoutId)
    };
}

async function addRemoteCandidateAsync(candidate) {
    try {
        await peerConnection.addIceCandidate(candidate);
    } catch (error) {
        console.warn("[VoiceCraft WebRTC] ICE candidate rejected", error);
    }
}

function parseIceServers(iceServersJson) {
    if (!iceServersJson)
        return [];

    try {
        const iceServers = JSON.parse(iceServersJson);
        if (!Array.isArray(iceServers))
            return [];

        return iceServers
            .map(server => ({
                urls: server.urls ?? server.Urls,
                username: server.username ?? server.Username,
                credential: server.credential ?? server.Credential
            }))
            .filter(server => server.urls);
    } catch (error) {
        console.warn("[VoiceCraft WebRTC] failed to parse ICE server config", error);
        return [];
    }
}

export async function connectAsync(signalingUrl, iceServersJson) {
    close();
    closeReason = null;
    pendingCandidates = [];

    if (location.protocol === "https:" && signalingUrl.startsWith("ws://") && !signalingUrl.includes("127.0.0.1") && !signalingUrl.includes("localhost"))
        signalingUrl = "wss://" + signalingUrl.substring("ws://".length);

    debugLog("[VoiceCraft WebRTC] connecting", signalingUrl);
    peerConnection = new RTCPeerConnection({
        iceServers: parseIceServers(iceServersJson)
    });
    dataChannel = peerConnection.createDataChannel("voicecraft");
    dataChannel.binaryType = "arraybuffer";

    peerConnection.oniceconnectionstatechange = () => {
        debugLog("[VoiceCraft WebRTC] ice connection state", peerConnection.iceConnectionState);
    };
    peerConnection.onconnectionstatechange = () => {
        debugLog("[VoiceCraft WebRTC] connection state", peerConnection.connectionState);
    };

    signalingSocket = new WebSocket(signalingUrl);

    const signalingOpen = new Promise((resolve, reject) => {
        signalingSocket.onopen = () => {
            debugLog("[VoiceCraft WebRTC] signaling open");
            resolve();
        };
        signalingSocket.onerror = event => {
            console.error("[VoiceCraft WebRTC] signaling error", event);
            reject(event);
        };
        signalingSocket.onclose = event => {
            debugLog("[VoiceCraft WebRTC] signaling closed", event.code, event.reason);
            const reason = event.reason || "VoiceCraft.DisconnectReason.ConnectionClosed";
            closeReason ??= reason;
            reject(new Error(reason));
        };
    });

    const dataChannelOpen = new Promise((resolve, reject) => {
        dataChannel.onopen = () => {
            debugLog("[VoiceCraft WebRTC] data channel open");
            resolve();
        };
        dataChannel.onerror = event => {
            console.error("[VoiceCraft WebRTC] data channel error", event);
            reject(event);
        };
        dataChannel.onclose = () => {
            debugLog("[VoiceCraft WebRTC] data channel closed");
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
                debugLog("[VoiceCraft WebRTC] answer received");
                await peerConnection.setRemoteDescription({
                    type: "answer",
                    sdp: message.sdp
                });
                for (const candidate of pendingCandidates)
                    await addRemoteCandidateAsync(candidate);
                pendingCandidates = [];
                break;
            case 2:
            case "Candidate":
            case "candidate":
                if (message.candidate) {
                    debugLog("[VoiceCraft WebRTC] candidate received");
                    const candidate = {
                        candidate: normalizeCandidate(message.candidate),
                        sdpMid: message.sdpMid ?? "0",
                        sdpMLineIndex: message.sdpMLineIndex ?? 0
                    };
                    if (peerConnection.remoteDescription)
                        await addRemoteCandidateAsync(candidate);
                    else
                        pendingCandidates.push(candidate);
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

    const timeout = createTimeout(10000, "VoiceCraft.DisconnectReason.ConnectionTimeout");
    try {
        await Promise.race([dataChannelOpen, timeout.promise]);
    } finally {
        timeout.clear();
    }
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
    pendingCandidates = [];
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
