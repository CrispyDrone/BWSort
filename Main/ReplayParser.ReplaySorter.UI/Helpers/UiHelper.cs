using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System;

namespace ReplayParser.ReplaySorter.UI.Helpers
{
    public static class UiHelper
    {
        public static T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            if (parent == null)
                return null;

            var elementsToSearch = new Queue<DependencyObject>();
            elementsToSearch.Enqueue(parent);

            while (elementsToSearch.Any())
            {
                var element = elementsToSearch.Dequeue();
                var numberOfChildren = VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < numberOfChildren; i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    var childAsT = child as T;
                    if (childAsT != null && predicate(childAsT))
                    {
                        return childAsT;
                    }
                    else
                    {
                        elementsToSearch.Enqueue(child);
                    }
                }
            }

            return null;
        }
    }
}
