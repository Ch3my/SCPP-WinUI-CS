using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    public class UpdateMenuLevelEventArgs : EventArgs
    {
        public string TargetLevel { get; }

        public UpdateMenuLevelEventArgs(string targetLevel)
        {
            TargetLevel = targetLevel;
        }
    }
}
