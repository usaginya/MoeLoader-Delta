using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MoeLoaderDelta.Control
{
    /// <summary>
    /// Toast.xaml 的交互逻辑
    /// by YIU
    /// Last: 2020-7-29
    /// </summary>
    public partial class Toast : UserControl
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public enum MsgType { Info, Success, Warning, Error }

        /// <summary>
        /// 吐司消息类
        /// </summary>
        private class ToastMsg
        {
            public string Msg { get; set; }
            public int Time { get; set; }
            public MsgType MsgType { get; set; }
        }

        /// <summary>
        /// 显示线程
        /// </summary>
        private Thread hideThread;

        /// <summary>
        /// 消息队列
        /// </summary>
        private Queue msgQueue = new Queue();

        public Toast()
        {
            InitializeComponent();
            SetAlignment();
        }

        /// <summary>
        /// 显示Toast消息
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="time">显示时间.毫秒</param>
        public void Show(string msg, MsgType msgType = MsgType.Info, int time = 1700 )
        {
            if (msgQueue.Count > 9) { return; }
            PrivateShow(msg, time < 100 ? 110 : time, msgType);
        }

        /// <summary>
        /// 内部消息队列
        /// </summary>
        /// <param name="callback">是否为回调</param>
        private void PrivateShow(string msg, int time, MsgType msgType, bool callback = false)
        {
            //如果是回调并且队列有消息时则按队列顺序显示消息、否则加入新消息到队列
            Dispatcher.Invoke(new Action(() => Popup.IsOpen = false));
            if (callback && msgQueue.Count > 0)
            {
                ToastMsg toastMsg = (ToastMsg)msgQueue.Dequeue();

                Dispatcher.Invoke(new Action(() =>
                {
                    toastText.Text = toastMsg.Msg;
                    SetBackground(msgType);
                    Popup.IsOpen = true;
                    SetCenter();
                }));

                hideThread = new Thread(() =>
                {
                    Thread.Sleep(toastMsg.Time);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Popup.IsOpen = false;
                        new Thread(() =>
                        {
                            Thread.Sleep(300);
                            PrivateShow(string.Empty, 0, msgType, true);
                        }).Start();
                    }));
                });
                hideThread.Start();
            }
            else if (!callback || !string.IsNullOrWhiteSpace(msg))
            {
                ToastMsg toastMsg = new ToastMsg
                {
                    Msg = msg,
                    Time = time,
                    MsgType = msgType
                };
                msgQueue.Enqueue(toastMsg);
                //首次加入消息要延迟一下等待新消息加入、避免太快的自动消息被覆盖
                if (msgQueue.Count == 1)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        new Thread(() =>
                        {
                            Thread.Sleep(300);
                            if (hideThread != null) { try { hideThread.Abort(); } catch { } }
                            PrivateShow(string.Empty, 0, msgType, true);
                        }).Start();
                    }));
                }
            }
        }

        /// <summary>
        /// 点击toast提示立刻消失
        /// </summary>
        private void ToastMouseHide(object sender, MouseButtonEventArgs e)
        {
            // 清空消息队列
            msgQueue.Clear();
            if (hideThread != null) { try { hideThread.Abort(); } catch { } }
            Popup.IsOpen = false;
        }

        /// <summary>
        /// 设置居中位置
        /// </summary>
        private void SetCenter()
        {
            Popup.HorizontalOffset = -(ToastBorder.ActualWidth / 2);
        }

        /// <summary>
        /// 设置对齐
        /// </summary>
        private static void SetAlignment()
        {
            var ifLeft = SystemParameters.MenuDropAlignment;
            if (ifLeft)
            {
                var t = typeof(SystemParameters);
                var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
                field?.SetValue(null, false);
            }
        }

        /// <summary>
        /// 设置消息类型背景色
        /// </summary>
        private void SetBackground(MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.Success:
                    ToastBorder.Background = (SolidColorBrush)Resources["ToastSuccess"];
                    break;
                case MsgType.Warning:
                    ToastBorder.Background = (SolidColorBrush)Resources["ToastWarning"];
                    break;
                case MsgType.Error:
                    ToastBorder.Background = (SolidColorBrush)Resources["ToastError"];
                    break;
                default:
                    ToastBorder.Background = (SolidColorBrush)Resources["ToastInfo"];
                    break;
            }
        }

    }
}
