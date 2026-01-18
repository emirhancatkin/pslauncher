using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PsLauncher.Models;

namespace PsLauncher.Converters;

public sealed class GameCoverConverter : IValueConverter
{
    private static readonly ExeIconConverter ExeFallback = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not GameEntry game)
            return null;

        if (!string.IsNullOrWhiteSpace(game.CoverPath) && File.Exists(game.CoverPath))
            return LoadCover(game.CoverPath, parameter);

        if (!string.IsNullOrWhiteSpace(game.ExePath))
            return ExeFallback.Convert(game.ExePath, targetType, parameter, culture);

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static ImageSource? LoadCover(string path, object parameter)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri(path);
            if (parameter != null && int.TryParse(parameter.ToString(), out var decodeWidth) && decodeWidth > 0)
                bmp.DecodePixelWidth = decodeWidth;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
