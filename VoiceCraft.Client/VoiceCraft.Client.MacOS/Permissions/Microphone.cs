using System.Threading.Tasks;
using AVFoundation;
using Microsoft.Maui.ApplicationModel;

namespace VoiceCraft.Client.MacOS.Permissions;

public class Microphone : Microsoft.Maui.ApplicationModel.Permissions.Microphone
{
    public override void EnsureDeclared()
    {
    }

    public override Task<PermissionStatus> CheckStatusAsync()
    {
        var status = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio);
        return Task.FromResult(status switch
        {
            AVAuthorizationStatus.Authorized => PermissionStatus.Granted,
            AVAuthorizationStatus.Denied => PermissionStatus.Denied,
            AVAuthorizationStatus.Restricted => PermissionStatus.Restricted,
            _ => PermissionStatus.Unknown
        });
    }

    public override async Task<PermissionStatus> RequestAsync()
    {
        var status = await CheckStatusAsync();
        if (status != PermissionStatus.Unknown)
            return status;

        var tcs = new TaskCompletionSource<bool>();
        AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Audio, granted => tcs.TrySetResult(granted));
        var granted = await tcs.Task;
        return granted ? PermissionStatus.Granted : PermissionStatus.Denied;
    }

    public override bool ShouldShowRationale()
    {
        return false;
    }
}
