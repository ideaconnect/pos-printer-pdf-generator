using PdfSharp.Drawing;

namespace IDCT.Type
{
    public class Box
    {
        public XUnit Width { get; private set; }

        public XUnit Height { get; private set; }
        public Box(XUnit width, XUnit height)
        {
            Width = width; 
            Height = height;
        }        
    }
}
