using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSPrinterPdfGenerator
{
    internal class DependencyNotFoundException(string message) : Exception(message)
    {
    }
}
