using System.Windows;
using ToastNotifications;
using ToastNotifications.Core;

namespace MoeLoaderDelta.Control.Toast
{
    public static class ToastBoxNotificationExtensions
    {
        public static async void ShowToastAsync(this Notifier notifier, string message, ToastBoxNotification.MsgType msgType, MessageOptions messageOptions = null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => notifier.Notify(() => new ToastBoxNotification(message, msgType, messageOptions)));
        }
    }
}
