using NetSystem = System;

namespace IDCT.Exception
{
    internal class InvalidPdfParametersException: NetSystem.Exception
    {
        public InvalidPdfParametersException(string message) : base(message)
        {
        }
    }
}
