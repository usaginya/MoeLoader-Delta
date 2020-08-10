using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MoeLoaderDelta.Control
{
    /// <summary>
    /// 流畅滚动条 只支持垂直滚动
    /// https://www.cnblogs.com/TwilightLemon/p/13112206.html
    /// Last 2020-8-10
    /// </summary>
    public class SmoothScrollViewer : ScrollViewer
    {
        /// <summary>
        /// 记录上一次的滚动位置
        /// </summary>
        private double LastLocation = 0;

        /// <summary>
        /// 重写鼠标滚动事件
        /// </summary>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            MoveScroll(e.Delta / 1.6);
            //通知ScrollViewer滚动完成
            e.Handled = true;
        }

        /// <summary>
        /// 重写键盘按下事件
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            MoveScroll(null, e);
            e.Handled = true;
        }

        /// <summary>
        /// 重写滚动事件
        /// </summary>
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            //更新记录的位置
            LastLocation = e.VerticalOffset;
           // base.OnScrollChanged(e);
        }

        /// <summary>
        /// 取按键事件默认滚动距离值
        /// </summary>
        private double GetDefaultWheelChange(KeyEventArgs e)
        {
            double wheelChange = 0;
            switch (e.Key)
            {
                case Key.Up: wheelChange = 100; break;
                case Key.Down: wheelChange = -100; break;
                case Key.PageUp: wheelChange = 300; break;
                case Key.PageDown: wheelChange = -300; break;
                case Key.Home: wheelChange = ScrollableHeight; break;
                case Key.End: wheelChange = -ScrollableHeight; break;
            }
            return wheelChange;
        }

        /// <summary>
        /// 移动滚动条 正数页面向上滚动 负数反之
        /// </summary>
        /// <param name="multiple">移动倍数</param>
        /// <param name="keyEvent">按键事件</param>
        public void MoveScroll(double? wheelChange, KeyEventArgs keyEvent = null)
        {
            try
            {
                if (wheelChange == null && keyEvent != null)
                {
                    wheelChange = GetDefaultWheelChange(keyEvent);
                }
                else if (wheelChange == null) { wheelChange = 0; }

                //Animation并不会改变真正的VerticalOffset(只是它的依赖属性) 所以将VOffset设置到上一次的滚动位置 (相当于衔接上一个动画)
                double newOffset = LastLocation - (double)(wheelChange * 2);

                ScrollToVerticalOffset(LastLocation);

                //碰到底部和顶部时的处理
                if (newOffset < 0) { newOffset = 0; }
                if (newOffset > ScrollableHeight) { newOffset = ScrollableHeight; }

                AnimateScroll(newOffset);
                LastLocation = newOffset;
            }
            catch { }
        }

        /// <summary>
        /// 进度条滚动动画
        /// </summary>
        /// <param name="ToValue">滚动位置</param>
        private void AnimateScroll(double ToValue)
        {
            //避免动画重复 先结束掉上一个动画
            BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
            DoubleAnimation Animation = new DoubleAnimation
            {
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut },
                From = VerticalOffset,
                To = ToValue,
                //动画速度
                Duration = TimeSpan.FromMilliseconds(600)
            };
            //固定帧数
            Timeline.SetDesiredFrameRate(Animation, 60);
            BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, Animation);
        }
    }

    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior), new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));
        public static void SetVerticalOffset(FrameworkElement target, double value) => target.SetValue(VerticalOffsetProperty, value);
        public static double GetVerticalOffset(FrameworkElement target) => (double)target.GetValue(VerticalOffsetProperty);
        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) => (target as ScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue);
    }
}
