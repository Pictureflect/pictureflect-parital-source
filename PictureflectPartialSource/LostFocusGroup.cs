using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace PictureflectPartialSource {
    public class LostFocusGroup {

        bool hasFocus = false;

        public LostFocusGroup() { }

        public LostFocusGroup(IEnumerable<UIElement> newElements) {
            AddElements(newElements);
        }

        public void AddElements(IEnumerable<UIElement> newElements) {
            foreach(var element in newElements) {
                AddElementHandlers(element);
            }
        }

        public void RemoveElements(IEnumerable<UIElement> elementsToRemove) {
            foreach (var element in elementsToRemove) {
                RemoveElementHandlers(element);
            }
        }

        public event Action<object> LostFocus;

        void AddElementHandlers(UIElement element) {
            RemoveElementHandlers(element);
            element.GotFocus += Element_GotFocus;
            element.LostFocus += Element_LostFocus;
        }

        private void Element_GotFocus(object sender, RoutedEventArgs e) {
            hasFocus = true;
        }

        private void Element_LostFocus(object sender, RoutedEventArgs e) {
            UIElement element = sender as UIElement;
            if(element == null) {
                return;
            }
            hasFocus = false;
            var task = element.Dispatcher.RunIdleAsync((args) => {
                if (!hasFocus) {
                    LostFocus?.Invoke(this);
                }
            });
        }

        void RemoveElementHandlers(UIElement element) {
            element.GotFocus -= Element_GotFocus;
            element.LostFocus -= Element_LostFocus;
        }

    }

}
