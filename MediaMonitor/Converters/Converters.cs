using MediaMonitor.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MediaMonitor.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaStatus status)
            {
                switch (status)
                {
                    case MediaStatus.Completed:
                        return new SolidColorBrush(Color.FromRgb(34, 139, 58));
                    case MediaStatus.Watching:
                        return new SolidColorBrush(Color.FromRgb(9, 105, 218));
                    case MediaStatus.Planned:
                        return new SolidColorBrush(Color.FromRgb(180, 110, 0));
                    case MediaStatus.Dropped:
                        return new SolidColorBrush(Color.FromRgb(240, 128, 110));
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) 
            => throw new NotImplementedException();
    }
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaType mediaType)
            {
                return mediaType switch
                {
                    MediaType.Movie => new SolidColorBrush(Color.FromRgb(128, 176, 232)),
                    MediaType.Series => new SolidColorBrush(Color.FromRgb(220, 150, 150)),
                    MediaType.Anime => new SolidColorBrush(Color.FromRgb(0, 180, 140)),
                    MediaType.Game => new SolidColorBrush(Color.FromRgb(200, 195, 80)),
                    MediaType.Other => new SolidColorBrush(Color.FromRgb(180, 170, 220)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RatingToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating && rating > 0)
                return rating.ToString("0.#");

            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = value == null || (value is string str && string.IsNullOrWhiteSpace(str));

            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bv && bv;
            bool inverse = parameter is string p && p == "inverse";
            if (inverse) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Показывает элемент только если список/строка не пусты (используется для тегов).
    public class CollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = value switch
            {
                null => 0,
                System.Collections.ICollection collection => collection.Count,
                System.Collections.IEnumerable enumerable => CountEnumerable(enumerable),
                _ => 0
            };

            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static int CountEnumerable(System.Collections.IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Проверяет, содержится ли тег (values[1]) в выбранных фильтрах-тегах (values[0]).
    public class TagsContainConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is System.Collections.IEnumerable list &&
                values[1] is string tag)
            {
                foreach (var entry in list)
                {
                    if (entry is string s && string.Equals(s, tag, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PercentToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = value is double d ? Math.Clamp(d, 0, 100) : 0;
            bool inverse = parameter is string p && p == "inverse";
            double result = inverse ? 100 - percent : percent;
            return new GridLength(Math.Max(result, 0.001), GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class IsFilterSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Length == 2 && values[0] is string firstValue && values[1] is string secondValue &&
                   firstValue == secondValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
