using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewFaceTest.Extensions
{
    public static class BitmapExtension
    {
        public static Bitmap DeepClone(this Bitmap source)
        {
            return source.Clone(new Rectangle(0, 0, source.Width, source.Height), source.PixelFormat);
        }
    }
}
