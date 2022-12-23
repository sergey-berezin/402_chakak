using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppCLient.ViewModels
{
    public struct ComparisonResult
    {
        public float distance { get; set; }
        public float similarity { get; set; }

        public override string ToString() => $"distance: {distance:f2}; similarity: {similarity:f2}";
    }
}
