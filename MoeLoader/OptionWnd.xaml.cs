using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MoeLoader
{
    /// <summary>
    /// 设置窗口
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
                FontFamily = new FontFamily("Microsoft YaHei");
            }

            //if (System.Environment.OSVersion.Version.Major >= 6)
            //{
            //    if (GlassHelper.DwmIsCompositionEnabled())
            //    {
            //        chkAero.IsEnabled = true;
            //    }
            //}

            //SetColor(main.GetColor());
            //chkPos.IsChecked = main.rememberPos;
            txtProxy.Text = MainWindow.Proxy;
            //chkProxy.IsChecked = MainWindow.ProxyEnable;
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

            textNameHelp.ToolTip = "【以下必须是小写英文】\r\n%site 站点名\r\n%id 编号\r\n%tag 标签\r\n%desc 描述\r\n"
                + "%author 作者名\r\n%date 上载时间\r\n%imgp[3] 图册页数[页数总长度(补0)]\r\n\r\n"
                + "<!< 裁剪符号【注意裁剪符号 <!< 只能有一个】\r\n"
                + "表示从 <!< 左边所有名称进行过长裁剪、避免路径过长问题\r\n"
               + "建议把裁剪符号写在 标签%tag 或 描述%desc 后面";
        }

        //private void SetColor(Color c)
        //{
        //    sr.Value = c.R;
        //    sg.Value = c.G;
        //    sb.Value = c.B;
        //    sa.Value = c.A;
        //}

        //private void SetMainColor()
        //{
        //    Color c = Color.FromArgb((byte)sa.Value, (byte)sr.Value, (byte)sg.Value, (byte)sb.Value);
        //    main.SetBackColorLive(c);
        //}

        //private void s_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        //{
        //    SetMainColor();
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txtSaveLocation.Text = txtSaveLocation.Text.Trim();
            if (txtSaveLocation.Text.Length < 3)
            {
                MessageBox.Show("存储位置目录不正确，请重新设置", MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!System.IO.Directory.Exists(txtSaveLocation.Text))
            {
                MessageBoxResult rsl = MessageBox.Show(this, txtSaveLocation.Text +
                    " 目录不存在，要创建它吗？", MainWindow.ProgramName, MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                    MessageBox.Show(this, "代理地址格式不正确，应类似于 127.0.0.1:1080 形式",
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
            //Color c = Color.FromArgb((byte)sa.Value, (byte)sr.Value, (byte)sg.Value, (byte)sb.Value);
            //OK
            //main.SetBackColor(c);
            //main.isAero = chkAero.IsChecked.Value;
            PreFetcher.CachedImgCount = int.Parse(txtCount.Text);

            DownloadControl.SaveLocation = txtSaveLocation.Text;
            main.downloadC.IsSepSave = chkSepSave.IsChecked.Value;
            main.downloadC.IsSaSave = chkSaSave.IsChecked.Value;
            main.downloadC.NumOnce = int.Parse(txtParal.Text);
            //main.rememberPos = chkPos.IsChecked.Value;
            Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            //cancel
            //main.RestoreColor();
            this.Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            //default
            //SetColor(Color.FromArgb(0x21, 0x7C, 0x9E, 0xBE));
            txtProxy.Text = "127.0.0.1:1080";
            txtPattern.Text = MainWindow.DefaultPatter;
            txtBossKey.Text = System.Windows.Forms.Keys.F9.ToString();
            //chkProxy.IsChecked = false;
            rtNoProxy.IsChecked = true;
            //txtProxy.IsEnabled = false;
            //chkPos.IsChecked = false;
            //chkAero.IsChecked = true;
            txtCount.Text = "6";
            chkProxy_Click(null, null);
            txtParal.Text = "2";
            chkSepSave.IsChecked = chkSaSave.IsChecked = false;
            cbBgHe.SelectedIndex = cbBgVe.SelectedIndex = 2;
            cbBgSt.SelectedIndex = 0;
            txtSaveLocation.Text = "MoeLoaderGallery";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //main.RestoreColor();
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
                + "\r\nMoeLoaderΔ ©2016-2017 Moekai All rights reserved.\r\n\r\n"
                , MainWindow.ProgramName + " - About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// browse
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "当前保存位置: " + txtSaveLocation.Text,
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
            MessageBox.Show(this, "将图片文件重命名为 bg.png 或 bg.jpg 后放入 MoeLoader.exe 所在目录，重启 MoeLoader Δ 即可",
                MainWindow.ProgramName, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void chkSepSave_Click(object sender, RoutedEventArgs e)
        {
            chkSaSave.IsEnabled = chkSepSave.IsChecked == null ? false : (bool)chkSepSave.IsChecked;
        }

    }
}