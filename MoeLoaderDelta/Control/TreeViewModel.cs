using LZ4;
using MoeLoaderDelta.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static MoeLoaderDelta.Control.JsonStructure;

/// <summary>
/// by YIU
/// Last: 2020-7-29
/// </summary>
namespace MoeLoaderDelta.Control
{
    #region ########## TreeViewModel ##########
    public class TreeViewModel : NotifyPropertyBase
    {
        /// <summary>
        /// TreeNode执行方法
        /// </summary>
        private enum TreeExecution { DeleteSelected, DeleteChecked, HaveChecked, RefreshCheckBox, FindNodes }

        /// <summary>
        /// 内部主树
        /// </summary>
        private static ObservableCollection<TreeNode> treeNodes = new ObservableCollection<TreeNode>();
        /// <summary>
        /// 主树
        /// </summary>
        public ObservableCollection<TreeNode> TreeNodes
        {
            get => treeNodes;
            set
            {
                treeNodes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 当前选中的节点
        /// </summary>
        public static TreeNode SelectedNode { get; set; }

        #region +++++++ TreeNode构造函数 +++++++
        /// <summary>
        ///  TreeNode构造函数
        /// </summary>
        public TreeViewModel() { }
        #endregion +++++++++++++++++

        #region +++++++ 列表管理方法 +++++++
        /// <summary>
        /// 载入标签收藏文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="unLZ4">是否LZ4解压</param>
        /// <param name="imports">导入个数</param>
        /// <param name="repetitions">忽略个数</param>
        /// <param name="failures">失败个数</param>
        public static string LoadFavoriteFile(string filePath, bool unLZ4, out int imports, out int repetitions, out int failures)
        {
            imports = repetitions = failures = 0;
            if (!File.Exists(filePath)) { return "找不到标签收藏文件"; }
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) { return "标签收藏文件内容是空的"; }
            if (unLZ4) { json = Encoding.UTF8.GetString(LZ4Codec.Unwrap(Convert.FromBase64String(json))); }
            return ImportJsonData(json, MainWindow.MainW.AllSitesName(), out imports, out repetitions, out failures);
        }
        /// <summary>
        /// 载入标签收藏文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="unLZ4">是否LZ4解压</param>
        public static string LoadFavoriteFile(string filePath, bool unLZ4 = true)
        {
            return LoadFavoriteFile(filePath, unLZ4, out int i, out int r, out int f);
        }

        /// <summary>
        /// 保存标签收藏文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="lz4">是否LZ4压缩</param>
        public static string SaveFavoriteFile(string filePath, bool lz4 = true)
        {
            if (string.IsNullOrWhiteSpace(filePath)) { return "保存标签文件路径是空的"; }
            string json = ExportJsonData(!lz4);
            if (lz4) { json = Convert.ToBase64String(LZ4Codec.Wrap(Encoding.UTF8.GetBytes(json))); }
            File.WriteAllText(filePath, json);
            return string.Empty;
        }

        /// <summary>
        /// 获取收藏夹站点下目录名列表
        /// </summary>
        public static List<string> GetSiteDirNameList(string siteName)
        {
            List<string> dirNameList = new List<string>();
            if (string.IsNullOrWhiteSpace(siteName) || !treeNodes.Any(n => n.Name == siteName)) { return dirNameList; }
            treeNodes.Where(n => n.Name == siteName).FindFirst().Children.Where(d => d.Type == TreeNode.NodeType.Dir).ForEach(m => dirNameList.Add(m.Name));
            return dirNameList;
        }

        /// <summary>
        /// 导出Json数据
        /// </summary>
        /// <param name="format">是否返回格式化的Json</param>
        public static string ExportJsonData(bool format = false)
        {
            //生成站点列表对象
            Dictionary<string, List<KeywordItem>> siteItems = new Dictionary<string, List<KeywordItem>>();
            treeNodes.ForEach(s =>
            {
                List<KeywordItem> keysItems = new List<KeywordItem>();

                s.Children.ForEach(n =>
                {
                    if (n.Type == TreeNode.NodeType.Dir)
                    {
                        n.Children.ForEach(m =>
                        {
                            keysItems.Add(new KeywordItem()
                            {
                                Dir = n.Name,
                                Name = m.Name,
                                Mark = m.Mark,
                                Expand = n.IsExpand ? 1 : 0
                            });
                        });
                    }
                    else if (n.Type == TreeNode.NodeType.Keyword)
                    {
                        keysItems.Add(new KeywordItem()
                        {
                            Dir = n.Parent.Name,
                            Name = n.Name,
                            Mark = n.Mark,
                            Expand = n.Parent.IsExpand ? 1 : 0
                        });
                    }
                });
                siteItems.Add(s.Name, keysItems);
            });
            JsonRoot.Sites = siteItems;

            return JsonConvert.SerializeObject(JsonRoot, format ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// 导入Json数据、发生异常时返回异常信息
        /// </summary>
        /// <param name="json">内容</param>
        /// <param name="sitesName">站点列表名、忽略不含有的站点</param>
        /// <param name="imports">成功导入个数</param>
        /// <param name="repetitions">重复标签个数</param>
        /// <param name="failures">导入失败个数</param>
        public static string ImportJsonData(string json, List<string> sitesName, out int imports, out int repetitions, out int failures)
        {
            imports = repetitions = failures = 0;
            string exceptionInfo = string.Empty;
            try
            {
                Root root = JsonConvert.DeserializeObject<Root>(json);
                if (root.Flag != JsonFlag) { return "导入的文件格式不支持"; }

                //根据站点列表添加站点标签和目录
                if (!root.Sites.Any()) { return "站点是空的"; }

                foreach (string site in sitesName)
                {
                    if (!root.Sites.ContainsKey(site)) { continue; }
                    List<KeywordItem> keywords = root.Sites[site];

                    foreach (KeywordItem keyword in keywords)
                    {
                        string dir = keyword.Dir,
                            name = string.IsNullOrWhiteSpace(keyword.Mark) ? keyword.Name : keyword.Mark,
                            mark = string.IsNullOrWhiteSpace(keyword.Mark) ? keyword.Mark : keyword.Name;
                        bool expand = keyword.Expand > 0;

                        if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(name))
                        { failures++; }
                        else
                        {
                            int result = AddOrEdit(site, null, dir, name, mark, true, expand);
                            if (result == 1) { imports++; }
                            else if (result == 2) { repetitions++; }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                exceptionInfo = e.AllMessage();
            }
            return exceptionInfo;
        }

        /// <summary>
        /// 新建目录、参数为空或目录名重复返回False
        /// </summary>
        /// <param name="siteName">站点名主节点</param>
        /// <param name="dirName">目录名</param>
        private static bool CreateDir(string siteName, string dirName)
        {
            if (string.IsNullOrWhiteSpace(siteName)
                || string.IsNullOrWhiteSpace(dirName)
                || !treeNodes.Any(n => n.Name == siteName)
                || treeNodes.Where(n => n.Name == siteName).FindFirst().Children.Any(m => m.Name == dirName))
            { return false; }

            TreeNode siteNode = treeNodes.Where(n => n.Name == siteName).FindFirst();
            TreeNode dirNode = new TreeNode(dirName, string.Empty, null, TreeNode.NodeType.Dir);

            siteNode.AddChildren(dirNode);
            siteNode.SetProperty(x => x.Children);
            return true;
        }

        /// <summary>
        /// 添加站点节点
        /// </summary>
        public static void AddSites(string siteName, BitmapImage icon = null)
        {
            if (treeNodes.Any(s => s.Name == siteName)) { return; }
            TreeNode treeNode = new TreeNode(siteName, string.Empty, icon, TreeNode.NodeType.Site);
            treeNodes.Add(treeNode);
        }

        /// <summary>
        /// 隐藏空站点
        /// </summary>
        public static void HideEmptySite()
        {
            treeNodes.Where(s => !s.Children.Any()).ForEach(s => s.IsVisibility = Visibility.Collapsed);
        }

        /// <summary>
        /// 添加或编辑收藏夹列表项。顽固地独立工作
        /// </summary>
        /// <param name="siteName">站点名</param>
        /// <param name="oldDir">标签原目录</param>
        /// <param name="newDir">标签新目录</param>
        /// <param name="originalName">原标签/目录名</param>
        /// <param name="markName">标签备注/新目录名</param>
        /// <param name="ignoreRepetition">忽略重名标签</param>
        /// <param name="expandDir">更改后是否展开目录</param>
        /// <param name="dirRename">是否为目录重命名</param>
        /// <returns>0无更改 1已更改 2已忽略 3目标目录已存在重名标签</returns>
        public static int AddOrEdit(string siteName, string oldDir, string newDir, string originalName, string markName = null,
            bool ignoreRepetition = false, bool expandDir = false, bool dirRename = false)
        {
            const int nothing = 0, changed = 1, ignore = 2, repeat = 3;

            if (string.IsNullOrWhiteSpace(siteName)
                || string.IsNullOrWhiteSpace(originalName)
                 || !treeNodes.Any(n => n.Name == siteName))
            { return nothing; }

            #region - 添加/编辑 标签 -
            TreeNode oldDirNode = treeNodes.Where(n => n.Name == siteName).FindFirst();
            TreeNode newDirNode = oldDirNode;

            TreeNode oldKeyNode = new TreeNode();
            TreeNode newKeyNode = new TreeNode()
            {
                Name = string.IsNullOrWhiteSpace(markName) ? originalName : markName,
                Mark = string.IsNullOrWhiteSpace(markName) ? string.Empty : originalName
            };

            bool addMode = string.IsNullOrWhiteSpace(oldDir);
            //定位原目录位置
            if (!addMode)
            {
                if (oldDir != siteName && oldDirNode.Children != null)
                {
                    oldDirNode = oldDirNode.Children.Where(n => n.Name == oldDir).FindFirst();
                }
                else if (oldDir != siteName && !oldDirNode.Children.Any(n => n.Name == oldDir))
                {
                    //不存在目录则创建
                    if (!CreateDir(siteName, oldDir)) { return nothing; }
                    oldDirNode = oldDirNode.Children.Where(n => n.Name == oldDir).FindFirst();
                }
            }

            //定位新目录位置
            if (newDir != null && newDir != siteName)
            {
                //不存在目录则创建
                if (!newDirNode.Children.Any(n => n.Name == newDir))
                {
                    if (!CreateDir(siteName, newDir)) { return nothing; }
                }
                newDirNode = newDirNode.Children.Where(n => n.Name == newDir).FindFirst();
            }

            //定位标签位置或新建标签
            if (!addMode && oldDirNode.Children != null)
            {
                oldKeyNode = oldDirNode.Children.Where(n => n.Name == originalName).FindFirst();
                oldKeyNode = oldKeyNode ?? oldDirNode.Children.Where(n => n.Mark == originalName).FindFirst();
            }
            else
            {
                //目标目录含有重复标签
                string newKeyName = string.IsNullOrWhiteSpace(newKeyNode.Mark) ? newKeyNode.Name : newKeyNode.Mark;
                if (newDirNode.Children.Any(n => string.IsNullOrWhiteSpace(n.Mark) ? n.Name == newKeyName : n.Mark == newKeyName))
                {
                    return ignoreRepetition ? ignore : repeat;
                }

                //新建标签
                newDirNode.AddChildren(new TreeNode(newKeyNode.Name, newKeyNode.Mark, null, TreeNode.NodeType.Keyword));
                newDirNode.SetProperty(x => x.Children);
                UpdateNodeAttr(newDirNode, expandDir);
                return changed;
            }

            #region - 移动编辑标签 -
            if (oldDir != newDir && oldKeyNode != null)
            {
                //目标目录含有重复标签
                string oldKeyName = string.IsNullOrWhiteSpace(oldKeyNode.Mark) ? oldKeyNode.Name : oldKeyNode.Mark;
                if (newDirNode.Children.Any(n => string.IsNullOrWhiteSpace(n.Mark) ? n.Name == oldKeyName : n.Mark == oldKeyName)) { return repeat; }

                //移动标签到新目录后删除原目录标签
                newDirNode.AddChildren(oldKeyNode);
                oldDirNode.Children.Remove(oldKeyNode);
                oldDirNode.SetProperty(x => x.Children);
                newDirNode.SetProperty(x => x.Children);

                UpdateNodeAttr(newDirNode, expandDir);
            }
            #endregion

            //重命名目录或更改标签
            if (!addMode && oldKeyNode != null)
            {
                if (dirRename)
                {
                    oldKeyNode.Name = markName;
                }
                else
                {
                    oldKeyNode.Name = newKeyNode.Name;
                    oldKeyNode.Mark = newKeyNode.Mark;
                }
                oldKeyNode.SetProperty(x => x.Name);
                oldKeyNode.SetProperty(x => x.Mark);

                //判断标签在原目录还是新目录 刷新目录节点
                if (oldDir == newDir)
                {
                    oldDirNode.SetProperty(x => x.Children);
                }
                else
                {
                    newDirNode.SetProperty(x => x.Children);
                }
            }
            #endregion

            return changed;

            #region = 更新节点属性 =
            void UpdateNodeAttr(TreeNode treeNode, bool expand)
            {
                if (expand && !treeNode.IsExpand)
                {
                    treeNode.IsExpand = expand;
                    treeNode.SetProperty(x => x.IsExpand);
                }
                //设置根目录属性
                treeNode = treeNode.GetRoot();
                if (treeNode != null)
                {
                    if (treeNode.IsVisibility != Visibility.Visible)
                    {
                        treeNode.IsVisibility = Visibility.Visible;
                        treeNode.SetProperty(x => x.IsVisibility);
                    }
                    if (expand)
                    {
                        treeNode.IsExpand = expand;
                        treeNode.SetProperty(x => x.IsExpand);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 列出符合查找条件的标签节点
        /// </summary>
        /// <param name="siteName">要查找的站点名</param>
        /// <param name="findKeywords">要查找的标签 用空格分割为多个标签</param>
        public static void FindNodes(string siteName, string findKeywords)
        {
            if (!treeNodes.Any()) { return; }
            //以下判断寻找标签为空就恢复树显示、否则寻找对应站点下的标签
            treeNodes.Where(n => n.IsVisibility == Visibility.Collapsed).ForEach(n => n.IsVisibility = Visibility.Visible);
            if (string.IsNullOrWhiteSpace(findKeywords) || string.IsNullOrWhiteSpace(siteName))
            {
                TreeNodeExecute(null, TreeExecution.FindNodes, findKeywords);
            }
            else
            {
                treeNodes.Where(n => n.Name != siteName).ForEach(m => m.IsVisibility = Visibility.Collapsed);
                TreeNodeExecute(treeNodes.Where(n => n.IsVisibility == Visibility.Visible).First(), TreeExecution.FindNodes, findKeywords);
            }
            HideEmptySite();
        }

        /// <summary>
        /// 是否有勾选的节点
        /// </summary>
        public static bool GetHaveChecked()
        {
            bool ret = false;
            if (FavoriteWnd.IsMultiSelect)
            {
                ret = TreeNodeExecute(null, TreeExecution.HaveChecked);
            }
            return ret;
        }

        /// <summary>
        /// 刷新多选框显示和列表提示
        /// </summary>
        public static void RefreshCheckBoxVisibility()
        {
            TreeNodeExecute(null, TreeExecution.RefreshCheckBox);
        }

        /// <summary>
        /// 清除所有勾选
        /// </summary>
        public static void ClearChecked()
        {
            treeNodes.ForEach(n => n.IsChecked = false);
        }

        /// <summary>
        /// 移除选中的节点
        /// </summary>
        public static void RemoveSelectedNode()
        {
            TreeNodeExecute(null, TreeExecution.DeleteSelected);
            HideEmptySite();
        }

        /// <summary>
        /// 移除勾选的节点
        /// </summary>
        public static void RemoveCheckedNode()
        {
            TreeNodeExecute(null, TreeExecution.DeleteChecked);
            ClearChecked();
            HideEmptySite();
        }

        /// <summary>
        /// 执行节点操作
        /// </summary>
        /// <param name="childrenNode">子节点, NULL时以TreeNodes为操作源</param>
        /// <param name="exeMethod">执行方法</param>
        /// <param name="jsonDat">处理返回转换的Json格式数据</param>
        /// <param name="findKeywords">要查找的标签 用空格分割为多个标签</param>
        /// <returns>执行结果, 只有单独操作节点的执行方法才可能返回True</returns>
        private static bool TreeNodeExecute(TreeNode childrenNode, TreeExecution exeMethod, string findKeywords = null)
        {
            bool ret = false;
            ObservableCollection<TreeNode> exeNodes = childrenNode == null ? treeNodes : childrenNode.Children;
            ObservableCollection<TreeNode> newNodes = new ObservableCollection<TreeNode>();

            #region ====== 节点执行======
            switch (exeMethod)
            {
                //====== 提取查找节点
                case TreeExecution.FindNodes:
                    //清空勾选
                    ClearChecked();
                    //恢复全部显示
                    exeNodes.Where(n => n.IsVisibility == Visibility.Collapsed).ForEach(n => n.IsVisibility = Visibility.Visible);

                    //筛选标签节点
                    findKeywords = findKeywords ?? string.Empty;
                    string[] keywords = findKeywords.Split(' ');
                    if (childrenNode != null && exeNodes.Any())
                    {
                        exeNodes.Where(n => !n.Children.Any()).ForEach(n =>
                          {
                              n.IsVisibility = string.IsNullOrWhiteSpace(findKeywords)
                              ? Visibility.Visible
                              : string.IsNullOrWhiteSpace(keywords.FirstOrDefault(k =>
                              {
                                  return n.Name.Contains(k, StringComparison.OrdinalIgnoreCase) || n.Mark.Contains(k, StringComparison.OrdinalIgnoreCase);
                              }))
                              ? Visibility.Collapsed
                              : Visibility.Visible;
                          });
                        newNodes = new ObservableCollection<TreeNode>(exeNodes);
                        childrenNode.Children = newNodes;
                        //遍历父节点
                        FindParentAllCollapsed(childrenNode, keywords, exeMethod);
                    }
                    break;

                //====== 删除选中的子节点
                case TreeExecution.DeleteSelected:
                    if (childrenNode != null)
                    {
                        ret = exeNodes.Remove(exeNodes.FirstOrDefault(n => n.IsSelected == true));
                        if (ret) { childrenNode.SetProperty(x => x.Children); }
                        //更新查找结果父节点
                        FindParentAllCollapsed(childrenNode, exeMethod);
                    }
                    break;

                //====== 删除勾选的子节点
                case TreeExecution.DeleteChecked:
                    if (childrenNode != null && exeNodes.Any(n => n.IsChecked == true))
                    {
                        newNodes = new ObservableCollection<TreeNode>(exeNodes.Where(n => n.IsChecked != true));
                        childrenNode.Children = newNodes;
                        childrenNode.SetProperty(x => x.Children);
                        FindParentAllCollapsed(childrenNode, exeMethod);
                    }
                    break;

                //====== 检查是否有勾选的节点
                case TreeExecution.HaveChecked:
                    ret = exeNodes.Any(n => n.IsChecked != false);
                    break;

                //====== 刷新节点提示和勾选框显示
                case TreeExecution.RefreshCheckBox:
                    exeNodes.ForEach(n =>
                    {
                        n.SetProperty(x => x.ToolTipMain);
                        n.SetProperty(x => x.ToolTipSub);
                        n.SetProperty(x => x.CheckBoxVisibility);
                    });
                    break;

                default:
                    return ret;
            }
            #endregion =========================


            #region ===== 嵌套子节点执行 ====
            foreach (TreeNode cNode in exeNodes)
            {
                //执行为True时立刻返回结束遍历
                if (ret)
                {
                    return ret;
                }
                else if ((!ret && cNode.Children.Any()) || (!ret && exeMethod == TreeExecution.FindNodes))
                {
                    ret = TreeNodeExecute(cNode, exeMethod, findKeywords);
                }
            }
            #endregion ===============
            return ret;
        }

        /// <summary>
        /// 遍历更改父节点显示状态
        /// </summary>
        private static void FindParentAllCollapsed(TreeNode childrenNode, TreeExecution execution)
        {
            string[] vs = { "$" };
            FindParentAllCollapsed(childrenNode, vs, execution);
        }
        /// <summary>
        /// 遍历更改父节点显示状态
        /// </summary>
        private static void FindParentAllCollapsed(TreeNode childrenNode, string[] keywords, TreeExecution execution)
        {
            if (childrenNode.Children.Any())
            {
                if (!keywords.Any() || (keywords.Length == 1 && string.IsNullOrWhiteSpace(keywords[0])))
                {
                    childrenNode.IsVisibility = Visibility.Visible;
                }
                else if (childrenNode.Children.All(n => n.IsVisibility == Visibility.Collapsed))
                {
                    childrenNode.IsVisibility = Visibility.Collapsed;
                }
                else if (childrenNode.Children.Any(n => n.IsSelected || n.IsChecked == true) || (execution == TreeExecution.FindNodes))
                {
                    childrenNode.IsExpand = true;
                    childrenNode.SetProperty(x => x.IsExpand);
                }
                //更改目录节点显示
                if (execution == TreeExecution.FindNodes)
                {
                    childrenNode.Children.Where(d => d.Type == TreeNode.NodeType.Dir).ForEach(d =>
                    {
                        d.IsVisibility = d.Children.All(n => n.IsVisibility == Visibility.Collapsed) ? Visibility.Collapsed : Visibility.Visible;
                    });
                }
                childrenNode.SetProperty(x => x.IsVisibility);
            }

            if (childrenNode.Parent != null) { FindParentAllCollapsed(childrenNode.Parent, keywords, execution); }
        }
        #endregion +++++++++++++++++
    }
    #endregion ####################

    #region ########## TreeNode ##########
    public class TreeNode : NotifyPropertyBase
    {
        /// <summary>
        /// 节点类型组
        /// </summary>
        public enum NodeType { Site, Dir, Keyword }

        /// <summary>
        /// 默认图标
        /// </summary>
        private readonly BitmapImage DefaultIcon = new BitmapImage(new Uri("pack://application:,,,/Images/favorites.png"));

        /// <summary>
        /// 父节点
        /// </summary>
        public TreeNode Parent { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public ObservableCollection<TreeNode> Children { get; set; }

        /// <summary>
        /// 节点主标题
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点子标记 用于注释
        /// </summary>
        public string Mark { get; set; } = string.Empty;

        /// <summary>
        /// 节点是否可见 默认可见 用于查找时隐藏目标节点以外的节点
        /// </summary>
        public Visibility IsVisibility
        {
            get => isVisibility;
            set
            {
                isVisibility = value;
                this.SetProperty(x => x.IsVisibility);
            }
        }
        private Visibility isVisibility = Visibility.Visible;

        #region - ToolTip -
        /// <summary>
        /// 提示主内容
        /// </summary>
        public string ToolTipMain
        {
            get
            {
                if (Type == NodeType.Dir || (Parent != null && Children.Any()))
                {
                    return Name;
                }
                else if (Type != NodeType.Site || Parent != null)
                {
                    return string.IsNullOrWhiteSpace(Mark) ? Name : Name + Environment.NewLine + Mark;
                }
                return null;
            }
        }
        /// <summary>
        /// 提示子内容
        /// </summary>
        public string ToolTipSub
        {
            get
            {
                const string rightCheckTip = "右击编辑";
                if (Type == NodeType.Site || Parent == null)
                {
                    return "右击收起下面所有目录";
                }
                else if (Type == NodeType.Dir || (Parent != null && Children.Any()))
                {
                    return rightCheckTip;
                }
                else
                {
                    return (FavoriteWnd.IsMultiSelect ? "双击勾选" : "双击添加到搜索框") + Environment.NewLine + rightCheckTip;
                }
            }
        }
        #endregion

        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpand
        {
            get => isExpand;
            set
            {
                if (Type == NodeType.Dir)
                {
                    BitmapImage bitmap = new BitmapImage(new Uri($"pack://application:,,,/Images/{(value ? "open" : "close")}dir.png"));
                    Icon = bitmap;
                    this.SetProperty(x => x.Icon);
                }
                isExpand = value;
            }
        }
        private bool isExpand;

        /// <summary>
        /// 节点图标
        /// </summary>
        private BitmapImage icon;
        public BitmapImage Icon
        {
            get => icon.UriSource == null && icon.StreamSource == null
                ? DefaultIcon
                : icon;
            set => icon = value ?? DefaultIcon;
        }

        /// <summary>
        /// 根据多选状态决定是否显示CheckBox
        /// </summary>
        public Visibility CheckBoxVisibility => FavoriteWnd.IsMultiSelect ? Visibility.Visible : Visibility.Collapsed;

        #region ++ TreeNode构造函数 ++
        /// <summary>
        /// 节点构造 默认
        /// </summary>
        public TreeNode() { }

        /// <summary>
        /// 节点构造 参数
        /// </summary>
        /// <param name="name">主标题</param>
        /// <param name="mark">子标题</param>
        /// <param name="icon">图标</param>
        /// <param name="type">类型</param>
        /// <param name="expand">是否展开</param>
        public TreeNode(string name, string mark = null, BitmapImage icon = null, NodeType type = NodeType.Keyword, bool expand = false)
        {
            Name = name;
            Mark = mark;
            Icon = icon;
            Type = type;
            IsExpand = expand;
            Children = new ObservableCollection<TreeNode>();
        }
        #endregion +++++++

        #region ++ IsChecked勾选属性 ++
        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set => SetIsChecked(value, true, true);
        }

        /// <summary>
        /// 设节点勾选遍历状态
        /// </summary>
        /// <param name="value"></param>
        /// <param name="checkedChildren"></param>
        /// <param name="checkedParent"></param>
        private void SetIsChecked(bool? value, bool checkedChildren, bool checkedParent)
        {
            if (_isChecked == value) { return; }
            _isChecked = value;

            //选中和取消子类
            if (checkedChildren && value.HasValue && Children != null)
            {
                Children.ForEach(n =>
                {
                    if (n.IsVisibility != Visibility.Collapsed || n.IsChecked == null)
                    {
                        n.SetIsChecked(value, true, false);
                    }
                });
                //查找情况下的选择
                if (_isChecked == true)
                {
                    if (Children.Any(n => n.IsVisibility != Visibility.Visible) || Children.Any(n => n.IsChecked != true))
                    {
                        _isChecked = null;
                    }
                }
            }

            //选中和取消父类
            if (checkedParent && Parent != null)
            {
                Parent.CheckParentCheckState();
            }

            //更改通知
            this.SetProperty(x => x.IsChecked);
        }

        /// <summary>
        /// 检查父类是否勾选
        /// 如果子类中有一个和第一个子类的状态不一样父类isChecked为null
        /// </summary>
        private void CheckParentCheckState()
        {
            bool? _firstState = null;

            Children.ForEach(n =>
            {
                bool? childrenState = n.IsChecked;
                if (Children.First().Equals(n))
                {
                    _firstState = childrenState;
                }
                else if (_firstState != childrenState)
                {
                    _firstState = null;
                }
            });


            bool? _currentState = IsChecked;
            if (_firstState != null) { _currentState = _firstState; }
            SetIsChecked(_firstState, false, true);
        }
        #endregion +++++++

        #region ++ IsSelected选择属性 ++
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                this.SetProperty(x => x.IsSelected);
                TreeViewModel.SelectedNode = this;
            }
        }
        #endregion +++++++

        #region +++ 节点操作 +++
        /// <summary>
        /// 在节点下添加子节点
        /// </summary>
        /// <param name="children">子节点</param>
        /// <param name="isChecked">是否勾选</param>
        public void AddChildren(TreeNode children, bool? isChecked = false)
        {
            Children.Add(children);

            children.Parent = this;
            children.IsChecked = isChecked;
        }

        /// <summary>
        /// 取根节点
        /// </summary>
        /// <returns></returns>
        public TreeNode GetRoot(TreeNode children = null)
        {
            TreeNode node = children ?? this;
            if (node.Parent != null) { node = GetRoot(node.Parent); }
            return node;
        }
        #endregion +++++++

    }

    #endregion ####################

    #region ########## NotifyProperty : INotifyPropertyChanged ##########
    /// <summary>
    /// 属性更改通知
    /// </summary>
    public class NotifyPropertyBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// 属性更改通知扩展
    /// </summary>
    public static class NotifyPropertyBaseEx
    {
        public static void SetProperty<T, E>(this T t, Expression<Func<T, E>> expre) where T : NotifyPropertyBase, new()
        {
            string pName = string.Empty;

            if (expre.Body is MemberExpression)
            {
                pName = (expre.Body as MemberExpression).Member.Name;
            }
            else if (expre.Body is UnaryExpression)
            {
                pName = ((expre.Body as UnaryExpression).Operand as MemberExpression).Member.Name;
            }

            t.OnPropertyChanged(pName);
        }
    }
    #endregion ####################

    #region ########### TreeViewDicEvent ############
    public partial class TreeViewDicEvent : ResourceDictionary
    {
        /// <summary>
        /// 节点项双击事件
        /// </summary>
        private void EvItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                TreeNode node = (TreeNode)((TreeViewItem)sender).Header;

                if (node.Parent != null && node.IsSelected && node.Type == TreeNode.NodeType.Keyword)
                {
                    if (FavoriteWnd.IsMultiSelect)
                    {
                        node.IsChecked = !node.IsChecked;
                    }
                    else
                    {
                        string addWork = $"{(string.IsNullOrWhiteSpace(node.Mark) ? node.Name : node.Mark)}";
                        MainWindow.MainW.searchControl.Text += string.IsNullOrWhiteSpace(MainWindow.MainW.searchControl.Text) ? addWork : " " + addWork;
                        MainWindow.MainW.Control_Toast.Show("已添加标签到搜索框");
                    }
                }
            }
        }

        /// <summary>
        /// 节点右击事件
        /// </summary>
        private void EvItemPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject VisualUpwardSearch<T>(DependencyObject source)
            {
                while (source != null && source.GetType() != typeof(T))
                {
                    source = VisualTreeHelper.GetParent(source);
                }
                return source;
            }

            if (VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) is TreeViewItem treeViewItem)
            {
                treeViewItem.Focus();
                TreeNode node = (TreeNode)treeViewItem.DataContext;

                switch (node.Type)
                {
                    case TreeNode.NodeType.Site:
                        //收起主目录下的子目录
                        node.Children.Where(n => n.Type == TreeNode.NodeType.Dir && n.IsExpand).ForEach(m =>
                        {
                            m.IsExpand = false;
                            m.SetProperty(x => x.IsExpand);
                        });
                        break;

                    case TreeNode.NodeType.Dir:
                    case TreeNode.NodeType.Keyword:
                        //编辑目录名或标签备注
                        MainWindow.MainW.favoriteAddWnd = new FavoriteAddWnd(node.Name,
                            node.Type == TreeNode.NodeType.Dir ? FavoriteAddWnd.AddMode.EditDir : FavoriteAddWnd.AddMode.EditKeyword,
                            node.Mark, node.Parent.Name, node.GetRoot().Name, FavoriteWnd.ThisWnd);
                        MainWindow.MainW.favoriteAddWnd.ShowDialog();
                        break;
                }

                e.Handled = true;
            }
        }

    }
    #endregion ####################

    #region  ###########  JsonStructure ############
    public class JsonStructure
    {
        /// <summary>
        /// 公用根节点对象
        /// </summary>
        public static Root JsonRoot { get; set; } = new Root();

        /// <summary>
        /// Json文件标识
        /// </summary>
        public const string JsonFlag = "MLD@FTL";

        #region = Class definition =
        /// <summary>
        /// 根对象
        /// </summary>
        public class Root
        {
            /// <summary>
            /// Json文件标识
            /// </summary>
            public string Flag { get; set; } = JsonFlag;
            /// <summary>
            /// 站点对象组
            /// </summary>
            public Dictionary<string, List<KeywordItem>> Sites { get; set; } = new Dictionary<string, List<KeywordItem>>();
        }

        /// <summary>
        /// 收藏关键词对象
        /// </summary>
        public class KeywordItem
        {
            /// <summary>
            /// 所在收藏目录
            /// </summary>
            public string Dir { get; set; }
            /// <summary>
            /// 原名
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 备注
            /// </summary>
            public string Mark { get; set; }
            /// <summary>
            /// 目录 0不展开 1展开
            /// </summary>
            public int Expand { get; set; }
        }
        #endregion
    }
    #endregion
}
