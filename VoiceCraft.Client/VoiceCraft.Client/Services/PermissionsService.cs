using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace VoiceCraft.Client.Services
{
    public class PermissionsService(NotificationService notificationService)
    {
        public async Task<PermissionStatus> CheckAndRequestPermission<TPermission>(string? rationalDescription = null) where TPermission : Permissions.BasePermission, new()
        {
            var status = await Permissions.CheckStatusAsync<TPermission>();

            switch (status)
            {
                case PermissionStatus.Granted:
                    return status;
                case PermissionStatus.Denied when DeviceInfo.Platform == DevicePlatform.iOS:
                    // Prompt the user to turn on in settings
                    // On iOS once a permission has been denied it may not be requested again from the application
                    return status;
                case PermissionStatus.Unknown:
                case PermissionStatus.Disabled:
                case PermissionStatus.Restricted:
                case PermissionStatus.Limited:
                default:
                    break;
            }

            status = await Permissions.RequestAsync<TPermission>();
            
            if (Permissions.ShouldShowRationale<TPermission>() && !string.IsNullOrWhiteSpace(rationalDescription))
            {
                notificationService.SendErrorNotification(rationalDescription);
            }
            return status;
        }
    }
}