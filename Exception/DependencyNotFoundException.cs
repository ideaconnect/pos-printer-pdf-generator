using NetSystem = System;

namespace IDCT.Exception
{
    internal class DependencyNotFoundException : NetSystem.Exception
    {
        public DependencyNotFoundException(string message) : base(message)
        {

        }
    }
}
