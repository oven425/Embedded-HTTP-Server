using QNetwork.Http.Server.Accept;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_Server_Http.ValueConvert
{
    public class CQListenStates2Brush : IValueConverter
    {
        public Brush Closed { set; get; }
        public Brush Opening { set; get; }
        public Brush Fail { set; get; }
        public Brush Normal { set; get; }
        public CQListenStates2Brush()
        {
            this.Closed = Brushes.Gray;
            this.Opening = Brushes.Orange;
            this.Normal = Brushes.Green;
            this.Fail = Brushes.Red;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListenStates state = (ListenStates)value;
            Brush br = this.Closed;
            switch (state)
            {
                case ListenStates.Fail:
                    {
                        br = this.Fail;
                    }
                    break;
                case ListenStates.Normal:
                    {
                        br = this.Normal;
                    }
                    break;
                case ListenStates.Opening:
                    {
                        br = this.Opening;
                    }
                    break;
                default:
                    {
                        br = this.Closed;
                    }
                    break;
            }
            return br;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CQObject2Name : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = "";
            if(value != null)
            {
                name = value.GetType().Name;
            }
            return name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
