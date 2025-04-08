// let original_stream = await navigator.getUserMedia({audio: true}, function() {}, function() {});

let the_status = await navigator.permissions.query({ name: "microphone" });

export function checkPermission() {
    console.log("we check   ");
    navigator.permissions.query(
        { name: "microphone" },
        function(new_status) {
            the_status = new_status;
        },
        function() {});
    return the_status.state === "granted";
}

export function askPermission() {
    // return await navigator.permissions.query({ name: "microphone" }) === "granted";
    try {
        let stream = original_stream;
        console.log("We asked user " + stream);
        return true;
    } catch {
        console.log("we failed  ");
        return false;
    }
}
