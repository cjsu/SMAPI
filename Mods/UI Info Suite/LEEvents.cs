using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite
{
    public interface LEEvents
    {
        event EventHandler OnXPChanged;
        void raiseEvent();
    }
}
