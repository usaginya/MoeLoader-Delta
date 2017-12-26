using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 設定視窗
    /// </summary>
    public partial class OptionWnd : Window
    {
        private MainWindow main;

        public OptionWnd(MainWindow main)
        {
            this.main = main;
            InitializeComponent();
            Title = MainWindow.ProgramName + " Option";

            if (!System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\nofont.txt"))
            {
                FontFamily = new FontFamily("Microsoft JhengHei");
            }

            txtProxy.Text = MainWindow.Proxy;

            rtNoProxy.IsChecked = true;
            if (MainWindow.ProxyType == ProxyType.System)
            {
                rtSystem.IsChecked = true;
            }
            else if (MainWindow.ProxyType == ProxyType.Custom)
            {
                rtCustom.IsChecked = true;
                txtProxy.IsEnabled = true;
            }
            txtBossKey.Text = MainWindow.BossKey.ToString();
            txtPattern.Text = main.namePatter;
            chkProxy_Click(null, null);
            //chkAero.IsChecked = main.isAero;
            txtCount.Text = PreFetcher.CachedImgCount.ToString();
            txtParal.Text = main.downloadC.NumOnce.ToString();
            chkSepSave.IsChecked = chkSaSave.IsEnabled = main.downloadC.IsSepSave;
            chkSaSave.IsChecked = main.downloadC.IsSaSave;
            txtSaveLocation.Text = DownloadControl.SaveLocation;

            if (main.bgSt == Stretch.None)
            {
                cbBgSt.SelectedIndex = 0;
            }
            else if (main.bgSt == Stretch.Uniform)
            {
                cbBgSt.SelectedIndex = 1;
            }
            else if (main.bgSt == Stretch.UniformToFill)
            {
                cbBgSt.SelectedIndex = 2;
            }

            if (main.bgHe == AlignmentX.Left)
            {
                cbBgHe.SelectedIndex = 0;
            }
            else if (main.bgHe == AlignmentX.Center)
            {
                cbBgHe.SelectedIndex = 1;
            }
            else if (main.bgHe == AlignmentX.Right)
            {
                cbBgHe.SelectedIndex = 2;
            }

            if (main.bgVe == AlignmentY.Top)
            {
                cbBgVe.SelectedIndex = 0;
            }
            else if (main.bgVe == AlignmentY.Center)
            {
                cbBgVe.SelectedIndex = 1;
            }
            else if (main.bgVe == AlignmentY.Bottom)
            {
                cbBgVe.SelectedIndex = 2;
            }

            textNameHelp.ToolTip = "【以下必須是小寫英文】\r\n%site 站點名\r\n%id 編號\r\n%tag 標籤\r\n%desc 描述\r\n"
                + "%author 作者名\r\n%date 上載時間\r\n%imgp[3] 圖冊頁數[頁數總長度(補0)]\r\n\r\n"
                + "<!< 裁剪符號【注意裁剪符號 <!< 只能有一個】\r\n"
                + "表示從 <!< 左邊所有名稱進行過長裁剪、避免路徑過長問題\r\n"
               + "建議把裁剪符號寫在 標籤%tag 或 描述%desc 後面";

            #region 檔案名規則格式按鈕綁定
            FNRsite.Click += new RoutedEventHandler(FNRinsert);
            FNRid.Click += new RoutedEventHandler(FNRinsert);
            FNRtag.Click += new RoutedEventHandler(FNRinsert);
            FNRdesc.Click += new RoutedEventHandler(FNRinsert);
            FNRauthor.Click += new RoutedEventHandler(FNRinsert);
            FNRdate.Click += new RoutedEventHandler(FNRinsert);
            FNRimgp.Click += new RoutedEventHandler(FNRinsert);
            FNRcut.Click += new RoutedEventHandler(FNRinsert);
            #endregion
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txtSaveLocation.Text = txtSaveLocation.Text.Trim();
            if (txtSaveLocation.Text.Length < 3)
            {
                MessageBox.Show("存儲位置目錄不正確，請重新設定", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!System.IO.Directory.Exists(txtSaveLocation.Text))
            {
                MessageBoxResult rsl = MessageBox.Show(this, txtSaveLocation.Text +
                    " 目錄不存在，要創建它嗎？", MainWindow.ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (rsl == MessageBoxResult.Yes)
                {
                    System.IO.Directory.CreateDirectory(txtSaveLocation.Text);
                }
                else
                {
                    return;
                }
            }
            if (txtProxy.Text.Trim().Length > 0)
            {
                string add = txtProxy.Text.Trim();
                bool right = false;
                if (System.Text.RegularExpressions.Regex.IsMatch(add, @"^.+:(\d+)$"))
                //@"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5]):(\d+)$"))
                {
                    int port;
                    if (int.TryParse(add.Substring(add.IndexOf(':') + 1), out port))
                    {
                        if (port > 0 && port < 65535)
                        {
                            MainWindow.Proxy = txtProxy.Text.Trim();
                            right = true;
                        }
                    }
                }
                if (!right)
                {
                    MessageBox.Show(this, "代理地址格式不正確，應類似於 127.0.0.1:1080 形式",
                        MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else MainWindow.Proxy = "";

            if (rtNoProxy.IsChecked.Value)
            {
                MainWindow.ProxyType = ProxyType.None;
            }
            else if (rtSystem.IsChecked.Value)
            {
                MainWindow.ProxyType = ProxyType.System;
            }
            else
            {
                MainWindow.ProxyType = ProxyType.Custom;
            }

            MainWindow.BossKey = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), txtBossKey.Text);
            main.namePatter = txtPattern.Text.Replace(";", "；").Trim();

            if (cbBgSt.SelectedIndex == 0)
            {
                main.bgSt = Stretch.None;
            }
            else if (cbBgSt.SelectedIndex == 1)
            {
                main.bgSt = Stretch.Uniform;
            }
            else if (cbBgSt.SelectedIndex == 2)
            {
                main.bgSt = Stretch.UniformToFill;
            }

            if (cbBgHe.SelectedIndex == 0)
            {
                main.bgHe = AlignmentX.Left;
            }
            else if (cbBgHe.SelectedIndex == 1)
            {
                main.bgHe = AlignmentX.Center;
            }
            else if (cbBgHe.SelectedIndex == 2)
            {
                main.bgHe = AlignmentX.Right;
            }

            if (cbBgVe.SelectedIndex == 0)
            {
                main.bgVe = AlignmentY.Top;
            }
            else if (cbBgVe.SelectedIndex == 1)
            {
                main.bgVe = AlignmentY.Center;
            }
            else if (cbBgVe.SelectedIndex == 2)
            {
                main.bgVe = AlignmentY.Bottom;
            }
            if (main.bgImg != null)
            {
                main.bgImg.Stretch = main.bgSt;
                main.bgImg.AlignmentX = main.bgHe;
                main.bgImg.AlignmentY = main.bgVe;
            }

            PreFetcher.CachedImgCount = int.Parse(txtCount.Text);

            DownloadControl.SaveLocation = txtSaveLocation.Text;
            main.downloadC.IsSepSave = chkSepSave.IsChecked.Value;
            main.downloadC.IsSaSave = chkSaSave.IsChecked.Value;
            main.downloadC.NumOnce = int.Parse(txtParal.Text);

            Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            //default
            txtProxy.Text = "127.0.0.1:1080";
            txtPattern.Text = MainWindow.DefaultPatter;
            txtBossKey.Text = System.Windows.Forms.Keys.F9.ToString();
            rtNoProxy.IsChecked = true;
            txtCount.Text = "6";
            chkProxy_Click(null, null);
            txtParal.Text = "2";
            chkSepSave.IsChecked = chkSaSave.IsChecked = chkSaSave.IsEnabled = false;
            cbBgHe.SelectedIndex = cbBgVe.SelectedIndex = 2;
            cbBgSt.SelectedIndex = 0;
            txtSaveLocation.Text = "MoeLoaderGallery";
        }

        private void txtBossKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.System && e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl && e.Key != Key.LeftShift
                && e.Key != Key.RightAlt && e.Key != Key.RightCtrl && e.Key != Key.RightShift && e.Key != Key.LWin && e.Key != Key.RWin)
            {
                txtBossKey.Text = ((System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key)).ToString();
            }
            e.Handled = true;
        }

        private void chkProxy_Click(object sender, RoutedEventArgs e)
        {
            if (txtProxy != null)
            {
                if (rtNoProxy.IsChecked.Value)
                    txtProxy.IsEnabled = false;
                else if (rtSystem.IsChecked.Value)
                    txtProxy.IsEnabled = false;
                else
                    txtProxy.IsEnabled = true;
            }
        }

        #region prefetch img count
        private void txtPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 || e.Key >= Key.D0 && e.Key <= Key.D9 || e.Key == Key.Back || e.Key == Key.Enter
                || e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                e.Handled = true;
            }
        }

        private void pageUp_Click(object sender, RoutedEventArgs e)
        {
            int value = int.Parse(txtCount.Text);
            if (value < 20)
                txtCount.Text = (value + 1).ToString();
        }

        private void pageDown_Click(object sender, RoutedEventArgs e)
        {
            int value = int.Parse(txtCount.Text);
            if (value > 1)
                txtCount.Text = (value - 1).ToString();
        }
        private void pageUp_Click1(object sender, RoutedEventArgs e)
        {
            int value = int.Parse(txtParal.Text);
            if (value < 5)
                txtParal.Text = (value + 1).ToString();
        }

        private void pageDown_Click1(object sender, RoutedEventArgs e)
        {
            int value = int.Parse(txtParal.Text);
            if (value > 1)
                txtParal.Text = (value - 1).ToString();
        }
        #endregion

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, MainWindow.ProgramName + " Ver" + MainWindow.ProgramVersion
                + "\r\n\r\n"
                + "Email: esonice@gmail.com\r\nSite: http://moeloader.sinaapp.com/"
                + "\r\nMoeLoader ©2008-2013 esonic All rights reserved.\r\n\r\n"
                + "Δ Version by YIU\r\n"
                + "Email: degdod@qq.com\r\nSite: http://usaginya.lofter.com/"
                + "\r\nMoeLoader Δ ©2016-2017 Moekai All rights reserved.\r\n\r\n"
                , MainWindow.ProgramName + " - About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// browse
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "當前儲存位置: " + txtSaveLocation.Text,
                SelectedPath = txtSaveLocation.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSaveLocation.Text = dialog.SelectedPath;
            }
        }

        private void textNameHelp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, textNameHelp.ToolTip.ToString(), MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this, "將圖片檔案重新命名為 bg.png 或 bg.jpg 後放入 MoeLoaderDelta.exe 所在目錄，重啟 MoeLoader Δ 即可",
                MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void chkSepSave_Click(object sender, RoutedEventArgs e)
        {
            chkSaSave.IsEnabled = chkSepSave.IsChecked == null ? false : (bool)chkSepSave.IsChecked;
        }

        /// <summary>
        /// 插入格式到規則文字框
        /// </summary>
        private void FNRinsert(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string format = btn.Content.ToSafeString();
            int selectstart = txtPattern.SelectionStart;

            if (string.IsNullOrWhiteSpace(txtPattern.SelectedText))
            {
                if (format.Contains("imgp"))
                    txtPattern.Text = txtPattern.Text.Insert(selectstart, format.Replace("n", "3"));
                else
                    txtPattern.Text = txtPattern.Text.Insert(selectstart, format);
            }
            else
            {
                if (format.Contains("imgp"))
                    txtPattern.SelectedText = format.Replace("n", "3");
                else
                    txtPattern.SelectedText = format;
                txtPattern.SelectionLength = 0;
            }
            txtPattern.SelectionStart = selectstart + format.Length;
            txtPattern.Focus();
        }
    }
}