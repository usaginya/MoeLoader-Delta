using System;
using System.Windows.Controls;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 常用转换器的静态引用
    /// 使用实例：Converter={x:Static local:XConverter.TrueToFalseConverter}
    /// </summary>
    public sealed class XConverter
    {
        public static BooleanToVisibilityConverter BooleanToVisibilityConverter => Singleton<BooleanToVisibilityConverter>.GetInstance();

        public static TrueToFalseConverter TrueToFalseConverter => Singleton<TrueToFalseConverter>.GetInstance();
        public static NullToCollapsedConverter NullToCollapsedConverter => Singleton<NullToCollapsedConverter>.GetInstance();
        public static ThicknessToDoubleConverter ThicknessToDoubleConverter => Singleton<ThicknessToDoubleConverter>.GetInstance();
        public static BackgroundToForegroundConverter BackgroundToForegroundConverter => Singleton<BackgroundToForegroundConverter>.GetInstance();
        public static TreeViewMarginConverter TreeViewMarginConverter => Singleton<TreeViewMarginConverter>.GetInstance();

        public static PercentToAngleConverter PercentToAngleConverter => Singleton<PercentToAngleConverter>.GetInstance();
    }
}
