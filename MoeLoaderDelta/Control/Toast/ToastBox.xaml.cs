using ToastNotifications.Core;

namespace MoeLoaderDelta.Control.Toast
{
    /// <summary>
    /// ToastBox.xaml 的交互逻辑
    /// </summary>
    public partial class ToastBox : NotificationDisplayPart
    {
        public ToastBox(ToastBoxNotification notification)
        {
            InitializeComponent();
            Bind(notification);
        }
    }
}
