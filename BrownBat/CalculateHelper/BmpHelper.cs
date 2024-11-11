using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace BrownBat.CalculateHelper
{
    public static class BmpHelper
    {
        public static bool GetBitmapFromFile(this string FilePath, out Bitmap bitmap)
        {
            bitmap = (Bitmap)null;
            if (!Path.HasExtension(FilePath))
                return false;
            switch (Path.GetExtension(FilePath))
            {
                case ".bmp":
                case ".gif":
                case ".jfif":
                case ".jpeg":
                case ".jpg":
                case ".png":
                case ".tif":
                case ".tiff":
                    bitmap = (Bitmap)Image.FromFile(FilePath);
                    return bitmap != null;
                default:
                    return false;
            }
        }
    }
}
