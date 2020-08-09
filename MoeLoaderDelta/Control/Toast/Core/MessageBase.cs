using System.Windows;
using System.Windows.Input;
using ToastNotifications.Core;

namespace MoeLoaderDelta.Control.Toast.Core
{
    public abstract class MessageBase<TDisplayPart> : NotificationBase where TDisplayPart : NotificationDisplayPart
    {
        private NotificationDisplayPart _displayPart;

        protected MessageBase(string message, MessageOptions options) : base(message, options) { }

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = Configure());

        private TDisplayPart Configure()
        {
            TDisplayPart displayPart = CreateDisplayPart;

            displayPart.Unloaded += OnUnloaded;
            displayPart.MouseLeftButtonDown += OnLeftMouseDown;

            UpdateDisplayOptions(displayPart, Options);
            return displayPart;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _displayPart.MouseLeftButtonDown -= OnLeftMouseDown;
            _displayPart.Unloaded -= OnUnloaded;
        }

        private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            Options.NotificationClickAction?.Invoke(this);
        }

        protected abstract void UpdateDisplayOptions(TDisplayPart displayPart, MessageOptions options);

        protected abstract TDisplayPart CreateDisplayPart { get; }
    }
    }
