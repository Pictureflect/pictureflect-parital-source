using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace PictureflectPartialSource {

    public static class CustomVisualTreeHelper {

        public static bool IsElementEqualToOrAncestorOf(UIElement ancestorElement, UIElement childElement) {
            if(childElement == null || ancestorElement == null) {
                return false;
            }
            UIElement elem = childElement;
            try {
                while (elem != null) {
                    if (elem == ancestorElement) {
                        return true;
                    }
                    elem = VisualTreeHelper.GetParent(elem) as UIElement;
                }
            } catch (Exception) { }
            return false;
        }

        //Includes the current element in the search
        public static UIElement FindAncestorOfType(UIElement childElement, Type type) {
            if (childElement == null) {
                return null;
            }
            UIElement elem = childElement;
            try {
                while (elem != null) {
                    if (elem.GetType() == type) {
                        return elem;
                    }
                    elem = VisualTreeHelper.GetParent(elem) as UIElement;
                }
            } catch (Exception) { }
            return null;
        }

    }

}
