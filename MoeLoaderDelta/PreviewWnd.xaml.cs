using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using MoeLoaderDelta.Control;
using System.IO;
using System.Linq;

namespace MoeLoaderDelta
{
    /// <summary>
    /// Interaction logic for PreviewWnd.xaml
    /// 預覽視窗
    /// </summary>
    public partial class PreviewWnd : Window
    {
        private MainWindow mainW;
        //自訂資料類型
        //id   index
        private Dictionary<int, int> imgs = new Dictionary<int, int>();
        private Dictionary<int, Img> descs = new Dictionary<int, Img>();

        //主視窗縮圖索引
        private Dictionary<int, int> oriIndex = new Dictionary<int, int>();
        private int selectedId;
        private int index;
        //上次滑鼠的位置
        private int preMX, preMY;

        #region === GetSet封裝 ===
        public int SelectedId
        {
            get
            {
                return selectedId;
            }

            set
            {
                selectedId = value;
            }
        }

        public Dictionary<int, int> Imgs
        {
            get
            {
                return imgs;
            }

            set
            {
                imgs = value;
            }
        }

        public Dictionary<int, Img> Descs
        {
            get
            {
                return descs;
            }
        }
        #endregion

        public PreviewWnd(MainWindow mainW)
        {
            this.mainW = mainW;
            InitializeComponent();
            Title = MainWindow.ProgramName + " Preview";

            if (!File.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft JhengHei");
            }

            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xF5, 0xF5, 0xF5));
            MouseLeftButtonDown += new MouseButtonEventHandler(MainWindow_MouseLeftButtonDown);
            KeyDown += new KeyEventHandler(Window_KeyDown);
        }

        void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        /// <summary>
        /// 添加預覽
        /// </summary>
        /// <param name="img"></param>
        /// <param name="parentIndex"></param>
        /// <param name="needReferer"></param>
        public void AddPreview(Img img, int parentIndex, string needReferer)
        {
            if (!imgs.ContainsKey(img.Id))
            {
                imgs.Add(img.Id, index++);
                oriIndex.Add(img.Id, parentIndex);
                descs.Add(img.Id, img);
                //添加預覽圖分頁按鈕
                ToggleButton btn = new ToggleButton
                {
                    Content = img.Id,
                    Margin = new Thickness(3, 1, 3, 1)
                };
                btn.Checked += new RoutedEventHandler(btn_Click);
                btns.Children.Add(btn);

                //初始化預覽圖
                PreviewImg prei = new PreviewImg(this, img);
                prei.MouseLeftButtonUp += new MouseButtonEventHandler(delegate (object s1, MouseButtonEventArgs ea)
                {
                    preMX = 0; preMY = 0;
                });
                prei.MouseLeftButtonDown += new MouseButtonEventHandler(delegate (object s1, MouseButtonEventArgs ea)
                {
                    preMX = 0; preMY = 0;
                });
                prei.MouseDown += new MouseButtonEventHandler(delegate (object s1, MouseButtonEventArgs ea)
                {
                    //中鍵縮放
                    if (ea.MiddleButton == MouseButtonState.Pressed)
                        Button_Click_2(null, null);
                });
                prei.MouseMove += new MouseEventHandler(delegate (object s1, MouseEventArgs ea)
                {
                    //拖動
                    if (ea.LeftButton == MouseButtonState.Pressed)
                    {
                        if (preMY != 0 && preMX != 0)
                        {
                            int offX = (int)(ea.GetPosition(LayoutRoot).X) - preMX;
                            int offY = (int)(ea.GetPosition(LayoutRoot).Y) - preMY;
                            ScrollViewer sc = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                            sc.ScrollToHorizontalOffset(sc.HorizontalOffset - offX);
                            sc.ScrollToVerticalOffset(sc.VerticalOffset - offY);
                        }
                        preMX = (int)(ea.GetPosition(LayoutRoot).X);
                        preMY = (int)(ea.GetPosition(LayoutRoot).Y);
                    }
                });

                //加入預覽圖控制項
                imgGrid.Children.Add(new ScrollViewer()
                {
                    Content = prei,
                    Visibility = Visibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
                });

                //開始下載圖片
                prei.DownloadImg(img.Id, img.SampleUrl, needReferer);

                if (selectedId == 0)
                {
                    (btns.Children[btns.Children.Count - 1] as ToggleButton).IsChecked = true;
                }
            }
        }


        /// <summary>
        /// 切換預覽圖
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btn_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)(sender as ToggleButton).Content;
            SwitchPreview(id);
        }

        /// <summary>
        /// 切換預覽圖操作
        /// </summary>
        /// <param name="id">預覽ID</param>
        private void SwitchPreview(int id)
        {
            if (selectedId != id)
            {
                if (selectedId != 0 && imgs.ContainsKey(selectedId))
                {
                    (btns.Children[imgs[selectedId]] as ToggleButton).IsChecked = false;

                    //(imgGrid.Children[imgs[selectedId]] as Image).Opacity = 0;
                    //(imgGrid.Children[imgs[selectedId]] as Image).BeginStoryboard(FindResource("imgClose") as Storyboard);
                    ScrollViewer tempPreview = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                    Storyboard sb = new Storyboard();
                    DoubleAnimationUsingKeyFrames frames = new DoubleAnimationUsingKeyFrames();
                    Storyboard.SetTargetProperty(frames, new PropertyPath(UIElement.OpacityProperty));
                    frames.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
                    sb.Children.Add(frames);
                    sb.Completed += new EventHandler(delegate (object s, EventArgs ea) { tempPreview.Visibility = Visibility.Hidden; });
                    sb.Begin(tempPreview);
                }
                selectedId = id;
                (btns.Children[imgs[selectedId]] as ToggleButton).IsChecked = true;
                ScrollViewer tempPreview1 = (imgGrid.Children[imgs[selectedId]] as ScrollViewer);
                tempPreview1.Visibility = Visibility.Visible;
                tempPreview1.BeginStoryboard(FindResource("imgShow") as Storyboard);

                ///////////////////////////////////////////////
                ////////////////////////////////////////////

                desc.Text = "";
                if (descs[selectedId].OriginalUrl == descs[selectedId].SampleUrl)
                {
                    desc.Inlines.Add("原圖與預覽圖相同");
                    desc.Inlines.Add(new LineBreak());
                }
                desc.Inlines.Add("描述: " + descs[selectedId].Id + " " + descs[selectedId].Desc);
                desc.Inlines.Add(new LineBreak());
                desc.Inlines.Add("作者: " + descs[selectedId].Author);
                desc.Inlines.Add(new LineBreak());
                try
                {
                    string fileType = descs[selectedId].OriginalUrl.Substring(descs[selectedId].OriginalUrl.LastIndexOf('.') + 1);
                    desc.Inlines.Add("類型: " + BooruProcessor.FormattedImgUrl("", fileType.ToUpper()));
                }
                catch { }
                desc.Inlines.Add(" 大小: " + descs[selectedId].FileSize);
                desc.Inlines.Add(" 尺寸: " + descs[selectedId].Dimension);
                //desc.Inlines.Add(new LineBreak());
                desc.Inlines.Add(" 評分: " + descs[selectedId].Score);
                desc.Inlines.Add(new LineBreak());
                desc.Inlines.Add("時間: " + descs[selectedId].Date);
                if (descs[selectedId].Source.Length > 0)
                {
                    desc.Inlines.Add(new LineBreak());
                    desc.Inlines.Add("來源: " + descs[selectedId].Source);
                }
            }
        }

        /// <summary>
        /// 選中並關閉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId != 0)
            {
                mainW.SelectByIndex(oriIndex[selectedId]);
                CloseImg();
            }
        }

        /// <summary>
        /// 關閉
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId != 0)
            {
                CloseImg();
            }
        }

        private void CloseImg()
        {
            //close
            int oriId = selectedId;
            int imgc = imgs.Count;

            PreviewImg pi = (imgGrid.Children[imgs[oriId]] as ScrollViewer).Content as PreviewImg;
            pi.StopLoadImg(oriId, "");

            //移除按鈕和預覽圖
            btns.Children.Remove(btns.Children[imgs[oriId]]);
            imgGrid.Children.Remove(imgGrid.Children[imgs[oriId]]);

            if (imgc > 0) index = imgs[oriId];

            //刪除關閉資料
            imgs.Remove(oriId);
            descs.Remove(oriId);
            oriIndex.Remove(oriId);

            imgc = imgs.Count;
            if (imgc > 0)
            {
                //更新數組索引值
                for (int i = index; i < imgc; i++)
                {
                    int newindex = imgs[(int)((ToggleButton)btns.Children[i]).Content];
                    imgs[(int)((ToggleButton)btns.Children[i]).Content] = --newindex;
                }

                //切換預覽圖
                //選擇關閉的圖前一張
                index--;
                //沒有前一張就選第一張
                index = index < 0 ? 0 : index;

                //選中按鈕
                ToggleButton checkedTB = (ToggleButton)btns.Children[index];
                checkedTB.IsChecked = true;

                //index為0時添加圖片不能自增、因此需要在此設1
                index = index < 1 ? 1 : index;
                GC.Collect();
            }
            else
            {
                selectedId = index = 0;
                GC.Collect();
                Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //清理預覽圖組資料
            foreach (int iid in imgs.Values)
            {
                PreviewImg pi = (imgGrid.Children[iid] as ScrollViewer).Content as PreviewImg;
                var dicSort = from pireq in pi.Reqs orderby pireq.Value descending select pireq;
                foreach (KeyValuePair<int, System.Net.HttpWebRequest> req in dicSort)
                    req.Value.Abort();
                pi.Reqs.Clear();
            }
            imgs.Clear();
            descs.Clear();
            oriIndex.Clear();
            imgGrid.Children.Clear();
            mainW.previewFrm = null;

            (new System.Threading.Thread(
                new System.Threading.ThreadStart(delegate ()
                {
                    //啟動回收
                    System.Threading.Thread.Sleep(2000);
                    GC.Collect();
                })
             )).Start();
        }

        /// <summary>
        /// 儲存預覽圖
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SavePreview(false, DownloadControl.SaveLocation + "\\" + GetSelectPreviewImgFileName());
        }

        /// <summary>
        /// 儲存預覽圖到檔案
        /// </summary>
        /// <param name="silent">靜默模式</param>
        /// <param name="path">儲存路徑</param>
        /// <returns>預覽檔案類型</returns>
        private string SavePreview(bool silent, string path)
        {
            try
            {
                if (!((imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as PreviewImg).ImgLoaded)
                {
                    ShowMessage("圖片尚未載入完畢", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return "";
                }

                PreviewImg pimg = (PreviewImg)((ScrollViewer)imgGrid.Children[imgs[selectedId]]).Content;
                switch (pimg.ImgType)
                {
                    case "bmp":
                    case "jpg":
                    case "png":
                        Image im = (Image)pimg.prewimg.Children[0];
                        DataHelpers.ImageToFile((BitmapSource)im.Source, pimg.ImgType, path);
                        break;
                    case "gif":
                        AnimatedGIF gi = (AnimatedGIF)pimg.prewimg.Children[0];
                        DataHelpers.MemoryStreamToFile((MemoryStream)gi.GIFSource, path);
                        break;
                    default:
                        throw new Exception(pimg.ImgType + "類型不支援儲存");
                }

                if (!silent)
                    ShowMessage("已成功儲存至下載資料夾", MessageBoxButton.OK, MessageBoxImage.Information);

                return pimg.ImgType;
            }
            catch (Exception ex)
            {
                if (!silent)
                    ShowMessage("儲存失敗\r\n" + ex.Message, MessageBoxButton.OK, MessageBoxImage.Warning);
                return "";
            }
        }

        /// <summary>
        /// 複製預覽圖
        /// 以QQ剪下板格式創建
        /// http://blog.csdn.net/crystal_lz/article/details/51737713
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //取當前預覽內容、類型、儲存路徑
                PreviewImg pimg = (PreviewImg)((ScrollViewer)imgGrid.Children[imgs[selectedId]]).Content;

                bool novprew = false;
                string ttype = pimg.ImgType;
                string tempfile = GetTempFilePath();

                if (!File.Exists(tempfile))
                    if (SavePreview(true, tempfile) == "") return;

                //構建剪貼對象
                IDataObject clipobj = new DataObject();

                //根據類型分類處理
                switch (ttype)
                {
                    case "bmp":
                    case "jpg":
                    case "png":
                    case "gif":

                        //--- 複製編輯框格式 ---
                        StringBuilder sb = new StringBuilder();
                        sb.Append("<QQRichEditFormat><Info version=\"1001\"></Info>");
                        sb.AppendFormat("<EditElement type=\"1\" filepath=\"{0}\" shortcut=\"\"></EditElement>", tempfile);
                        sb.Append("<EditElement type=\"0\"><![CDATA[]]></EditElement></QQRichEditFormat>");

                        byte[] bydate = Encoding.UTF8.GetBytes(sb.ToSafeString());
                        clipobj.SetData("QQ_Unicode_RichEdit_Format", new MemoryStream(bydate));
                        clipobj.SetData("QQ_RichEdit_Format", new MemoryStream(bydate));

                        //--- 複製圖像資料 ---
                        System.Drawing.Image di = System.Drawing.Image.FromStream(pimg.Strs);
                        clipobj.SetData(DataFormats.Dib, di);
                        break;

                    default:
                        novprew = true;
                        break;
                }

                if (novprew)
                    ShowMessage(ttype + "類型不支援複製預覽，僅複製了檔案", MessageBoxButton.OK, MessageBoxImage.Warning);

                //--- 複製預覽檔案 ---
                string[] tempfs = new string[] { tempfile };
                clipobj.SetData("FileDrop", tempfs);
                clipobj.SetData("FileNameW", tempfs);
                clipobj.SetData("FileName", tempfs);

                //置入剪貼對象到剪貼簿
                Clipboard.SetDataObject(clipobj, true);

            }
            catch (Exception ex)
            {
                ShowMessage("複製預覽 " + selectedId + " 失敗\r\n" + ex.Message, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 縮放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (!((imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as PreviewImg).ImgLoaded)
            {
                ShowMessage("圖片尚未載入完畢", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                PreviewImg img = (imgGrid.Children[imgs[selectedId]] as ScrollViewer).Content as PreviewImg;
                if (img.isZoom)
                {
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    img.ImgZoom(false, false);
                }
                else
                {
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    (imgGrid.Children[imgs[selectedId]] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    img.ImgZoom(false);
                }
            }
        }

        /// <summary>
        /// 複製預覽連結
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].SampleUrl);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 複製原始連結
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].OriginalUrl);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 複製來源連結
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].Source);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 複製描述
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].Desc);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 複製詳情頁連結
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            try
            {
                if (descs[selectedId].DetailUrl.Length > 0)
                    System.Diagnostics.Process.Start(descs[selectedId].DetailUrl);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 複製JPG連結
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(descs[selectedId].JpegUrl);
            }
            catch (Exception) { }
        }

        #region 訊息框滑鼠懸浮樣式處理
        private void Border_MouseEnter_1(object sender, MouseEventArgs e)
        {
            brdDesc.Opacity = 0.05;
        }

        private void Border_MouseLeave_1(object sender, MouseEventArgs e)
        {
            brdDesc.Opacity = 1;
        }
        #endregion

        /// <summary>
        /// 取預覽圖遠程檔案名
        /// </summary>
        /// <returns></returns>
        private string GetSelectPreviewImgFileName()
        {
            string dest = Uri.UnescapeDataString(descs[selectedId].SampleUrl.Substring(descs[selectedId].SampleUrl.LastIndexOf('/') + 1));
            dest = DownloadControl.ReplaceInvalidPathChars(dest);
            return dest;
        }

        /// <summary>
        /// 取臨時檔案路徑
        /// </summary>
        /// <returns>臨時路徑</returns>
        private string GetTempFilePath()
        {
            string tp = Path.GetTempPath();
            string dr = tp + @"Moeloadelta\";

            if (!Directory.Exists(dr))
                Directory.CreateDirectory(dr);
            return dr + mainW.siteMenu.Header + "_" + GetSelectPreviewImgFileName();
        }

        /// <summary>
        /// 顯示提示消息框
        /// </summary>
        /// <param name="Msg">消息內容</param>
        /// <param name="MsgButton">消息按鈕</param>
        /// <param name="MsgImg">消息圖示</param>
        private void ShowMessage(string Msg, MessageBoxButton MsgButton, MessageBoxImage MsgImg)
        {
            MessageBox.Show(this, Msg, MainWindow.ProgramName, MsgButton, MsgImg);
        }



        /// <summary>
        /// 預覽視窗按鍵處理
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + C 複製預覽
            if (MainWindow.IsCtrlDown() && e.Key == Key.C)
            {
                if (IsLoaded)
                {
                    MenuItem_Click(null, null);
                }
            }
            else if (e.Key == Key.NumPad4)//數字鍵盤4 切換上個預覽
            {
                SwitchNearPreview();
            }
            else if (e.Key == Key.NumPad6)//數字鍵盤6 切換下個預覽
            {
                SwitchNearPreview(1);
            }
            else if (MainWindow.IsCtrlDown() && e.Key == Key.W) //Ctrl + W 關閉當前預覽
            {
                Button1_Click(null, null);
            }
        }

        /// <summary>
        /// 切換臨近預覽
        /// </summary>
        /// <param name="next">0上一個 非0下一個</param>
        private void SwitchNearPreview(int next)
        {
            if (imgs.Count > 0)
            {
                bool isact = false;
                int nowprew = imgs[selectedId];
                int lestprew = imgs.Count - 1;

                if (next > 0 && nowprew < lestprew) //有下一個
                {
                    nowprew++;
                    isact = true;
                }
                else if (next < 1 && nowprew > 0)//有上一個
                {
                    nowprew--;
                    isact = true;
                }
                else if (next > 0 && nowprew == lestprew)//沒有下一個時切到第1個
                {
                    nowprew = 0;
                    isact = true;
                }
                else if (next < 1 && nowprew == 0)//沒有上一個時切到最後1個
                {
                    nowprew = lestprew;
                    isact = true;
                }

                if (isact)
                {
                    foreach (int id in imgs.Keys)
                    {
                        if (imgs[id] == nowprew)
                        {
                            nowprew = id;
                            break;
                        }
                    }
                    SwitchPreview(nowprew);
                }
            }
        }
        private void SwitchNearPreview()
        {
            SwitchNearPreview(0);
        }

    }
}
