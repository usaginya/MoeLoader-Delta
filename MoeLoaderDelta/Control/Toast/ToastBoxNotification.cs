using MoeLoaderDelta.Control.Toast.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using ToastNotifications.Core;

namespace MoeLoaderDelta.Control.Toast
{
    public class ToastBoxNotification : MessageBase<ToastBox>, INotifyPropertyChanged
    {
        protected override ToastBox CreateDisplayPart => new ToastBox(this);
        protected override void UpdateDisplayOptions(ToastBox displayPart, MessageOptions options) { }

        public ToastBoxNotification(string message, MsgType msgType, MessageOptions messageOptions) : base(message, messageOptions)
        {
            SetStyle(msgType);
        }

        #region binding properties
        private Canvas _icon;
        public Canvas Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush _background;
        public SolidColorBrush Background
        {
            get => _background;
            set
            {
                _background = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) { handler.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
        }
        #endregion

        /// <summary>
        /// 消息类型
        /// </summary>
        public enum MsgType { Info, Success, Warning, Error }


        /// <summary>
        /// 设置样式
        /// </summary>
        private void SetStyle(MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.Success:
                    Icon = (Canvas)DisplayPart.Resources["SuccessIcon"];
                    Background = (SolidColorBrush)DisplayPart.Resources["ToastSuccess"];
                    break;
                case MsgType.Error:
                    Icon = (Canvas)DisplayPart.Resources["ErrorIcon"];
                    Background = (SolidColorBrush)DisplayPart.Resources["ToastError"];
                    break;
                case MsgType.Warning:
                    Icon = (Canvas)DisplayPart.Resources["WarningIcon"];
                    Background = (SolidColorBrush)DisplayPart.Resources["ToastWarning"];
                    break;
                default:
                    Icon = (Canvas)DisplayPart.Resources["InformationIcon"];
                    Background = (SolidColorBrush)DisplayPart.Resources["ToastInfo"];
                    break;
            }
        }

    }
}
