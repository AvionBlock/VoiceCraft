export async function setItemAsync(directory, data) {
    return await new Promise((resolve) => {
        globalThis.localStorage.setItem(directory, data);
        resolve();
    });
}

export async function getItemAsync(directory, data) {
    return await new Promise((resolve) => {
        resolve(globalThis.localStorage.getItem(directory));
    });
}
