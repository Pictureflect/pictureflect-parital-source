using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace PictureflectPartialSource {

    public sealed class UnhandledButton : Button {

        protected override void OnPointerPressed(PointerRoutedEventArgs e) {
            bool wasHandled = e.Handled;
            base.OnPointerPressed(e);
            e.Handled = wasHandled;
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e) {
            bool wasHandled = e.Handled;
            base.OnPointerReleased(e);
            e.Handled = wasHandled;
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e) {
            bool wasHandled = e.Handled;
            base.OnPointerCanceled(e);
            e.Handled = wasHandled;
        }

        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e) {
            bool wasHandled = e.Handled;
            base.OnPointerCaptureLost(e);
            e.Handled = wasHandled;
        }

    }

}
