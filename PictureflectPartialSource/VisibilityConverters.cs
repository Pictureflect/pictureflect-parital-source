using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PictureflectPartialSource {

    public class NullVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, string language) {
            if(value is bool) {
                if((bool)value) {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }else if (value is string) {
                if (!string.IsNullOrEmpty((string)value)) {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }else if (value != null) {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }

    }

    public class NullVisibilityInverseConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool) {
                if ((bool)value) {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            } else if (value is string) {
                if (!string.IsNullOrEmpty((string)value)) {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            } else if (value != null) {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }

    }

    public class VisibilityInverseConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is Visibility) {
                if (((Visibility)value) == Visibility.Visible) {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            return Visibility.Visible;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }

    }

}
