using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace VoiceCraft.Client.Browser.Permissions;

public class Microphone : Microsoft.Maui.ApplicationModel.Permissions.Microphone
{
    public override void EnsureDeclared()
    {
    } //Legit do nothing

    public override async Task<PermissionStatus> CheckStatusAsync()
    {
        return await JsNativeMicrophonePermission.CheckStatusAsync() ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public override async Task<PermissionStatus> RequestAsync()
    {
        return await JsNativeMicrophonePermission.RequestAsync() ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public override bool ShouldShowRationale()
    {
        return JsNativeMicrophonePermission.ShouldShowRationale();
    }
}

internal static partial class JsNativeMicrophonePermission
{
    [JSImport("checkStatusAsync", "microphonePermission.js")]
    public static partial Task<bool> CheckStatusAsync();

    [JSImport("requestAsync", "microphonePermission.js")]
    public static partial Task<bool> RequestAsync();

    [JSImport("shouldShowRationale", "microphonePermission.js")]
    public static partial bool ShouldShowRationale();
}