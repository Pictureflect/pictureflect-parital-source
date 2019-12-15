using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PictureflectPartialSource {

    public sealed partial class VariableSlider : UserControl {

        public bool IsAdditive {
            get { return (bool)GetValue(IsAdditiveProperty); }
            set { SetValue(IsAdditiveProperty, value); }
        }
        public static readonly DependencyProperty IsAdditiveProperty = DependencyProperty.Register(nameof(IsAdditive), typeof(bool), typeof(VariableSlider), new PropertyMetadata(false, IsAdditivePropertyChanged));

        private static void IsAdditivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is VariableSlider control) {
                if(control.variableSliderInitialValue == 1 && control.IsAdditive) {
                    control.variableSliderInitialValue = 0;
                }else if(control.variableSliderInitialValue == 0 && !control.IsAdditive) {
                    control.variableSliderInitialValue = 1;
                }
            }
        }

        public double AdditiveHalfRange {
            get { return (double)GetValue(AdditiveHalfRangeProperty); }
            set { SetValue(AdditiveHalfRangeProperty, value); }
        }
        public static readonly DependencyProperty AdditiveHalfRangeProperty = DependencyProperty.Register(nameof(AdditiveHalfRange), typeof(double), typeof(VariableSlider), new PropertyMetadata(1.0));

        bool initialized;

        double currentValue;
        bool variableSliderPointerPressed;
        double variableSliderInitialValue;
        DateTimeOffset lastValueUpdateTime;
        double variableSliderStuckInterval;
        int variableSliderTimerInterval;
        DispatcherTimer variableSliderTimer;

        public VariableSlider() {
            currentValue = 1.0;
            initialized = false;
            variableSliderPointerPressed = false;
            variableSliderInitialValue = 1;
            lastValueUpdateTime = DateTimeOffset.Now;
            variableSliderStuckInterval = 0;
            variableSliderTimerInterval = 20;
            variableSliderTimer = new DispatcherTimer();
            variableSliderTimer.Interval = new TimeSpan(0, 0, 0, 0, variableSliderTimerInterval);
            variableSliderTimer.Tick += variableSliderTimer_Tick;
            this.InitializeComponent();
            initialized = true;
            variableSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(variableSlider_PointerPressed), true);
            variableSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(variableSlider_PointerReleased), true);
            variableSlider.AddHandler(PointerCanceledEvent, new PointerEventHandler(variableSlider_PointerReleased), true);
            variableSlider.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(variableSlider_PointerReleased), true);
            variableSlider.AddHandler(ManipulationCompletedEvent, new ManipulationCompletedEventHandler(variableSlider_ManipulationCompleted), true);
            UpdateControls();
        }

        public event Action<object> ValueChanged;

        public double FactorHalfRange { get; } = 0.8; //Must be strictly between 0 and 1

        public void SetCurrentValue(double newValue) {
            if(currentValue == newValue) {
                return;
            }
            currentValue = newValue;
            UpdateControls();
            ValueChanged?.Invoke(this);
        }

        public double GetCurrentValue() {
            return currentValue;
        }
        
        bool updatingControls = false;
        void UpdateControls() {
            if (!initialized || updatingControls) {
                return;
            }
            updatingControls = true;
            if (!variableSliderPointerPressed) {
                variableSlider.Value = 0.5;
            }
            updatingControls = false;
        }

        static readonly double minVariableSliderStuckInterval = 0.15; //Seconds
        static readonly double minVariableSliderTimerInterval = 0.02; //Seconds
        static readonly double additiveTimerMaxFactor = 0.5;
        static readonly double additiveTimerBaseFactor = 0.1;
        static readonly double additiveTimerIncreaseFactor = 0.4;
        static readonly double timerMaxFactor = 5.0;
        static readonly double timerBaseFactor = 1.03;
        static readonly double timerIncreaseFactor = 1.0/15.0;

        private void variableSliderTimer_Tick(object sender, object e) {
            if (!initialized || !variableSliderPointerPressed) {
                return;
            }
            if (variableSlider.Value == 1 || variableSlider.Value == 0) {
                variableSliderStuckInterval += (double)variableSliderTimerInterval / 1000.0;
                if (DateTimeOffset.Now.Subtract(lastValueUpdateTime).TotalSeconds >= minVariableSliderTimerInterval && variableSliderStuckInterval >= minVariableSliderStuckInterval) {
                    var oldValue = currentValue;
                    if (IsAdditive) {
                        var factor = Math.Min(additiveTimerMaxFactor, additiveTimerBaseFactor + (variableSliderStuckInterval - minVariableSliderStuckInterval) * additiveTimerIncreaseFactor);
                        factor = variableSlider.Value == 1 ? factor : -factor;
                        SetCurrentValue(oldValue + AdditiveHalfRange * factor);
                        variableSliderInitialValue = variableSlider.Value == 1 ? currentValue - AdditiveHalfRange : currentValue + AdditiveHalfRange;
                    } else {
                        var factor = Math.Min(timerMaxFactor, timerBaseFactor + (variableSliderStuckInterval - minVariableSliderStuckInterval) * timerIncreaseFactor);
                        factor = variableSlider.Value == 1 ? factor : 1 / factor;
                        SetCurrentValue(oldValue * factor);
                        variableSliderInitialValue = variableSlider.Value == 1 ? currentValue / (1 + FactorHalfRange) : currentValue / (1 - FactorHalfRange);
                    }
                    lastValueUpdateTime = DateTimeOffset.Now;
                }
            } else {
                variableSliderStuckInterval = 0;
            }
        }  

        private void variableSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            if (!initialized || updatingControls || !variableSliderPointerPressed) {
                return;
            }
            UpdateValueFromSlider();
        }

        void UpdateValueFromSlider() {
            if (!initialized) {
                return;
            }
            if (IsAdditive) {
                SetCurrentValue(variableSliderInitialValue  + variableSlider.Value * AdditiveHalfRange * 2.0 - AdditiveHalfRange);
            } else {
                var factor = variableSlider.Value * FactorHalfRange * 2.0 + (1 - FactorHalfRange);
                SetCurrentValue(variableSliderInitialValue * factor);
            }
        }

        public event Action<object> UpdateFromSliderStarted;
        public event Action<object> UpdateFromSliderStopped; //This is not guaranteed to be fired

        private void variableSlider_PointerPressed(object sender, PointerRoutedEventArgs e) {
            if (!initialized) {
                return;
            }
            variableSliderPointerPressed = true;
            variableSlider.CapturePointer(e.Pointer);
            variableSliderTimer.Start();
            UpdateFromSliderStarted?.Invoke(this); //Important this comes before setting the inital value to give a chance to reset it
            variableSliderInitialValue = currentValue;
            UpdateValueFromSlider();
        }

        void HandleSliderEnd() {
            if (!variableSliderPointerPressed) {
                return;
            }
            variableSliderPointerPressed = false;
            variableSliderStuckInterval = 0;
            variableSliderTimer.Stop();
            UpdateControls();
            UpdateFromSliderStopped?.Invoke(this);
        }

        private void variableSlider_PointerReleased(object sender, PointerRoutedEventArgs e) {
            if (!initialized || !variableSliderPointerPressed) { //Important to check variableSliderPointerPressed since this is also called if PoitnerCanceled or PointerCaptureLost and we don't want to handle it multiple times
                return;
            }
            variableSlider.ReleasePointerCapture(e.Pointer);
            HandleSliderEnd();
        }

        private void variableSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) { //We need to handle this as well as pointer released since manipulation captures the pointer before we have a chance to
            HandleSliderEnd();
        }

    }

}
