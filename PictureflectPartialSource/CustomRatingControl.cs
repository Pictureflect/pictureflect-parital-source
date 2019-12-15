using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace PictureflectPartialSource {

    public sealed class CustomRatingControl : Control {

        //Note that 0 should generally be interpreted as unset
        public double Rating {
            get { return (double)GetValue(RatingProperty); }
            set { SetValue(RatingProperty, value); }
        }
        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(nameof(Rating), typeof(double), typeof(CustomRatingControl), new PropertyMetadata(0.0d, RatingPropertyChanged));

        private static void RatingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomRatingControl control)) {
                return;
            }
            var newRating = control.CorrectRatingValue(control.Rating);
            if (newRating != control.Rating) {
                control.Rating = newRating;
            } else {
                control.UpdateControls();
                control.RatingChanged?.Invoke(control);
            }
        }

        public event Action<object> RatingChanged;

        double CorrectRatingValue(double value) {
            if (value > MaxRating) {
                value = MaxRating;
            }
            if (value < 0.0) {
                value = 0.0;
            }
            return value;
        }

        public double IconSpacing {
            get { return (double)GetValue(IconSpacingProperty); }
            set { SetValue(IconSpacingProperty, value); }
        }
        public static readonly DependencyProperty IconSpacingProperty = DependencyProperty.Register(nameof(IconSpacing), typeof(double), typeof(CustomRatingControl), new PropertyMetadata(0.0d, IconSpacingPropertyChanged));

        private static void IconSpacingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomRatingControl control)) {
                return;
            }
            control.UpdateIconProperties();
        }

        double MaxRating { get; } = 5.0;

        double previewRating = 0.0;

        public CustomRatingControl() {
            this.DefaultStyleKey = typeof(CustomRatingControl);
            IsEnabledChanged += CustomRatingControl_IsEnabledChanged;
            KeyDown += CustomRatingControl_KeyDown;
        }

        private void CustomRatingControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            UpdateIconProperties();
            UpdateVisualState();
        }

        private void CustomRatingControl_KeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Handled) {
                return;
            }
            switch (e.Key) {
                case Windows.System.VirtualKey.Right:
                    Rating = CorrectRatingValue(Rating + 1.0);
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Up:
                    Rating = CorrectRatingValue(Rating + 1.0);
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Left:
                    Rating = CorrectRatingValue(Rating - 1.0);
                    e.Handled = true;
                    break;
                case Windows.System.VirtualKey.Down:
                    Rating = CorrectRatingValue(Rating - 1.0);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        FrameworkElement contentRoot = null;
        StackPanel ratingBackgroundStackPanel = null;
        StackPanel ratingForegroundStackPanel = null;
        Style backgroundIconStyle = null;
        Style foregroundIconStyle = null;
        Style foregroundIconOverStyle = null;
        Style foregroundIconDisabledStyle = null;

        protected override void OnApplyTemplate() {
            var layoutRoot = GetTemplateChild("LayoutRoot") as FrameworkElement;
            if (layoutRoot != null) {
                if (layoutRoot.Resources.TryGetValue("BackgroundIconStyle", out var backgroundStyleObject)) {
                    backgroundIconStyle = backgroundStyleObject as Style;
                }
                if (layoutRoot.Resources.TryGetValue("ForegroundIconStyle", out var foregroundStyleObject)) {
                    foregroundIconStyle = foregroundStyleObject as Style;
                }
                if (layoutRoot.Resources.TryGetValue("ForegroundIconOverStyle", out var foregroundOverStyleObject)) {
                    foregroundIconOverStyle = foregroundOverStyleObject as Style;
                }
                if (layoutRoot.Resources.TryGetValue("ForegroundIconDisabledStyle", out var foregroundDisabledStyleObject)) {
                    foregroundIconDisabledStyle = foregroundDisabledStyleObject as Style;
                }
            }
            contentRoot = GetTemplateChild("ContentRoot") as FrameworkElement;
            if(contentRoot != null) {
                contentRoot.HorizontalAlignment = HorizontalAlignment.Left;
                contentRoot.Tapped += ContentRootStackPanel_Tapped;
                contentRoot.PointerEntered += ContentRootStackPanel_PointerEntered;
                contentRoot.PointerExited += ContentRootStackPanel_PointerExited;
                contentRoot.PointerMoved += ContentRootStackPanel_PointerMoved;
                contentRoot.Unloaded += ContentRootStackPanel_Unloaded;
            }
            ratingBackgroundStackPanel = GetTemplateChild("RatingBackgroundStackPanel") as StackPanel;
            ratingForegroundStackPanel = GetTemplateChild("RatingForegroundStackPanel") as StackPanel;
            base.OnApplyTemplate();
            UpdateControls();
        }

        bool isPointerOver = false;

        private void ContentRootStackPanel_Tapped(object sender, TappedRoutedEventArgs e) {
            if (ratingBackgroundStackPanel == null || !IsEnabled || ratingBackgroundStackPanel.ActualWidth <= 0) {
                return;
            }
            e.Handled = true;
            Point position;
            try {
                position = e.GetPosition(ratingBackgroundStackPanel);
            } catch (Exception) {
                return;
            }
            double newRating = ComputeNewRating(position.X);
            if (Math.Round(newRating) == Rating) {
                Rating = 0;
            } else {
                Rating = newRating;
            }
        }
        private void ContentRootStackPanel_PointerEntered(object sender, PointerRoutedEventArgs e) {
            isPointerOver = true;
            UpdateIconProperties();
            UpdateVisualState();
        }

        private void ContentRootStackPanel_PointerExited(object sender, PointerRoutedEventArgs e) {
            previewRating = 0.0;
            isPointerOver = false;
            UpdateRatingView();
            UpdateIconProperties();
            UpdateVisualState();
        }

        private void ContentRootStackPanel_Unloaded(object sender, RoutedEventArgs e) {
            isPointerOver = false;
            UpdateIconProperties();
            UpdateVisualState();
        }
        private void ContentRootStackPanel_PointerMoved(object sender, PointerRoutedEventArgs e) {
            if (!isPointerOver) {
                return;
            }
            Point position;
            try {
                position = e.GetCurrentPoint(ratingBackgroundStackPanel).Position;
            } catch (Exception) {
                return;
            }
            previewRating = ComputeNewRating(position.X);
            UpdateRatingView();
        }

        double ComputeNewRating(double pointerXPos) {
            if (ratingBackgroundStackPanel == null || ratingBackgroundStackPanel.ActualWidth <= 0) {
                return Rating;
            }
            return Math.Min(Math.Max(0.0, Math.Floor(((pointerXPos + IconSpacing / 2.0) / (ratingBackgroundStackPanel.ActualWidth + IconSpacing)) * MaxRating)) + 1.0, MaxRating);
        }

        void UpdateControls() {
            if (ratingBackgroundStackPanel == null || ratingForegroundStackPanel == null) {
                return;
            }
            bool shouldUpdateProperties = false;
            int numIcons = Math.Max(1, (int)Math.Round(MaxRating));
            var backgroundChildCount = ratingBackgroundStackPanel.Children.Count;
            if (backgroundChildCount < numIcons) {
                shouldUpdateProperties = true;
                for (int i = backgroundChildCount; i < numIcons; i++) {
                    var textBlock = new TextBlock() { Style = backgroundIconStyle };
                    ratingBackgroundStackPanel.Children.Add(textBlock);
                }
            } else if(backgroundChildCount > numIcons) {
                shouldUpdateProperties = true;
                for (int i = backgroundChildCount - 1; i >= numIcons; i--) {
                    ratingBackgroundStackPanel.Children.RemoveAt(i);
                }
            }
            var foregroundChildCount = ratingForegroundStackPanel.Children.Count;
            if (foregroundChildCount < numIcons) {
                shouldUpdateProperties = true;
                for (int i = foregroundChildCount; i < numIcons; i++) {
                    var textBlock = new TextBlock() { Style = foregroundIconStyle };
                    ratingForegroundStackPanel.Children.Add(textBlock);
                }
            } else if (foregroundChildCount > numIcons) {
                shouldUpdateProperties = true;
                for (int i = foregroundChildCount - 1; i >= numIcons; i--) {
                    ratingForegroundStackPanel.Children.RemoveAt(i);
                }
            }
            if (shouldUpdateProperties) {
                UpdateIconProperties();
            }
            UpdateRatingView();
        }

        void UpdateRatingView() {
            if(ratingForegroundStackPanel == null) {
                return;
            }
            double ratingToView = isPointerOver && previewRating > 0 ? previewRating : Rating;
            int wholeRating = Math.Max(0, (int)Math.Round(ratingToView));
            for (int i = 0; i < ratingForegroundStackPanel.Children.Count; i++) {
                var child = ratingForegroundStackPanel.Children[i];
                child.Visibility = i + 1 <= wholeRating ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void UpdateIconProperties() {
            if (ratingBackgroundStackPanel == null || ratingForegroundStackPanel == null) {
                return;
            }
            for (int i = 0; i < ratingBackgroundStackPanel.Children.Count; i++) {
                var child = ratingBackgroundStackPanel.Children[i] as FrameworkElement;
                if (child != null) {
                    child.Margin = i < ratingBackgroundStackPanel.Children.Count - 1 ? new Thickness(0, 0, IconSpacing, 0) : new Thickness(0);
                    child.Style = backgroundIconStyle;
                }
            }
            for (int i = 0; i < ratingForegroundStackPanel.Children.Count; i++) {
                var child = ratingForegroundStackPanel.Children[i] as FrameworkElement;
                if (child != null) {
                    child.Margin = i < ratingForegroundStackPanel.Children.Count - 1 ? new Thickness(0, 0, IconSpacing, 0) : new Thickness(0);
                    child.Style = (!IsEnabled ? foregroundIconDisabledStyle : (isPointerOver ? foregroundIconOverStyle : foregroundIconStyle)) ?? foregroundIconStyle;
                }
            }
        }

        void UpdateVisualState() {
            if (!IsEnabled) {
                VisualStateManager.GoToState(this, "Disabled", false);
            } else if (isPointerOver) {
                VisualStateManager.GoToState(this, "PointerOver", true);
            } else {
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }

    }

}
