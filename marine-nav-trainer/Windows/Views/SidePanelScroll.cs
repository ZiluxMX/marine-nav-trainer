using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace marine_nav_trainer.Windows.Views {
    // Sekcja wsparta przez SI
    // scroll panelu bocznego z ramkami
    public static class SidePanelScroll {
        public static void OnMouseWheel(ScrollViewer panel, MouseWheelEventArgs e) {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            bool isInPanel = false;

            while (source != null) {
                // CmbBox przejmuje priorytet scrolla
                if (source is Popup)
                    return;
                if (source == panel)
                    isInPanel = true;

                source = GetParent(source);
            }

            if (!isInPanel)
                return;

            panel.ScrollToVerticalOffset(panel.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // Run z podglądu MD nie ma rodzica wizualnego
        private static DependencyObject? GetParent(DependencyObject source) {
            DependencyObject? parent = null;
            if (source is Visual)
                parent = VisualTreeHelper.GetParent(source);
            if (parent == null)
                parent = LogicalTreeHelper.GetParent(source);

            return parent;
        }
    }
}
