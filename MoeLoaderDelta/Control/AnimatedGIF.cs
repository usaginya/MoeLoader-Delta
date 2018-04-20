﻿//source( Microsoft All-In-One Code Framework ):http://1code.codeplex.com/
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using File = System.Utility.Helper.File;
using Image = System.Windows.Controls.Image;

namespace MoeLoaderDelta.Control
{
    /// <summary>
    /// 支援GIF動畫圖片播放的圖片控制項，GIF圖片源GIFSource
    /// </summary>
    public class AnimatedGIF : Image
    {
        public static readonly DependencyProperty GIFSourceProperty = DependencyProperty.Register(
            "GIFSource", typeof(object), typeof(AnimatedGIF), new PropertyMetadata(OnSourcePropertyChanged));

        /// <summary>
        /// GIF圖片源，支援相對路徑、絕對路徑、Steam
        /// </summary>
        public object GIFSource
        {
            get { return (object)GetValue(GIFSourceProperty); }
            set { SetValue(GIFSourceProperty, value); }
        }

        internal Bitmap Bitmap; // Local bitmap member to cache image resource
        internal BitmapSource BitmapSource;
        public delegate void FrameUpdatedEventHandler();

        //gif寬和高
        private static double imgw = double.NaN, imgh = double.NaN;

        //========= 封裝 ==========
        public double PixelWidth
        {
            get
            {
                return imgw;
            }
        }

        public double PixelHeight
        {
            get
            {
                return imgh;
            }
        }


        /// <summary>
        /// Delete local bitmap resource
        /// Reference: http://msdn.microsoft.com/en-us/library/dd183539(VS.85).aspx
        /// </summary>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DeleteObject(IntPtr hObject);

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Loaded += AnimatedGIF_Loaded;
            Unloaded += AnimatedGIF_Unloaded;
        }

        void AnimatedGIF_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimate();
        }

        void AnimatedGIF_Loaded(object sender, RoutedEventArgs e)
        {
            BindSource(this);
        }

        /// <summary>
        /// Start animation
        /// </summary>
        public void StartAnimate()
        {
            if (ImageAnimator.CanAnimate(Bitmap))
                ImageAnimator.Animate(Bitmap, OnFrameChanged);
            else
            {
                if (BitmapSource != null)
                    BitmapSource.Freeze();
                Source = BitmapSource;
            }
        }

        /// <summary>
        /// Stop animation
        /// </summary>
        public void StopAnimate()
        {
            ImageAnimator.StopAnimate(Bitmap, OnFrameChanged);
        }

        /// <summary>
        /// Event handler for the frame changed
        /// </summary>
        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                   new FrameUpdatedEventHandler(FrameUpdatedCallback));
        }

        private void FrameUpdatedCallback()
        {
            ImageAnimator.UpdateFrames();

            if (BitmapSource != null)
                BitmapSource.Freeze();

            // Convert the bitmap to BitmapSource that can be display in WPF Visual Tree
            BitmapSource = GetBitmapSource(this.Bitmap, this.BitmapSource);
            Source = BitmapSource;
            InvalidateVisual();
        }

        /// <summary>
        /// 屬性更改處理事件
        /// </summary>
        private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            AnimatedGIF gif = sender as AnimatedGIF;
            if (gif == null) return;
            if (!gif.IsLoaded) return;
            BindSource(gif);
        }
        private static void BindSource(AnimatedGIF gif)
        {
            gif.StopAnimate();
            if (gif.Bitmap != null) gif.Bitmap.Dispose();
            object source = gif.GIFSource;
            if (source == null) return;

            //根據類型處理
            string sourcetype = source.GetType().ToSafeString();
            if (sourcetype.Contains("System.String"))
            {
                //檔案路徑
                string path = source.ToSafeString();
                if (path.IsInvalid()) return;
                if (!Path.IsPathRooted(path))
                {
                    source = File.GetPhysicalPath(path);
                }
                gif.Bitmap = new Bitmap(path);
            }
            else if (sourcetype.Contains("System.IO.Stream"))
            {
                //io.Stream
                gif.Bitmap = new Bitmap(source as Stream);
            }
            else if (sourcetype.Contains("System.IO.MemoryStream"))
            {
                //io.MemoryStream
                gif.Bitmap = new Bitmap(source as MemoryStream);
            }
            else
            {
                //無效
                throw new Exception("Unsupported Stream");
            }

            GetWidthHeight(gif, ref imgw, ref imgh);
            gif.BitmapSource = GetBitmapSource(gif.Bitmap, gif.BitmapSource);
            gif.StartAnimate();
        }
        /// <summary>
        /// 從Bitmap取得BitmapSource
        /// </summary>
        /// <param name="bmap">Bitmap</param>
        /// <param name="bimg">BitmapSource</param>
        /// <returns></returns>
        private static BitmapSource GetBitmapSource(Bitmap bmap, BitmapSource bimg)
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                handle = bmap.GetHbitmap();
                bimg = Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    DeleteObject(handle);
            }
            return bimg;
        }

        /// <summary>
        /// 取當前GIF寬度高度
        /// </summary>
        /// <param name="gif">AnimatedGIF</param>
        /// <param name="width">寬</param>
        /// <param name="height">高</param>
        public static void GetWidthHeight(AnimatedGIF gif, ref double width, ref double height)
        {
            //透過Drawing.Image取寬高
            Bitmap gb = gif.Bitmap;
            MemoryStream ms = new MemoryStream();
            gb.Save(ms, gb.RawFormat);
            System.Drawing.Image dimg = System.Drawing.Image.FromStream(ms);
            width = dimg.Width;
            height = dimg.Height;
            ms.Dispose();
            ms.Close();
        }
    }
}
