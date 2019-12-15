using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PictureflectPartialSource {

    public class CustomVirtualStackPanel : Panel {

        public Orientation Orientation {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(CustomVirtualStackPanel), new PropertyMetadata(Orientation.Vertical, LayoutPropertyChanged));

        public Thickness Padding {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(CustomVirtualStackPanel), new PropertyMetadata(default(Thickness), LayoutPropertyChanged));

        public HorizontalAlignment HorizontalContentAlignment {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty HorizontalContentAlignmentProperty = DependencyProperty.Register(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment), typeof(CustomVirtualStackPanel), new PropertyMetadata(HorizontalAlignment.Stretch, LayoutPropertyChanged));

        public VerticalAlignment VerticalContentAlignment {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }
        public static readonly DependencyProperty VerticalContentAlignmentProperty = DependencyProperty.Register(nameof(VerticalContentAlignment), typeof(VerticalAlignment), typeof(CustomVirtualStackPanel), new PropertyMetadata(VerticalAlignment.Stretch, LayoutPropertyChanged));

        public double CacheLength {
            get { return (double)GetValue(CacheLengthProperty); }
            set { SetValue(CacheLengthProperty, value); }
        }
        public static readonly DependencyProperty CacheLengthProperty = DependencyProperty.Register(nameof(CacheLength), typeof(double), typeof(CustomVirtualStackPanel), new PropertyMetadata(1.0, LayoutPropertyChanged));

        public int MaxUnusedContainers {
            get { return (int)GetValue(MaxUnusedContainersProperty); }
            set { SetValue(MaxUnusedContainersProperty, value); }
        }
        public static readonly DependencyProperty MaxUnusedContainersProperty = DependencyProperty.Register(nameof(MaxUnusedContainers), typeof(int), typeof(CustomVirtualStackPanel), new PropertyMetadata(1000, LayoutPropertyChanged));

        private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomVirtualStackPanel panel) || Equals(e.NewValue, e.OldValue)) {
                return;
            }
            panel.DoFullPanelLayout();
        }

        public IReadOnlyList<ICustomVirtualStackPanelData> ItemsSource {
            get { return (IReadOnlyList<ICustomVirtualStackPanelData>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList<ICustomVirtualStackPanelData>), typeof(CustomVirtualStackPanel), new PropertyMetadata(null, ItemsSourcePropertyChanged));

        private static void ItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomVirtualStackPanel panel)) {
                return;
            }
            if(e.OldValue != null && e.OldValue is IObservableVector<ICustomVirtualStackPanelData> oldObservableVector) {
               oldObservableVector.VectorChanged -= panel.ObservableVector_VectorChanged;
            } else if (e.OldValue != null && e.OldValue is INotifyCollectionChanged oldNotifyCollection) {
                oldNotifyCollection.CollectionChanged -= panel.NotifyCollection_CollectionChanged;
            }
            if (panel.ItemsSource != null && panel.ItemsSource is IObservableVector<ICustomVirtualStackPanelData> observableVector) {
                observableVector.VectorChanged += panel.ObservableVector_VectorChanged;
            }else if (panel.ItemsSource != null && panel.ItemsSource is INotifyCollectionChanged notifyCollection) {
                notifyCollection.CollectionChanged += panel.NotifyCollection_CollectionChanged;
            }
            panel.InvalidateItems();
            panel.DoFullPanelLayout();
        }

        private void ObservableVector_VectorChanged(IObservableVector<ICustomVirtualStackPanelData> sender, IVectorChangedEventArgs args) {
            InvalidateItems();
            DoFullPanelLayout();
        }

        private void NotifyCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            InvalidateItems();
            DoFullPanelLayout();
        }

        public DataTemplate ItemTemplate {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(CustomVirtualStackPanel), new PropertyMetadata(null, ItemTemplatePropertyChanged));

        private static void ItemTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomVirtualStackPanel panel)) {
                return;
            }
            foreach(var item in panel.dataLayoutMap) {
                if (item.Value.Container != null) {
                    item.Value.Container.ContentTemplate = panel.ItemTemplate;
                }
            }
            foreach (var item in panel.unusedContainers) {
                item.ContentTemplate = panel.ItemTemplate;
            }
        }

        public CustomVirtualStackPanel() {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.FrameworkElement", "EffectiveViewportChanged")) {
                EffectiveViewportChanged += CustomVirtualStackPanel_EffectiveViewportChanged;
            } else {
                LayoutUpdated += CustomVirtualStackPanel_LayoutUpdated;
            }
        }

        static readonly double layoutEpsilon = 0.0001;

        Dictionary<ICustomVirtualStackPanelData, VirtualStackPanelItemLayout> dataLayoutMap = new Dictionary<ICustomVirtualStackPanelData, VirtualStackPanelItemLayout>();
        Dictionary<ContentPresenter, VirtualStackPanelItemLayout> containerLayoutMap = new Dictionary<ContentPresenter, VirtualStackPanelItemLayout>();
        Stack<ContentPresenter> unusedContainers = new Stack<ContentPresenter>();

        public ContentPresenter ContainerFromItem(ICustomVirtualStackPanelData item) {
            if(item == null) {
                return null;
            }
            dataLayoutMap.TryGetValue(item, out var result);
            return result?.Container;
        }

        public ICustomVirtualStackPanelData ItemFromContainer(ContentPresenter container) {
            if (container == null) {
                return null;
            }
            containerLayoutMap.TryGetValue(container, out var result);
            return result?.ItemData;
        }

        //Can this if the height of an item has changed
        public void ForceLayoutUpdate() {
            DoFullPanelLayout();
        }

        public int FirstVisibleIndex { get; private set; } = -1;
        public int LastVisibleIndex { get; private set; } = -1;
        public int FirstCacheIndex { get; private set; } = -1;
        public int LastCacheIndex { get; private set; } = -1;

        public event Action<object> VisibleOrCacheIndexesChanged;

        static readonly double visibleViewportTolerance = 1.0;
        static readonly double cacheViewportTolerance = 10.0;

        Rect? computedEffectiveViewport = null;
        public Rect? ComputedEffectiveViewport { get => computedEffectiveViewport; }

        static bool AreRectsDifferentInDirection(Rect newRect, Rect? oldRect, Orientation direction, double tolerance) {
            if(oldRect == null) {
                return true;
            }
            var topLeft = new UvMeasure(direction, newRect.Left, newRect.Top);
            var oldTopLeft = new UvMeasure(direction, oldRect.Value.Left, oldRect.Value.Top);
            var bottomRight = new UvMeasure(direction, newRect.Right, newRect.Bottom);
            var oldBottomRight = new UvMeasure(direction, oldRect.Value.Right, oldRect.Value.Bottom);
            return Math.Abs(topLeft.U - oldTopLeft.U) > visibleViewportTolerance || Math.Abs(bottomRight.U - oldBottomRight.U) > visibleViewportTolerance;
        }

        void HandleEffectiveViewportUpdated(Rect newEffectiveViewport) {
            if (Visibility == Visibility.Visible && computedVisibility == Visibility.Visible && AreRectsDifferentInDirection(newEffectiveViewport, computedEffectiveViewport, Orientation, visibleViewportTolerance)) {
                computedEffectiveViewport = newEffectiveViewport;
                UpdatePanelLayoutOnIdle();
            } else {
                computedEffectiveViewport = newEffectiveViewport;
            }
        }

        private void CustomVirtualStackPanel_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args) {
            if(Visibility != Visibility.Collapsed) {
                ComputeFrameworkElementRootAndVisibility();
            }
            var newEffectiveViewport = args.EffectiveViewport;
            HandleEffectiveViewportUpdated(newEffectiveViewport);
        }

        private void CustomVirtualStackPanel_LayoutUpdated(object sender, object e) {
            if(Visibility == Visibility.Collapsed) {
                return;
            }
            ComputeFrameworkElementRootAndVisibility();
            if (computedRoot == null) {
                return;
            }
            var newEffectiveViewport = computedRoot.TransformToVisual(this).TransformBounds(new Rect(0, 0, computedRoot.ActualWidth, computedRoot.ActualHeight));
            HandleEffectiveViewportUpdated(newEffectiveViewport);
        }

        FrameworkElement computedRoot = null;
        Visibility computedVisibility = Visibility.Visible;
        void ComputeFrameworkElementRootAndVisibility() {
            FrameworkElement root = this;
            UIElement currentControl = root;
            bool isVisible = true;
            while (currentControl != null) {
                if (currentControl is FrameworkElement frameworkElement) {
                    root = frameworkElement;
                }
                isVisible = isVisible && currentControl.Visibility == Visibility.Visible;
                currentControl = VisualTreeHelper.GetParent(currentControl) as FrameworkElement;
            }
            computedVisibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            computedRoot = root;
        }

        Rect? lastUsedVisibleEffectiveViewport = null; //This is to try to ensure we do not get into a loop based on layout round errors - we only use a new viewport in computing the cache indexes once the old one has changed by cacheViewportTolerance
        Rect? lastUsedCacheEffectiveViewport = null; //This is to try to ensure we do not get into a loop based on layout round errors - we only use a new viewport in computing the viewport indexes once the old one has changed by visibleViewportTolerance

        bool hasItemsChanged = false;
        void InvalidateItems() {
            hasItemsChanged = true;
        }

        bool itemsHeightMayHaveChanged = false;
        void DoFullPanelLayout() {
            itemsHeightMayHaveChanged = true;
            UpdatePanelLayoutOnIdle();
        }

        bool updatingPanelLayout = false;
        bool continueUpdatingPanelLayout = false;
        async void UpdatePanelLayoutOnIdle() {
            continueUpdatingPanelLayout = true;
            if (updatingPanelLayout) {
                return;
            }
            updatingPanelLayout = true;
            while (continueUpdatingPanelLayout) {
                continueUpdatingPanelLayout = false;
                await Dispatcher.RunIdleAsync(e => UpdatePanelLayoutDirect());
            }
            updatingPanelLayout = false;
        }

        double totalDesiredLength = 0; //Includes padding

        //Only called at the start when computedEffectiveViewport is null
        void UpdateTotalDesiredLength() {
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var paddingEnd = new UvMeasure(Orientation, Padding.Right, Padding.Bottom);
            double currentU = paddingStart.U;
            var itemsList = ItemsSource ?? new List<ICustomVirtualStackPanelData>();
            for (int i = 0; i < itemsList.Count; i++) {
                var item = itemsList[i];
                currentU += item.DesiredLength;
            }
            currentU += paddingEnd.U;
            if(Math.Abs(totalDesiredLength - currentU) > layoutEpsilon) {
                totalDesiredLength = currentU;
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        //Only to be called by UpdateContainersOnIdle
        void UpdatePanelLayoutDirect() {
            if (computedEffectiveViewport == null) {
                UpdateTotalDesiredLength();
                return;
            }
            var oldHasItemsChanged = hasItemsChanged;
            hasItemsChanged = false;
            var oldItemsHeightMayHaveChanged = itemsHeightMayHaveChanged;
            itemsHeightMayHaveChanged = false;
            bool needsToAddContainers = false;
            Rect visibleViewportRect = (lastUsedCacheEffectiveViewport == null || AreRectsDifferentInDirection(computedEffectiveViewport.Value, lastUsedVisibleEffectiveViewport, Orientation, visibleViewportTolerance)) ? computedEffectiveViewport.Value : lastUsedVisibleEffectiveViewport.Value;
            Rect cacheViewportRect = (lastUsedCacheEffectiveViewport == null || AreRectsDifferentInDirection(computedEffectiveViewport.Value, lastUsedCacheEffectiveViewport, Orientation, cacheViewportTolerance)) ? computedEffectiveViewport.Value : lastUsedCacheEffectiveViewport.Value;
            if(visibleViewportRect == lastUsedVisibleEffectiveViewport && cacheViewportRect == lastUsedCacheEffectiveViewport && !oldItemsHeightMayHaveChanged && !oldHasItemsChanged) {
                return;
            }
            lastUsedVisibleEffectiveViewport = visibleViewportRect;
            lastUsedCacheEffectiveViewport = cacheViewportRect;
            bool hasChanged = false;
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var paddingEnd = new UvMeasure(Orientation, Padding.Right, Padding.Bottom);
            double currentU = paddingStart.U;
            int newFirstCacheIndex = -1;
            int newFirstVisibleIndex = -1;
            int newLastCacheIndex = -1;
            int newLastVisibleIndex = -1;
            var itemsList = ItemsSource ?? new List<ICustomVirtualStackPanelData>();
            List<double> itemOffsets = new List<double>(itemsList.Count);
            for (int i = 0; i < itemsList.Count; i++) {
                var item = itemsList[i];
                itemOffsets.Add(currentU);
                double desiredLength = item.DesiredLength;
                if (newFirstCacheIndex < 0 && currentU + desiredLength > cacheViewportRect.Top - CacheLength * cacheViewportRect.Height - cacheViewportTolerance) {
                    newFirstCacheIndex = i;
                }
                if (newFirstVisibleIndex < 0 && currentU + desiredLength > visibleViewportRect.Top - visibleViewportTolerance) {
                    newFirstVisibleIndex = i;
                }
                if (newLastVisibleIndex < 0 && currentU + desiredLength >= visibleViewportRect.Bottom + visibleViewportTolerance) {
                    newLastVisibleIndex = i;
                }
                if (newLastCacheIndex < 0 && currentU + desiredLength >= cacheViewportRect.Bottom + CacheLength * cacheViewportRect.Height + cacheViewportTolerance) {
                    newLastCacheIndex = i;
                }
                currentU += desiredLength;
            }
            var newTotalDesiredLength = currentU + paddingEnd.U;
            if(Math.Abs(newTotalDesiredLength - totalDesiredLength) > layoutEpsilon) {
                totalDesiredLength = newTotalDesiredLength;
                hasChanged = true;
            }
            if(newLastVisibleIndex < 0) {
                newLastVisibleIndex = itemsList.Count - 1;
            }
            if (newLastCacheIndex < 0) {
                newLastCacheIndex = itemsList.Count - 1;
            }
            if (newFirstVisibleIndex < 0 || newLastVisibleIndex < 0) {
                newFirstVisibleIndex = -1;
                newLastVisibleIndex = -1;
            } else {
                newFirstCacheIndex = Math.Min(newFirstVisibleIndex, newFirstCacheIndex);
                newLastCacheIndex = Math.Min(newLastVisibleIndex, newLastCacheIndex);
            }
            if (newFirstCacheIndex < 0 || newLastCacheIndex < 0) {
                newFirstCacheIndex = -1;
                newLastCacheIndex = -1;
            }
            if (newFirstVisibleIndex != FirstVisibleIndex || newLastVisibleIndex != LastVisibleIndex || newFirstCacheIndex != FirstCacheIndex || newLastCacheIndex != LastCacheIndex) {
                FirstVisibleIndex = newFirstVisibleIndex;
                LastVisibleIndex = newLastVisibleIndex;
                FirstCacheIndex = newFirstCacheIndex;
                LastCacheIndex = newLastCacheIndex;
                VisibleOrCacheIndexesChanged?.Invoke(this);
            } else {
                if (!oldHasItemsChanged && !oldItemsHeightMayHaveChanged) {
                    return;
                }
            }
            var newItemsWithContainer = new HashSet<ICustomVirtualStackPanelData>();
            int firstSequentialIndex = newFirstVisibleIndex;
            int lastSequentialIndex = newLastVisibleIndex;
            if(firstSequentialIndex < 0 || lastSequentialIndex < 0) {
                if(newLastCacheIndex == itemsList.Count - 1) {
                    firstSequentialIndex = newLastCacheIndex;
                    lastSequentialIndex = newLastCacheIndex;
                } else {
                    firstSequentialIndex = newFirstCacheIndex;
                    lastSequentialIndex = newFirstCacheIndex;
                }
            }
            for (int i = firstSequentialIndex; i >= newFirstCacheIndex && i <= newLastCacheIndex && i >= 0 && i < itemsList.Count; i = GetNextContainerIndexToGenerate(firstSequentialIndex, lastSequentialIndex, newFirstCacheIndex, newLastCacheIndex, i)) {
                var item = itemsList[i];
                var desiredLength = item.DesiredLength;
                if(desiredLength < layoutEpsilon) {
                    continue;
                }
                var offset = i < itemOffsets.Count ? itemOffsets[i] : 0;
                dataLayoutMap.TryGetValue(item, out var layout);
                if (layout == null || layout.Container == null) {
                    if (unusedContainers.Count > 0) {
                        var container = unusedContainers.Pop();
                        container.Content = item;
                        if (layout == null) {
                            layout = new VirtualStackPanelItemLayout(item);
                            dataLayoutMap.Add(item, layout);
                        } else {
                            layout.CreateContainerPriority = -1;
                        }
                        layout.Container = container;
                        containerLayoutMap.Add(container, layout);
                        container.Visibility = Visibility.Visible;
                        hasChanged = true;
                    } else {
                        if (layout == null) {
                            layout = new VirtualStackPanelItemLayout(item) { CreateContainerPriority = i };
                            dataLayoutMap.Add(item, layout);
                            needsToAddContainers = true;
                        }
                    }
                    if (layout != null) {
                        layout.Offset = offset;
                        layout.DesiredLength = desiredLength;
                    }
                } else {
                    if(Math.Abs(layout.Offset - offset) > layoutEpsilon) {
                        layout.Offset = offset;
                        hasChanged = true;
                    }
                    if (Math.Abs(layout.DesiredLength - desiredLength) > layoutEpsilon) {
                        layout.DesiredLength = desiredLength;
                        hasChanged = true;
                    }
                }
                newItemsWithContainer.Add(item);
            }
            List<KeyValuePair<ICustomVirtualStackPanelData, VirtualStackPanelItemLayout>> toRemove = new List<KeyValuePair<ICustomVirtualStackPanelData, VirtualStackPanelItemLayout>>();
            foreach (var item in dataLayoutMap) {
                if (!newItemsWithContainer.Contains(item.Key)) {
                    toRemove.Add(item);
                }
            }
            foreach (var item in toRemove) {
                hasChanged = true;
                var container = item.Value.Container;
                if(container != null) {
                    container.Visibility = Visibility.Collapsed;
                    unusedContainers.Push(container);
                    containerLayoutMap.Remove(container);
                }
                dataLayoutMap.Remove(item.Key);
            }
            if (toRemove.Count > 0 && unusedContainers.Count > MaxUnusedContainers) {
                while (unusedContainers.Count > 0) {
                    var container = unusedContainers.Pop();
                    container.Content = null;
                    Children.Remove(container);
                }
            }
            if (needsToAddContainers) {
                ProcessAddContainersOnIdle();
            }
            if (hasChanged) {
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        //Note this can return items out of range
        int GetNextContainerIndexToGenerate(int startSequentialIndex, int endSequentialIndex, int firstCacheIndex, int lastCacheIndex, int currentIndex) {
            if(currentIndex == endSequentialIndex) {
                var result = startSequentialIndex - 1;
                if(result < firstCacheIndex) {
                    result = endSequentialIndex + 1;
                }
                return result;
            }
            if(currentIndex < startSequentialIndex) {
                var result = endSequentialIndex + (startSequentialIndex - currentIndex);
                if(result > lastCacheIndex) {
                    result = currentIndex - 1;
                }
                return result;
            }
            if(currentIndex > endSequentialIndex) {
                var result = startSequentialIndex - (currentIndex - endSequentialIndex) - 1;
                if (result < firstCacheIndex) {
                    result = currentIndex + 1;
                }
                return result;
            }
            return currentIndex + 1;
        }

        bool addingContainers = false;
        bool continueAddingContainers = false;
        async void ProcessAddContainersOnIdle() {
            continueAddingContainers = true;
            if (addingContainers) {
                return;
            }
            addingContainers = true;
            while (continueAddingContainers) {
                continueAddingContainers = false;
                bool hasMore = false;
                await Dispatcher.RunIdleAsync(e => {
                    hasMore = ProcessAddContainersDirect();
                });
                if (hasMore) {
                    continueAddingContainers = true;
                }
            }
            addingContainers = false;
        }

        bool ProcessAddContainersDirect() {
            int batchSize = 30;
            var toAdd = dataLayoutMap.Values.Where(item => item.Container == null).OrderBy(item => item.CreateContainerPriority);
            int index = 0;
            foreach (var item in toAdd) {
                var container = new ContentPresenter();
                item.Container = container;
                container.ContentTemplate = ItemTemplate;
                Children.Add(container);
                container.Content = item.ItemData;
                containerLayoutMap.Add(container, item);
                container.Visibility = Visibility.Visible;
                index++;
                if (index >= batchSize) {
                    return true;
                }
            }
            return false;
        }

        protected override Size MeasureOverride(Size availableSize) {
            var childAvailableSize = new Size(availableSize.Width - Padding.Left - Padding.Right, availableSize.Height - Padding.Top - Padding.Bottom);
            UvMeasure totalMeasure = new UvMeasure(Orientation, 0, 0);
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            var paddingEnd = new UvMeasure(Orientation, Padding.Right, Padding.Bottom);
            foreach (var child in Children) {
                if(!(child is ContentPresenter container)) {
                    child.Measure(new Size(0, 0));
                    continue;
                }
                containerLayoutMap.TryGetValue(container, out var layout);
                if(layout == null) {
                    child.Measure(new Size(0, 0));
                    continue;
                }
                var desiredLength = layout.DesiredLength;
                var childAvailableSizeCorrected = new UvMeasure(Orientation, childAvailableSize.Width, childAvailableSize.Height);
                childAvailableSizeCorrected.U = Math.Min(childAvailableSizeCorrected.U, desiredLength);
                child.Measure(new Size(childAvailableSizeCorrected.X, childAvailableSizeCorrected.Y));
                if (child.Visibility != Visibility.Collapsed) {
                    var childDesiredSize = new UvMeasure(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
                    totalMeasure.V = Math.Max(childDesiredSize.V, totalMeasure.V);
                }
            }
            totalMeasure.U = totalDesiredLength;
            totalMeasure.V += paddingStart.V + paddingEnd.V;
            return new Size(totalMeasure.X, totalMeasure.Y);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var childAvailableSize = new Size(finalSize.Width - Padding.Left - Padding.Right, finalSize.Height - Padding.Top - Padding.Bottom);
            var paddingStart = new UvMeasure(Orientation, Padding.Left, Padding.Top);
            foreach (var child in Children) {
                if (!(child is ContentPresenter container)) {
                    child.Measure(new Size(0, 0));
                    continue;
                }
                containerLayoutMap.TryGetValue(container, out var layout);
                if (layout == null) {
                    child.Measure(new Size(0, 0));
                    continue;
                }
                var desiredLength = layout.DesiredLength;
                var correctedDesiredSize = child.Visibility != Visibility.Collapsed ? child.DesiredSize : new Size(0, 0);
                var correctedOffset = new UvMeasure(Orientation, 0, 0);
                correctedOffset.U = layout.Offset;
                correctedOffset.V = paddingStart.V;
                if(Orientation == Orientation.Horizontal) {
                    correctedDesiredSize.Width = Math.Min(childAvailableSize.Width, desiredLength);
                    if (VerticalContentAlignment == VerticalAlignment.Center) {
                        correctedOffset.V = (childAvailableSize.Height - correctedDesiredSize.Height) / 2.0;
                    }else if (VerticalContentAlignment == VerticalAlignment.Bottom) {
                        correctedOffset.V = childAvailableSize.Height - correctedDesiredSize.Height;
                    } else if (VerticalContentAlignment == VerticalAlignment.Stretch) {
                        correctedDesiredSize.Height = childAvailableSize.Height;
                    }
                }else {
                    correctedDesiredSize.Height = Math.Min(childAvailableSize.Height, desiredLength);
                    if (HorizontalContentAlignment == HorizontalAlignment.Center) {
                        correctedOffset.V = (childAvailableSize.Width - correctedDesiredSize.Width) / 2.0;
                    } else if (HorizontalContentAlignment == HorizontalAlignment.Right) {
                        correctedOffset.V = childAvailableSize.Width - correctedDesiredSize.Width;
                    } else if (HorizontalContentAlignment == HorizontalAlignment.Stretch) {
                        correctedDesiredSize.Width = childAvailableSize.Width;
                    }
                }
                child.Arrange(new Rect(correctedOffset.X, correctedOffset.Y, correctedDesiredSize.Width, correctedDesiredSize.Height));
            }
            return finalSize;
        }

        private class VirtualStackPanelItemLayout {
            public VirtualStackPanelItemLayout(ICustomVirtualStackPanelData data) {
                ItemData = data;
            }
            public ICustomVirtualStackPanelData ItemData { get; } //Cannot be null
            public ContentPresenter Container { get; set; } = null; //null indicates the container is pending creation
            public int CreateContainerPriority { get; set; } = 0;
            public double Offset { get; set; } = 0;
            public double DesiredLength { get; set; } = 0;
        }

        //Note we consider U to be in the direction of Orientation
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

    public interface ICustomVirtualStackPanelData {
        double DesiredLength { get; }
    }

}