using ReplayParser.ReplaySorter.Sorting.SortResult;
using System.Windows;
using System.Windows.Controls;

namespace ReplayParser.ReplaySorter.UI.Customization
{
    public class TreeViewItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DirectoryTemplate { get; set; }
        public DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null)
            {
                if (item is DirectoryFileTreeNode)
                {
                    var directoryFileTreeNode = item as DirectoryFileTreeNode;

                    if (directoryFileTreeNode.IsDirectory)
                        return DirectoryTemplate;

                    return FileTemplate;
                }
                else if (item is DirectoryFileTreeNodeSimple)
                {
                    var directoryFileTreeNode = item as DirectoryFileTreeNodeSimple;

                    if (directoryFileTreeNode.IsDirectory)
                        return DirectoryTemplate;

                    return FileTemplate;
                }
            }

            return null;
        }
    }
}
