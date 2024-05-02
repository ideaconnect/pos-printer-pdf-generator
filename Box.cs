using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSPrinterPdfGenerator
{
    public class Box(XUnit width, XUnit height)
    {
        public XUnit Width { get; private set; } = width;

        public XUnit Height { get; private set; } = height;
    }
}
