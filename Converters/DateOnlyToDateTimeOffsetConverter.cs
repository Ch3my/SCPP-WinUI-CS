using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    class DateOnlyToDateTimeOffsetConverter : IValueConverter
    {
        // Converter para poder hacer Binding desde un DateOnly a un CalendarPicker que solo recibe DateTimeOffset
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateOnly date)
            {
                // Se incluye el offset local sino el Calendar muestra un dia menos UTC - 3 hrs es ayer 
                return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow));
            }

            return null;
        }
        public object Convert(object value)
        {
            if (value is DateOnly date)
            {
                // Se incluye el offset local sino el Calendar muestra un dia menos UTC - 3 hrs es ayer 
                return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeZoneInfo.Local.BaseUtcOffset);
                // Esto daba una Hora menos en horario de verano, quiza temporalmente
                //return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                return new DateOnly(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day);
            }

            return null;
        }
    }
}
