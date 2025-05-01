const textEncoder = new TextEncoder();

export function exists(directory) {
    return window.localStorage.getItem(directory) !== undefined;
}

export function load(directory) {
    const data = window.localStorage.getItem(directory);
    if (data === undefined) throw "No data available";
    return textEncoder.encode(data);
}