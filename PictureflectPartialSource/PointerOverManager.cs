using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace PictureflectPartialSource {

    public class PointerOverManager {

        bool isPointerOver = false;
        public bool IsPointerOver {
            get => isPointerOver;
            set {
                if (isPointerOver != value) {
                    isPointerOver = value;
                }
                IsPointerOverChanged?.Invoke(this);
            }
        }
        public UIElement Element { get; private set; } = null;

        public event Action<PointerOverManager> IsPointerOverChanged;

        static readonly Lazy<bool> isActualSizePresent = new Lazy<bool>(() => ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ActualSize"));

        public PointerOverManager(UIElement element, bool addEvents) {
            Element = element;
            if (addEvents) {
                AddEvents();
            }
        }

        PointerEventHandler pointerEnteredHandler = null;
        PointerEventHandler pointerExitedHandler = null;
        PointerEventHandler pointerMaybeExitedHandler = null;

        bool areEventsAdded = false;
        public void AddEvents() { //Note that IsMouseOver is not tracked until this.
            if (areEventsAdded || Element == null) {
                return;
            }
            areEventsAdded = true;
            if (pointerEnteredHandler == null) {
                pointerEnteredHandler = new PointerEventHandler(Element_PointerEntered);
            }
            if (pointerExitedHandler == null) {
                pointerExitedHandler = new PointerEventHandler(Element_PointerExited);
            }
            if (pointerMaybeExitedHandler == null) {
                pointerMaybeExitedHandler = new PointerEventHandler(Element_PointerMaybeExited);
            }
            Element.AddHandler(UIElement.PointerPressedEvent, pointerEnteredHandler, true);
            Element.AddHandler(UIElement.PointerEnteredEvent, pointerEnteredHandler, true);
            Element.AddHandler(UIElement.PointerMovedEvent, pointerEnteredHandler, true);
            Element.AddHandler(UIElement.PointerExitedEvent, pointerExitedHandler, true);
            Element.AddHandler(UIElement.PointerReleasedEvent, pointerMaybeExitedHandler, true);
            Element.AddHandler(UIElement.PointerCanceledEvent, pointerMaybeExitedHandler, true);
            Element.AddHandler(UIElement.PointerCaptureLostEvent, pointerMaybeExitedHandler, true);
            if (Element is FrameworkElement frameworkElement) {
                frameworkElement.Unloaded += FrameworkElement_Unloaded;
            }
        }

        public void RemoveEvents(bool setPointerOverToFalse) { //Note that IsMouseOver is no longer tracked after this.
            if (!areEventsAdded || Element == null) {
                return;
            }
            areEventsAdded = false;
            if (pointerEnteredHandler != null) {
                Element.RemoveHandler(UIElement.PointerPressedEvent, pointerEnteredHandler);
                Element.RemoveHandler(UIElement.PointerEnteredEvent, pointerEnteredHandler);
                Element.RemoveHandler(UIElement.PointerMovedEvent, pointerEnteredHandler);
            }
            if (pointerExitedHandler != null) {
                Element.RemoveHandler(UIElement.PointerExitedEvent, pointerExitedHandler);
            }
            if (pointerMaybeExitedHandler != null) {
                Element.RemoveHandler(UIElement.PointerReleasedEvent, pointerMaybeExitedHandler);
                Element.RemoveHandler(UIElement.PointerCanceledEvent, pointerMaybeExitedHandler);
                Element.RemoveHandler(UIElement.PointerCaptureLostEvent, pointerMaybeExitedHandler);
            }
            if (Element is FrameworkElement frameworkElement) {
                frameworkElement.Unloaded -= FrameworkElement_Unloaded;
            }
            if (setPointerOverToFalse) {
                IsPointerOver = false;
            }
        }

        public void SetIsPointerOver(bool value) { //Only do this if necessary
            IsPointerOver = value;
        }

        private void Element_PointerEntered(object sender, PointerRoutedEventArgs e) {
            IsPointerOver = true;
        }

        private void Element_PointerExited(object sender, PointerRoutedEventArgs e) {
            IsPointerOver = false;
        }

        private void Element_PointerMaybeExited(object sender, PointerRoutedEventArgs e) {
            if (Element == null || !IsPointerOver) {
                return;
            }
            Point currentPoint = new Point();
            try {
                currentPoint = e.GetCurrentPoint(Element).Position;
            } catch (Exception) {
                return;
            }
            if (isActualSizePresent.Value) {
                if (currentPoint.X < 0 || currentPoint.X > Element.ActualSize.X || currentPoint.Y < 0 || currentPoint.Y > Element.ActualSize.Y) {
                    IsPointerOver = false;
                }
            } else if (Element is FrameworkElement frameworkElement) {
                if (currentPoint.X < 0 || currentPoint.X > frameworkElement.ActualWidth || currentPoint.Y < 0 || currentPoint.Y > frameworkElement.ActualHeight) {
                    IsPointerOver = false;
                }
            }
        }

        private void FrameworkElement_Unloaded(object sender, RoutedEventArgs e) {
            IsPointerOver = false;
        }

    }

}
