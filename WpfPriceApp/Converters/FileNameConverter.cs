using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace WpfPriceApp.Converters
{
    /// <summary>전체 경로에서 확장자를 뺀 파일명만 표시.</summary>
    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string path ? Path.GetFileNameWithoutExtension(path) : value ?? "";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
