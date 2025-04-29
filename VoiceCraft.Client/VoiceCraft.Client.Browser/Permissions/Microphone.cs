using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace VoiceCraft.Client.Browser.Permissions
{
    public class Microphone : Microsoft.Maui.ApplicationModel.Permissions.Microphone
    {
        public override void EnsureDeclared()
        {} //Legit do nothing

        public override Task<PermissionStatus> CheckStatusAsync()
        {
            // /* EnsureDeclared();
            // return Task.FromResult(PermissionStatus.Granted);
            // return Task.FromResult(async () =>
            //     {
                    var check = EmbedInterop.check();
                    if (check) {
                        return Task.FromResult(PermissionStatus.Granted);
                    } else {
                        return Task.FromResult(PermissionStatus.Denied);
                    }
                // });
        }
        
        public override Task<PermissionStatus> RequestAsync() {
            // return CheckStatusAsync();
            // return new Task<PermissionStatus>(async () =>
            //     {
                    var check = EmbedInterop.ask();
                    if (check) {
                        return Task.FromResult(PermissionStatus.Granted);
                    } else {
                        return Task.FromResult(PermissionStatus.Denied);
                    }
                // });
        }

        public override bool ShouldShowRationale() => false;
    }

    internal static partial class EmbedInterop
    {
        [JSImport("checkPermission", "permissions.js")]
        public static partial bool check();

        [JSImport("askPermission", "permissions.js")]
        public static partial bool ask();
    }
}
