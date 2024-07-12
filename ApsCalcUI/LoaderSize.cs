using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    /// <summary>
    /// Contains information about each loader size, including top shell in that size for current calculator
    /// </summary>
    /// <param name="minLengthExclusive">Shortest length accommodated by loader, exclusive</param>
    /// <param name="maxLengthInclusive">Nominal loader length, inclusive</param>
    internal class LoaderSize (string name, float minLengthExclusive, float maxLengthInclusive)
    {
        public string Name { get { return name; } }
        public float MinLength { get { return minLengthExclusive; } }
        public float MaxLength { get { return maxLengthInclusive; } }
        public Shell TopShell { get; set; } = new(default, default, default, default, default, default, default,
            default, default, default, default, default, default, default, default);
    }
}
