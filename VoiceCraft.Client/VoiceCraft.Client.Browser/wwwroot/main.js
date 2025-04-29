import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

// if (typeof dotnetRuntime.Module.AL !== "object") {
//     exit(-10, "Can't find AL");
// }
//
// console.log(dotnetRuntime.Module.AL)
//
// if (typeof dotnetRuntime.Module.GL !== "object") {
//     exit(-10, "Can't find GL");
// }

// const assemblyName = "wrapper.wasm";
// const response = await fetch("./" + assemblyName);
// const assemblyBytes = await response.arrayBuffer();
// const isPdb = dotnetRuntime.Module._mono_wasm_add_assembly(assemblyName, assemblyBytes, assemblyBytes.byteLength);

dotnetRuntime.setModuleImports('audio_player.js', await import('./audio_player.js'));
dotnetRuntime.setModuleImports('audio_recorder.js', await import('./audio_recorder.js'));
dotnetRuntime.setModuleImports('permissions.js', await import('./permissions.js'));
dotnetRuntime.setModuleImports('proc.js', await import('./proc.js'));
dotnetRuntime.setModuleImports('storage.js', await import('./storage.js'));

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href]);
