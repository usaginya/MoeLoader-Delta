using System;
using System.Windows.Controls;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 常用轉換器的靜態引用
    /// 使用實例：Converter={x:Static local:XConverter.TrueToFalseConverter}
    /// </summary>
    public sealed class XConverter
    {
        public static BooleanToVisibilityConverter BooleanToVisibilityConverter
        {
            get { return Singleton<BooleanToVisibilityConverter>.GetInstance(); }
        }

        public static TrueToFalseConverter TrueToFalseConverter
        {
            get { return Singleton<TrueToFalseConverter>.GetInstance(); }
        }

        public static ThicknessToDoubleConverter ThicknessToDoubleConverter
        {
            get { return Singleton<ThicknessToDoubleConverter>.GetInstance(); }
        }
        public static BackgroundToForegroundConverter BackgroundToForegroundConverter
        {
            get { return Singleton<BackgroundToForegroundConverter>.GetInstance(); }
        }
        public static TreeViewMarginConverter TreeViewMarginConverter
        {
            get { return Singleton<TreeViewMarginConverter>.GetInstance(); }
        }

        public static PercentToAngleConverter PercentToAngleConverter
        {
            get { return Singleton<PercentToAngleConverter>.GetInstance(); }
        }
    }
}
