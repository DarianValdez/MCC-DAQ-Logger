using MccDaq;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace DAQ_Data_Logger.ViewModels
{
    /// <summary>
    /// Convert Double to String.
    /// </summary>
    class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToString((double)value);
        }
    }

    /// <summary>
    /// Convert Integer to String.
    /// </summary>
    class IntegerToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return System.Convert.ToInt32(value);
            }
            catch (Exception ex)
            {
                return (int)parameter;
            }
        }
    }

    /// <summary>
    /// Convert Range type to formatted string. Bip5Volts => 5V
    /// </summary>
    class RangeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                //if single range
                if (value.GetType() == typeof(Range))
                {
                    return ConvertName((Range)value);
                }
                //if collection of ranges
                else
                {
                    string[] names = new string[((ObservableCollection<Range>)value).Count];
                    int i = 0;
                    foreach (Range r in (ObservableCollection<Range>)value)
                    {
                        names[i] = ConvertName(r);
                        i++;
                    }
                    return names;
                }
            }
            else return new string[0];
        }
        private string ConvertName(Range r)
        {
            string name = Enum.GetName(typeof(Range), r);
            StringBuilder sb = new StringBuilder(name);
            sb.Replace("Bip", "");
            sb.Replace("Pt", ".");
            sb.Replace("Volts", "V");
            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = (string)value;
            StringBuilder sb = new StringBuilder(name);
            sb.Replace(".", "Pt");
            sb.Replace("V", "Volts");

            if(name != null)
            return Enum.Parse(typeof(Range), "Bip" + sb.ToString());

            return null;
        }
    }

    /// <summary>
    /// Convert Range type to double value. Bip5Volts => (double)5.0
    /// </summary>
    class RangeToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = Enum.GetName(typeof(Range), (Range)value);
            StringBuilder sb = new StringBuilder(name);
            sb.Replace("Bip", "");
            sb.Replace("Pt", ".");
            sb.Replace("Volts", "");
            return System.Convert.ToDouble((string)parameter) * System.Convert.ToDouble(sb.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
