using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PictureflectPartialSource {

    public class BoolInverseConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool boolVal) {
                return !boolVal;
            }
            return value == null;
        }

        // ConvertBack is not implemented for a OneWay binding.
        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }

    }

}
