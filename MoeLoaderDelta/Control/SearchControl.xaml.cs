using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MoeLoaderDelta
{
    /// <summary>
    /// Interaction logic for SearchControl.xaml
    /// </summary>
    public partial class SearchControl : UserControl
    {
        //private System.Net.HttpWebRequest req;
        private Storyboard sb;

        private SessionState currentSession;
        public event EventHandler Enteded;
        private Thread WaitAutoComplete;

        public SearchControl()
        {
            InitializeComponent();

            sb = (Storyboard)FindResource("searching");

            txtSearch.ToolTip = "按下Delete键可删除当前的搜索关键词"
                + Environment.NewLine + "按下Shift+Delete键可删除全部搜索过的关键词";

            //sb.Begin(path, true);
            //sb.Pause();
        }

        /// <summary>
        /// 文本框最后选中的文本
        /// </summary>
        private string lastSelectText = string.Empty;

        /// <summary>
        /// 最后输入状态 True=0  False>0
        /// </summary>
        private int lastInputState = 1;

        private ScrollViewer PopupScroll => (ScrollViewer)txtSearch.Template.FindName("PopupScroll", txtSearch);

        public TextBox Textbox => (TextBox)txtSearch.Template.FindName("PART_EditableTextBox", txtSearch);

        private string textText = string.Empty;
        public string Text
        {
            get => textText == (string)Textbox.Tag ? string.Empty : textText;
            set => Textbox.Text = textText = value;
        }

        private LinkedList<string> usedItems = new LinkedList<string>();

        /// <summary>
        /// 最近搜索过的词
        /// </summary>
        public string[] UsedItems
        {
            get { return usedItems.ToArray(); }
        }

        /// <summary>
        /// 添加搜索过的词
        /// </summary>
        /// <param name="word"></param>
        public void AddUsedItem(string word)
        {
            if (word != null && word.Trim().Length > 0 && word != "搜索")
            {
                if (usedItems.Contains(word))
                {
                    usedItems.Remove(word);
                    usedItems.AddFirst(word);
                }

                else
                {
                    if (usedItems.Count > 30)
                        usedItems.RemoveLast();
                    usedItems.AddFirst(word);
                }

            }
        }

        /// <summary>
        /// 从配置文件中加载搜索过的词
        /// </summary>
        /// <param name="word"></param>
        public void LoadUsedItems(string word)
        {
            if (word != null && word.Trim().Length > 0 && !usedItems.Contains(word) && word != "搜索")
            {
                if (usedItems.Count < 30)
                    usedItems.AddLast(word);
            }
        }

        private void ShowUsedItems()
        {
            if (SiteManager.Instance.Sites.Count > 0 && !SiteManager.Instance.Sites[MainWindow.MainW.comboBoxIndex].IsSupportTag)
            {
                txtSearch.Items.Add(new SearchItem() { Name = "该站点无关键词自动提示", Count = null, Enabled = false });
            }
            txtSearch.Items.Add(new SearchItem() { Name = "------------------最近搜索的关键词------------------", Count = null, Enabled = false });
            foreach (string item in usedItems)
                txtSearch.Items.Add(new SearchItem() { Name = item, Count = null });

            //重载搜索列表时滚动到顶部
            PopupScroll.ScrollToTop();
        }

        private void PART_EditableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = Textbox.Text;
            lastInputState = lastInputState > 1 ? lastInputState : 0;
            txtSearch.Items.Clear();
            ShowUsedItems();
            txtSearch.IsDropDownOpen = true;
            Textbox.ToolTip = Text.Length > 18
                ? Text + Environment.NewLine + Environment.NewLine + txtSearch.ToolTip
                : txtSearch.ToolTip;

            (sender as TextBox).Foreground = Text == (string)Textbox.Tag
                ? new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA))
                : new SolidColorBrush(Colors.Black);

            //auto hint when site support, wait for 1 second to run
            if (WaitAutoComplete != null) { try { WaitAutoComplete.Abort(); } catch { } }
            WaitAutoComplete = new Thread(() =>
            {
                Thread.Sleep(1000);
                Dispatcher.Invoke(new Action(() =>
                {
                    if (SiteManager.Instance.Sites.Count > 0 && SiteManager.Instance.Sites[MainWindow.MainW.comboBoxIndex].IsSupportTag)
                    {
                        if (Text.Trim().Length == 0 || Text == (string)Textbox.Tag) { return; }

                        if (currentSession != null)
                            currentSession.IsStop = true;

                        if (path.Visibility != Visibility.Visible)
                        {
                            path.Visibility = Visibility.Visible;

                            sb.Stop();
                            sb.Begin();
                        }
                        currentSession = new SessionState();

                        new Thread(new ParameterizedThreadStart((o) =>
                        {
                            try
                            {
                                string word = string.Empty;
                                Dispatcher.Invoke(new Action(() => { word = Text; }));
                                string[] parts = word.Split(' ');
                                if (parts != null && parts.Length > 0)
                                {
                                    //last word
                                    word = parts[parts.Length - 1];
                                }
                                word = Uri.EscapeDataString(word);

                                List<TagItem> tagList = SiteManager.Instance.Sites[MainWindow.MainW.comboBoxIndex].GetTags(word, MainWindow.WebProxy);
                                if (!(o as SessionState).IsStop)
                                {
                                    Dispatcher.Invoke(new UIdelegate((tagl) =>
                                    {
                                        txtSearch.Items.Clear();
                                        List<TagItem> tags = tagl as List<TagItem>;
                                        foreach (TagItem node in tags)
                                        {
                                            txtSearch.Items.Add(new SearchItem() { Name = node.Name, Count = node.Count });
                                        }
                                        ShowUsedItems();
                                    }), tagList);
                                }
                            }
                            catch (Exception) { }
                            finally
                            {
                                if (!(o as SessionState).IsStop)
                                {
                                    Dispatcher.Invoke(new VoidDel(delegate ()
                                    {
                                        path.Visibility = Visibility.Hidden;
                                        sb.Stop();
                                    }));
                                }
                            }
                        })).Start(currentSession);
                    }
                }));
            });
            WaitAutoComplete.Start();
        }

        internal class SearchItem
        {
            public SearchItem()
            {
                Enabled = true;
            }
            public string Name { get; set; }
            public bool Enabled
            {
                get;
                set;
            }
            public Brush Color
            {
                get
                {
                    if (Enabled)
                        return Brushes.Black;
                    else return Brushes.Gray;
                }
            }
            private string count;
            public string Count
            {
                get { return count; }
                set
                {
                    count = value;
                    if (count == null)
                        Visiable = Visibility.Hidden;
                    else Visiable = Visibility.Visible;
                }
            }

            public Visibility Visiable { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private void PART_EditableTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearch.IsDropDownOpen = true;
            TextBox tbox = (TextBox)sender;
            if (tbox.Text == (string)tbox.Tag)
            {
                tbox.Text = string.Empty;
                tbox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PART_EditableTextBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            TextBox tbox = (TextBox)sender;
            if (tbox.Text.Trim().Length == 0)
            {
                tbox.Text = (string)tbox.Tag;
                tbox.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
            lastSelectText = tbox.SelectedText;
        }

        /// <summary>
        /// 关键词下拉列表选中
        /// </summary>
        private void TxtSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSearch.SelectedIndex > -1)
            {
                string selectedText = txtSearch.Items[txtSearch.SelectedIndex].ToString();

                if (!string.IsNullOrWhiteSpace(Text))
                {
                    if (!string.IsNullOrEmpty(lastSelectText))
                    {
                        // 替换选择的关键词
                        Text = Text.Replace(lastSelectText, selectedText);
                        return;
                    }
                    else if (lastInputState < 1)
                    {
                        // 替换最后一个输入的关键词
                        lastInputState = 2;
                        string[] parts = Text.Split(' ');
                        string newWord = string.Empty;
                        foreach (string word in parts)
                        {
                            if (word == parts.Last())
                            {
                                newWord += selectedText;
                                Text = newWord;
                                lastInputState = 1;
                                return;
                            }
                            newWord += word + " ";
                        }
                    }
                    else if (Text.LastIndexOf(' ') == Text.Length - 1 || Text.LastIndexOf('"') == Text.Length - 1)
                    {
                        // 如果关键词最后为空格或双引号则追加
                        if (Text.LastIndexOf('"') == Text.Length - 1) { selectedText = " "; }
                        Text += selectedText;
                        return;
                    }
                }
                // 以上条件都为False则完全覆盖
                Text = selectedText;
            }
        }

        private void TxtSearch_DropDownOpened(object sender, EventArgs e)
        {
            txtSearch.Items.Clear();
            ShowUsedItems();
        }

        private void PART_EditableTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (MainWindow.IsShiftDown())
                {
                    usedItems.Clear();
                    txtSearch.IsDropDownOpen = false;
                    Text = string.Empty;
                }
                else if (usedItems.Contains(Text))
                {
                    usedItems.Remove(Text);
                    txtSearch.IsDropDownOpen = false;
                    Text = string.Empty;
                }
            }
            else if (e.Key == Key.Enter)
            {
                Enteded?.Invoke(this, null);
            }
        }

    }
}