using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ReplayParser.ReplaySorter.UI.Sorting
{
    public class SortAdorner : Adorner
    {
        private static Geometry _arrowAscending = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");
        private static Geometry _arrowDescending = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public SortAdorner(UIElement adornedElement, ListSortDirection direction) : base(adornedElement)
        {
            Direction = direction;
        }

        public ListSortDirection Direction { get; private set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            var translation = new TranslateTransform(AdornedElement.RenderSize.Width - 15, (AdornedElement.RenderSize.Height - 5) / 2);

            drawingContext.PushTransform(translation);

            var geometry = _arrowAscending;
            if (Direction == ListSortDirection.Descending)
                geometry = _arrowDescending;

            drawingContext.DrawGeometry(Brushes.Black, null, geometry);
            drawingContext.Pop();
        }

    }
}
