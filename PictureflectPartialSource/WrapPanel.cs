//Based originally on the version in the Windows Community Toolkit, which uses the MIT license. However, it has been heavily modified and shares little code, so acknowledgement probably isn't required.

using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PictureflectPartialSource {

    public class WrapPanel : Panel {

        public double HorizontalSpacing {
            get { return (double)GetValue(HorizontalSpacingProperty); }
            set { SetValue(HorizontalSpacingProperty, value); }
        }
        public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(WrapPanel), new PropertyMetadata(0d, LayoutPropertyChanged));

        public double VerticalSpacing {
            get { return (double)GetValue(VerticalSpacingProperty); }
            set { SetValue(VerticalSpacingProperty, value); }
        }
        public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(WrapPanel), new PropertyMetadata(0d, LayoutPropertyChanged));

        public Orientation Orientation {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(WrapPanel), new PropertyMetadata(Orientation.Horizontal, LayoutPropertyChanged));

        public Thickness Padding {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(WrapPanel), new PropertyMetadata(default(Thickness), LayoutPropertyChanged));

        public HorizontalAlignment HorizontalContentAlignment {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment), typeof(WrapPanel), new PropertyMetadata(HorizontalAlignment.Left, LayoutPropertyChanged));

        public VerticalAlignment VerticalContentAlignment {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty VerticalContentAlignmentProperty = DependencyProperty.Register(nameof(VerticalContentAlignment), typeof(VerticalAlignment), typeof(WrapPanel), new PropertyMetadata(VerticalAlignment.Top, LayoutPropertyChanged));

        private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is WrapPanel wp && !Equals(e.NewValue, e.OldValue)) {
                wp.InvalidateMeasure();
                wp.InvalidateArrange();
            }
        }

        private List<int> measuredRowOrColumnCounts = new List<int>();

        protected override Size MeasureOverride(Size availableSize) {
            var childAvailableSize = new Size(Math.Max(availableSize.Width - Padding.Left - Padding.Right, 0), Math.Max(availableSize.Height - Padding.Top - Padding.Bottom, 0));
            foreach (var child in Children) {
                child.Measure(childAvailableSize);
            }
            var totalSize = ComputeTotalSize(new UvMeasure(Orientation, childAvailableSize.Width, childAvailableSize.Height), false);
            measuredRowOrColumnCounts = totalSize.RowOrColumnCounts;
            var result = new Size(totalSize.Size.X + Padding.Left + Padding.Right, totalSize.Size.Y + Padding.Top + Padding.Bottom);
            return result;
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var parentMeasure = new UvMeasure(Orientation, finalSize.Width, finalSize.Height);
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var paddingEnd = new UvMeasure(Orientation, Padding.Right, Padding.Bottom);
            UvMeasure availableSize = parentMeasure;
            availableSize.U -= (paddingStart.U + paddingEnd.U);
            availableSize.V -= (paddingStart.V + paddingEnd.V);
            var position = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var totalSize = ComputeTotalSize(availableSize, true);
            double desiredV = new UvMeasure(Orientation, DesiredSize.Width, DesiredSize.Height).V;
            if (Orientation == Orientation.Horizontal) {
                if (VerticalContentAlignment == VerticalAlignment.Center || (VerticalContentAlignment == VerticalAlignment.Stretch && totalSize.VisibleCount <= 1)) {
                    position.V = paddingStart.V + (parentMeasure.V - paddingStart.V - paddingEnd.V - totalSize.Size.V) / 2.0;
                } else if (VerticalContentAlignment == VerticalAlignment.Bottom) {
                    position.V = paddingStart.V + (parentMeasure.V - paddingStart.V - paddingEnd.V - totalSize.Size.V);
                } else if (VerticalContentAlignment == VerticalAlignment.Stretch && totalSize.VisibleCount > 1) {
                    spacingMeasure.V = (parentMeasure.V - paddingStart.V - paddingEnd.V - totalSize.Size.V) / ((double)(totalSize.VisibleCount - 1));
                }
            } else {
                if (HorizontalContentAlignment == HorizontalAlignment.Center || (HorizontalContentAlignment == HorizontalAlignment.Stretch && totalSize.VisibleCount <= 1)) {
                    position.V = paddingStart.V + (parentMeasure.V - paddingStart.V - paddingEnd.V - totalSize.Size.V) / 2.0;
                } else if (HorizontalContentAlignment == HorizontalAlignment.Right) {
                    position.V = paddingStart.V + (parentMeasure.U - paddingStart.V - paddingEnd.V - totalSize.Size.V);
                } else if (HorizontalContentAlignment == HorizontalAlignment.Stretch && totalSize.VisibleCount > 1) {
                    spacingMeasure.V = (parentMeasure.V - paddingStart.V - paddingEnd.V - totalSize.Size.V) / ((double)(totalSize.VisibleCount - 1));
                }
            }
            int j = 0;
            int rowOrColumnCount = 0;
            RowOrColumnSizeWithCount rowOrColumnSize = new RowOrColumnSizeWithCount() { Count = 0, Size = new UvMeasure() };
            var correctedSpacingMeasure = spacingMeasure;
            for (var i = 0; i < Children.Count; i++) {
                var child = Children[i];
                Visibility visibility = child.Visibility;
                var desiredMeasure = visibility == Visibility.Visible ? new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height) : new UvMeasure(Orientation, 0, 0);
                if (j >= rowOrColumnSize.Count) {
                    correctedSpacingMeasure = spacingMeasure;
                    if (i > 0) {
                        position.V += rowOrColumnSize.Size.V + correctedSpacingMeasure.V;
                    }
                    position.U = paddingStart.U;
                    rowOrColumnSize = ComputeRowOrColumnSize(availableSize, i, measuredRowOrColumnCounts != null && rowOrColumnCount < measuredRowOrColumnCounts.Count ? (int?)measuredRowOrColumnCounts[rowOrColumnCount] : null);
                    rowOrColumnCount++;
                    j = 0;
                    if (Orientation == Orientation.Horizontal) {
                        if (HorizontalContentAlignment == HorizontalAlignment.Center || (HorizontalContentAlignment == HorizontalAlignment.Stretch && rowOrColumnSize.VisibleCount <= 1)) {
                            position.U = paddingStart.U + (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U) / 2.0;
                        } else if (HorizontalContentAlignment == HorizontalAlignment.Right) {
                            position.U = paddingStart.U + (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U);
                        } else if (HorizontalContentAlignment == HorizontalAlignment.Stretch && rowOrColumnSize.VisibleCount > 1) {
                            correctedSpacingMeasure.U = (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U) / ((double)(rowOrColumnSize.VisibleCount - 1));
                        }
                    } else {
                        if (VerticalContentAlignment == VerticalAlignment.Center || (VerticalContentAlignment == VerticalAlignment.Stretch && rowOrColumnSize.VisibleCount <= 1)) {
                            position.U = paddingStart.U + (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U) / 2.0;
                        } else if (VerticalContentAlignment == VerticalAlignment.Bottom) {
                            position.U = paddingStart.U + (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U);
                        } else if (VerticalContentAlignment == VerticalAlignment.Stretch && rowOrColumnSize.VisibleCount > 1) {
                            correctedSpacingMeasure.U = (parentMeasure.U - paddingStart.U - paddingEnd.U - rowOrColumnSize.Size.U) / ((double)(rowOrColumnSize.VisibleCount - 1));
                        }
                    }
                }
                var correctedPosition = position;
                var correctedMeasure = desiredMeasure;
                if (child is FrameworkElement frameworkChild) {
                    if (Orientation == Orientation.Horizontal) {
                        if (frameworkChild.VerticalAlignment == VerticalAlignment.Center) {
                            correctedPosition.V += (rowOrColumnSize.Size.V - desiredMeasure.V) / 2.0;
                        } else if (frameworkChild.VerticalAlignment == VerticalAlignment.Bottom) {
                            correctedPosition.V += (rowOrColumnSize.Size.V - desiredMeasure.V);
                        } else if (frameworkChild.VerticalAlignment == VerticalAlignment.Stretch) {
                            correctedMeasure.V = rowOrColumnSize.Size.V;
                        }
                    } else {
                        if (frameworkChild.HorizontalAlignment == HorizontalAlignment.Center || frameworkChild.HorizontalAlignment == HorizontalAlignment.Stretch) {
                            correctedPosition.V += (rowOrColumnSize.Size.V - desiredMeasure.V) / 2.0;
                        } else if (frameworkChild.HorizontalAlignment == HorizontalAlignment.Right) {
                            correctedPosition.V += (rowOrColumnSize.Size.V - desiredMeasure.V);
                        } else if (frameworkChild.HorizontalAlignment == HorizontalAlignment.Stretch) {
                            correctedMeasure.V = rowOrColumnSize.Size.V;
                        }
                    }
                }
                child.Arrange(new Rect(correctedPosition.X, correctedPosition.Y, Math.Max(0.0, correctedMeasure.X), Math.Max(0.0, correctedMeasure.Y)));
                position.U += desiredMeasure.U;
                if (visibility != Visibility.Collapsed) {
                    position.U += correctedSpacingMeasure.U;
                }
                j++;
            }
            return finalSize;
        }

        private RowOrColumnSizeWithCount ComputeRowOrColumnSize(UvMeasure availableSize, int startIndex, int? measuredRowOrColumnCount) { //Do not include padding. If measuredRowOrColumnCount not null, then it will use measuredRowOrColumnCounts as a tiebreaker in determining row or column allocation when within 1.1 view pixels of the max size.
            if (startIndex < 0) {
                return new RowOrColumnSizeWithCount() { Count = 0, Size = new UvMeasure(Orientation, 0, 0) };
            }
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var size = new UvMeasure(Orientation, 0, 0);
            int j = 0;
            int visibleCount = 0;
            for (var i = startIndex; i < Children.Count; i++) {
                var child = Children[i];
                Visibility visibility = child.Visibility;
                if(visibility == Visibility.Collapsed) {
                    j++;
                    continue;
                }
                double spacingU = 0.0;
                if (visibleCount > 0) {
                    spacingU = spacingMeasure.U;
                }
                var desiredMeasure = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                var newSizeU = spacingU + desiredMeasure.U + size.U;
                if (visibleCount > 0 && size.U > 0 && ((measuredRowOrColumnCount != null && (newSizeU >= availableSize.U + 1.1 || (newSizeU + 1.1 > availableSize.U && j >= measuredRowOrColumnCount.Value))) || (measuredRowOrColumnCount == null && newSizeU > availableSize.U))) {
                    break;
                }
                size.U = newSizeU;
                size.V = Math.Max(desiredMeasure.V, size.V);
                j++;
                visibleCount++;
            }
            return new RowOrColumnSizeWithCount() { Count = j, VisibleCount = visibleCount, Size = size };
        }

        private TotalSizeWithCount ComputeTotalSize(UvMeasure availableSize, bool useMeasuredRowOrColumnCounts) { //Do not include padding. If useMeasuredRowOrColumnCounts is true, then it will use measuredRowOrColumnCounts as a tiebreaker in determining row or column allocation when within 1.1 view pixels of the max size.
            var useMeasuredCounts = useMeasuredRowOrColumnCounts && measuredRowOrColumnCounts != null && measuredRowOrColumnCounts.Aggregate(0, (acc, val) => acc + val) == Children.Count;
            List<int> rowOrColumnCounts = new List<int>();
            int visibleCount = 0;
            var spacingMeasure = new UvMeasure(Orientation, HorizontalSpacing, VerticalSpacing);
            var size = new UvMeasure(Orientation, 0, 0);
            int i = 0;
            int j = 0;
            while (i < Children.Count) {
                int? measuredRowOrColumnCount = null;
                if (useMeasuredCounts && measuredRowOrColumnCounts != null && j < measuredRowOrColumnCounts.Count) {
                    measuredRowOrColumnCount = measuredRowOrColumnCounts[j];
                }
                var rowOrColumnSize = ComputeRowOrColumnSize(availableSize, i, measuredRowOrColumnCount);
                if(measuredRowOrColumnCount != null && rowOrColumnSize.Count != measuredRowOrColumnCount) {
                    useMeasuredCounts = false;
                }
                size.U = Math.Max(size.U, rowOrColumnSize.Size.U);
                if (i > 0) {
                    size.V += spacingMeasure.V;
                }
                size.V += rowOrColumnSize.Size.V;
                i += Math.Max(rowOrColumnSize.Count, 1);
                j++;
                rowOrColumnCounts.Add(rowOrColumnSize.Count);
                visibleCount += rowOrColumnSize.VisibleCount;
            }
            return new TotalSizeWithCount() { RowOrColumnCounts = rowOrColumnCounts, VisibleCount = visibleCount, Size = size };
        }

        private struct RowOrColumnSizeWithCount {
            public int Count { get; set; }
            public int VisibleCount { get; set; }
            public UvMeasure Size { get; set; }
        }

        private struct TotalSizeWithCount {
            public List<int> RowOrColumnCounts { get; set; }
            public int VisibleCount { get; set; }
            public UvMeasure Size { get; set; }
        }

        private struct UvMeasure {
            Orientation orientation;
            public double U { get; set; }
            public double V { get; set; }
            public double X {
                get {
                    return orientation == Orientation.Horizontal ? U : V;
                }
            }
            public double Y {
                get {
                    return orientation == Orientation.Horizontal ? V : U;
                }
            }
            public UvMeasure(Orientation givenOrientation, double x, double y) {
                orientation = givenOrientation;
                if (orientation == Orientation.Horizontal) {
                    U = x;
                    V = y;
                } else {
                    U = y;
                    V = x;
                }
            }
        }

    }

}