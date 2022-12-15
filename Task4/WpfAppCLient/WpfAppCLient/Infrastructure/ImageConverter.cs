using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfAppCLient.Infrastructure
{
    internal class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imagePaths = value as string[];
            if (imagePaths is not null)
            {

                List<BitmapImage> images = new List<BitmapImage>();

                foreach (var imagePath in imagePaths)
                {
                    var image = Image.FromFile(imagePath);

                    var bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    MemoryStream memoryStream = new MemoryStream();
                    image.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    bitmap.StreamSource = memoryStream;
                    bitmap.EndInit();

                    images.Add(bitmap);
                }

                return images;
            }

            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}
