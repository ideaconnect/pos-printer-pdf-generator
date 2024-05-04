using NetSystem = System;

namespace IDCT.Exception
{
    internal class InvalidLicenseException : NetSystem.Exception
    {
        public InvalidLicenseException(string message) : base(message)
        {

        }
    }
}
