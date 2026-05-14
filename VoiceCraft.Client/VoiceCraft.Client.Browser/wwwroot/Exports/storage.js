const textEncoder = new TextEncoder();
const textDecoder = new TextDecoder();

export function exists(directory) {
    return window.localStorage.getItem(directory) !== null;
}

export function load(directory) {
    const data = window.localStorage.getItem(directory);
    if (data === null) throw "No data available";
    return textEncoder.encode(data);
}

export function save(directory, data) {
    window.localStorage.setItem(directory, textDecoder.decode(data));
}
