using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
//using Android.Graphics;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace NeospectraMauiDemo.Driver
{
    public class ImageUtil
    {
        public static SKBitmap convert(string base64Str)
        {
            byte[] decodedBytes = Convert.FromBase64String(
                base64Str.Substring(base64Str.IndexOf(",") + 1));


            var bitmap = SKBitmap.Decode(decodedBytes);
            return bitmap;

        }
        public static string convert(byte[] data)
        {
            string base64String = Convert.ToBase64String(data, 0, data.Length);
            var img = "data:image/png;base64," + base64String;
            return img;
        }
        //public static string convert(Bitmap bitmap)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        bitmap.Save(ms, ImageFormat.Png);
        //        byte[] imageBytes = ms.ToArray();

        //        return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
        //    }
        //}
    }
}
