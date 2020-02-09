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

    /**
     * Due to a layout cycle bug in the standard slider control, we use this instead. Note that it does not support a thumb tooltip.
    */
    public sealed class CustomSlider : RangeBase {

        public double StepFrequency {
            get { return (double)GetValue(StepFrequencyProperty); }
            set { SetValue(StepFrequencyProperty, value); }
        }
        public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(nameof(StepFrequency), typeof(double), typeof(CustomSlider), new PropertyMetadata(0.01d, StepFrequencyChanged));

        private static void StepFrequencyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomSlider control)) {
                return;
            }
            control.RoundAndSetValue(control.Value);
        }

        public Orientation Orientation {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(CustomSlider), new PropertyMetadata(Orientation.Horizontal, LayoutPropertyChanged));

        private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d == null || !(d is CustomSlider control)) {
                return;
            }
            control.UpdateControls();
        }

        public CustomSlider() {
            this.DefaultStyleKey = typeof(CustomSlider);
        }

        protected override void OnValueChanged(double oldValue, double newValue) {
            if (!maniplationInProgress) {
                UpdateControls();
            }
            base.OnValueChanged(oldValue, newValue);
        }

        protected override void OnMaximumChanged(double oldMaximum, double newMaximum) {
            UpdateControls();
            base.OnMaximumChanged(oldMaximum, newMaximum);
        }

        protected override void OnMinimumChanged(double oldMinimum, double newMinimum) {
            UpdateControls();
            base.OnMinimumChanged(oldMinimum, newMinimum);
        }

        FrameworkElement sliderContainer = null;
        PointerOverManager sliderContainerPointerOverManager = null;
        Grid horizontalTemplate = null;
        FrameworkElement horizontalThumb = null;
        TranslateTransform horizontalThumbTransform = new TranslateTransform();
        FrameworkElement horizontalDecreaseRect = null;
        ScaleTransform horizontalDecreaseRectTransform = new ScaleTransform();
        Grid verticalTemplate = null;
        FrameworkElement verticalThumb = null;
        TranslateTransform verticalThumbTransform = new TranslateTransform();
        FrameworkElement verticalDecreaseRect = null;
        ScaleTransform verticalDecreaseRectTransform = new ScaleTransform();

        protected override void OnApplyTemplate() {
            sliderContainer = GetTemplateChild("SliderContainer") as FrameworkElement;
            if (sliderContainer != null) {
                sliderContainer.PointerPressed += SliderContainer_PointerPressed;
                sliderContainer.AddHandler(PointerPressedEvent, new PointerEventHandler(SliderContainer_PointerPressed), true);
                sliderContainer.AddHandler(PointerReleasedEvent, new PointerEventHandler(SliderContainer_PointerReleased), true);
                sliderContainer.AddHandler(PointerCanceledEvent, new PointerEventHandler(SliderContainer_PointerReleased), true);
                sliderContainer.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(SliderContainer_PointerReleased), true);
                sliderContainer.ManipulationMode = Orientation == Orientation.Vertical ? ManipulationModes.TranslateY : ManipulationModes.TranslateX;
                sliderContainer.ManipulationStarted += SliderContainer_ManipulationStarted;
                sliderContainer.ManipulationDelta += SliderContainer_ManipulationDelta;
                sliderContainer.ManipulationCompleted += SliderContainer_ManipulationCompleted;
                sliderContainer.Unloaded += SliderContainer_Unloaded;
                sliderContainerPointerOverManager = new PointerOverManager(sliderContainer, true);
                sliderContainerPointerOverManager.IsPointerOverChanged += SliderContainerPointerOverManager_IsPointerOverChanged;

            }
            horizontalTemplate = GetTemplateChild("HorizontalTemplate") as Grid;
            if (horizontalTemplate != null) {
                horizontalTemplate.SizeChanged += HorizontalTemplate_SizeChanged;
            }
            horizontalThumb = GetTemplateChild("HorizontalThumb") as FrameworkElement;
            if (horizontalThumb != null) {
                horizontalThumb.RenderTransform = horizontalThumbTransform;
                horizontalThumb.SizeChanged += HorizontalThumb_SizeChanged;
            }
            horizontalDecreaseRect = GetTemplateChild("HorizontalDecreaseRect") as FrameworkElement;
            if (horizontalDecreaseRect != null) {
                horizontalDecreaseRect.RenderTransform = horizontalDecreaseRectTransform;
                horizontalDecreaseRect.RenderTransformOrigin = new Point(0.0, 0.5);
            }
            verticalTemplate = GetTemplateChild("VerticalTemplate") as Grid;
            if (verticalTemplate != null) {
                verticalTemplate.SizeChanged += VerticalTemplate_SizeChanged;
            }
            verticalThumb = GetTemplateChild("VerticalThumb") as FrameworkElement;
            if (verticalThumb != null) {
                verticalThumb.RenderTransform = verticalThumbTransform;
                verticalThumb.SizeChanged += VerticalThumb_SizeChanged;
            }
            verticalDecreaseRect = GetTemplateChild("VerticalDecreaseRect") as FrameworkElement;
            if (verticalDecreaseRect != null) {
                verticalDecreaseRect.RenderTransform = horizontalDecreaseRectTransform;
                verticalDecreaseRect.RenderTransformOrigin = new Point(0.5, 1.0);
            }
            base.OnApplyTemplate();
            UpdateControls();
        }

        readonly static double layoutEpsilon = 0.001;

        private void HorizontalTemplate_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateControls();
        }

        private void VerticalTemplate_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateControls();
        }

        private void HorizontalThumb_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateControls();
        }

        private void VerticalThumb_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateControls();
        }

        bool isPressed = false;
        bool isPointerOver = false;

        private void SliderContainer_PointerPressed(object sender, PointerRoutedEventArgs e) {
            if (sliderContainer == null) {
                return;
            }
            e.Handled = true;
            isPressed = true;
            UpdateVisualState();
            try {
                sliderContainer.CapturePointer(e.Pointer);
            } catch (Exception) { }
            if (Orientation == Orientation.Vertical) {
                if (verticalThumb == null || verticalTemplate == null) {
                    return;
                }
                double trackHeight = verticalTemplate.ActualHeight - verticalThumb.ActualHeight;
                if (trackHeight <= layoutEpsilon) {
                    return;
                }
                Point position;
                try {
                    position = e.GetCurrentPoint(verticalTemplate).Position;
                } catch (Exception) {
                    return;
                }
                double fraction = 1.0 - (position.Y - verticalThumb.ActualHeight / 2.0) / trackHeight;
                double newValue = fraction * (Maximum - Minimum) + Minimum;
                RoundAndSetValue(newValue);
            } else {
                if (horizontalThumb == null || horizontalTemplate == null) {
                    return;
                }
                double trackWidth = horizontalTemplate.ActualWidth - horizontalThumb.ActualWidth;
                if (trackWidth <= layoutEpsilon) {
                    return;
                }
                Point position;
                try {
                    position = e.GetCurrentPoint(horizontalTemplate).Position;
                } catch (Exception) {
                    return;
                }
                double fraction = (position.X - horizontalThumb.ActualWidth / 2.0) / trackWidth;
                double newValue = fraction * (Maximum - Minimum) + Minimum;
                RoundAndSetValue(newValue);
            }
        }

        private void SliderContainer_PointerReleased(object sender, PointerRoutedEventArgs e) {
            if (sliderContainer == null || !isPressed) { //Important to check isPressed since this is also called if PoitnerCanceled or PointerCaptureLost and we don't want to handle it multiple times
                return;
            }
            e.Handled = true;
            isPressed = false;
            UpdateVisualState();
            try {
                sliderContainer.ReleasePointerCapture(e.Pointer);
            } catch (Exception) { }
        }

        bool maniplationInProgress = false;
        double manipulationInitialOffset = 0;

        private void SliderContainer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e) {
            maniplationInProgress = true;
            e.Handled = true;
            if (Orientation == Orientation.Vertical) {
                manipulationInitialOffset = verticalThumbTransform.Y;
            } else {
                manipulationInitialOffset = horizontalThumbTransform.X;
            }
        }

        private void SliderContainer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            if (!maniplationInProgress) {
                return;
            }
            e.Handled = true;
            if (Orientation == Orientation.Vertical) {
                if (verticalThumb == null || verticalTemplate == null) {
                    return;
                }
                double trackHeight = verticalTemplate.ActualHeight - horizontalThumb.ActualHeight;
                if (trackHeight <= layoutEpsilon) {
                    return;
                }
                double thumbTop = verticalThumbTransform.Y + e.Delta.Translation.Y;
                if (thumbTop > trackHeight) {
                    thumbTop = trackHeight;
                }
                if (thumbTop < 0) {
                    thumbTop = 0;
                }
                double valueFrac = 1.0 - thumbTop / trackHeight;
                UpdateControlsDirect(valueFrac);
                UpdateValue();
            } else {
                if (horizontalThumb == null || horizontalTemplate == null) {
                    return;
                }
                double trackWidth = horizontalTemplate.ActualWidth - horizontalThumb.ActualWidth;
                if (trackWidth <= layoutEpsilon) {
                    return;
                }
                double thumbLeft = manipulationInitialOffset + e.Cumulative.Translation.X;
                if (thumbLeft > trackWidth) {
                    thumbLeft = trackWidth;
                }
                if (thumbLeft < 0) {
                    thumbLeft = 0;
                }
                double valueFrac = thumbLeft / trackWidth;
                UpdateControlsDirect(valueFrac);
                UpdateValue();
            }
        }

        private void SliderContainer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            if (!maniplationInProgress) {
                return;
            }
            e.Handled = true;
            maniplationInProgress = false;
            manipulationInitialOffset = 0;
            UpdateControls();
        }

        void UpdateControls() {
            if (Orientation == Orientation.Vertical) {
                if (sliderContainer != null) {
                    sliderContainer.ManipulationMode = ManipulationModes.TranslateY;
                }
                if (verticalTemplate != null && verticalTemplate.Visibility != Visibility.Visible) {
                    verticalTemplate.Visibility = Visibility.Visible;
                }
                if (horizontalTemplate != null && horizontalTemplate.Visibility != Visibility.Collapsed) {
                    horizontalTemplate.Visibility = Visibility.Collapsed;
                }
            } else {
                if (sliderContainer != null) {
                    sliderContainer.ManipulationMode = ManipulationModes.TranslateX;
                }
                if (horizontalTemplate != null && horizontalTemplate.Visibility != Visibility.Visible) {
                    horizontalTemplate.Visibility = Visibility.Visible;
                }
                if (verticalTemplate != null && verticalTemplate.Visibility != Visibility.Collapsed) {
                    verticalTemplate.Visibility = Visibility.Collapsed;
                }
            }
            double diff = (Maximum - Minimum);
            if (diff <= 0) {
                return;
            }
            double valueFrac = (Math.Max(Minimum, Math.Min(Maximum, Value)) - Minimum) / diff;
            UpdateControlsDirect(valueFrac);
        }

        void UpdateControlsDirect(double fraction) {
            if (Orientation == Orientation.Vertical) {
                if (verticalThumb == null || verticalTemplate == null) {
                    return;
                }
                double newFraction = Math.Max(0.0, Math.Min(1.0, fraction));
                double trackHeight = verticalTemplate.ActualHeight - verticalThumb.ActualHeight;
                double thumbTop = trackHeight * (1.0 - newFraction);
                verticalThumbTransform.Y = thumbTop;
                verticalDecreaseRectTransform.ScaleY = newFraction;
            } else {
                if (horizontalThumb == null || horizontalTemplate == null) {
                    return;
                }
                double newFraction = Math.Max(0.0, Math.Min(1.0, fraction));
                double trackWidth = horizontalTemplate.ActualWidth - horizontalThumb.ActualWidth;
                double thumbLeft = trackWidth * newFraction;
                horizontalThumbTransform.X = thumbLeft;
                horizontalDecreaseRectTransform.ScaleX = newFraction;
            }
        }

        void UpdateValue() {
            if (Orientation == Orientation.Vertical) {
                if (verticalThumb == null || verticalTemplate == null) {
                    return;
                }
                double trackHeight = verticalTemplate.ActualHeight - verticalThumb.ActualHeight;
                if (trackHeight <= layoutEpsilon) {
                    return;
                }
                double fraction = verticalThumbTransform.Y / trackHeight;
                double newValue = fraction * (Maximum - Minimum) + Minimum;
                RoundAndSetValue(newValue);
            } else {
                if (horizontalThumb == null || horizontalTemplate == null) {
                    return;
                }
                double trackWidth = horizontalTemplate.ActualWidth - horizontalThumb.ActualWidth;
                if (trackWidth <= layoutEpsilon) {
                    return;
                }
                double fraction = horizontalThumbTransform.X / trackWidth;
                double newValue = fraction * (Maximum - Minimum) + Minimum;
                RoundAndSetValue(newValue);
            }
        }

        void RoundAndSetValue(double newValue) {
            double correctedValue = newValue;
            if (StepFrequency > 0) {
                correctedValue = Math.Round(newValue / StepFrequency) * StepFrequency;
            }
            Value = Math.Max(Minimum, Math.Min(Maximum, correctedValue));
        }

        private void SliderContainerPointerOverManager_IsPointerOverChanged(PointerOverManager sender) {
            isPointerOver = sender.IsPointerOver;
            UpdateVisualState();
        }

        private void SliderContainer_Unloaded(object sender, RoutedEventArgs e) {
            isPressed = false;
            isPointerOver = false;
            UpdateVisualState();
        }

        void UpdateVisualState() {
            if (!IsEnabled) {
                VisualStateManager.GoToState(this, "Disabled", false);
            } else if (isPressed) {
                VisualStateManager.GoToState(this, "Pressed", true);
            } else if (isPointerOver) {
                VisualStateManager.GoToState(this, "PointerOver", true);
            } else {
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }

    }

}
