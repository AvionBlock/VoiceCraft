using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace VoiceCraft.Client.PDK.Services
{
    public class PermissionsService
    {
        public async Task<PermissionStatus> CheckAndRequestPermission<TPermission>(string? rationalDescription = null) where TPermission : Permissions.BasePermission, new()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<TPermission>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<TPermission>() && !string.IsNullOrWhiteSpace(rationalDescription))
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<TPermission>();

            return status;
        }
    }
}
