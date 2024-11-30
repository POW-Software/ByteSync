using Avalonia;
using Avalonia.Controls;

namespace ByteSync.Views.Misc;

public static class ScrollViewerHelper
{
    public static void AutoSetPadding(this object? sender)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            bool isScrollBarVisible;
                
            if (scrollViewer.Viewport.Width < scrollViewer.Extent.Width)
            {
                isScrollBarVisible = true;
            }
            else // >=
            {
                isScrollBarVisible = false;
            }

            if (isScrollBarVisible)
            {
                if (scrollViewer.Padding.Bottom == 0)
                {
                    scrollViewer.Padding = new Thickness(0, 0, 0, 16);
                }
            }
            else
            {
                if (Math.Abs(scrollViewer.Padding.Bottom - 16) < 0.1)
                {
                    scrollViewer.Padding = new Thickness(0, 0, 0, 0);
                }
            }
        }
    }
}