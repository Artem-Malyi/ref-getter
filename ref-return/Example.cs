using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefReturn
{
    //
    // Assume this class is defined in a different assembly and is not known at compile time
    //
    class Example
    {
#pragma warning disable CS0414 // The field is assigned but its value is never used
        private static bool TheFlag;
#pragma warning restore CS0414 // The field is assigned but its value is never used

        internal Example(bool flag)
        {
            TheFlag = flag;
        }
    }
}
