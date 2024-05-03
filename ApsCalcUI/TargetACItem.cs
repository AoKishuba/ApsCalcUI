using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    class TargetACItem
    {
        public float ID { get; }
        public string Text { get; }

        public TargetACItem(float id, string text)
        {
            ID = id;
            Text = text;
        }
    }
}
