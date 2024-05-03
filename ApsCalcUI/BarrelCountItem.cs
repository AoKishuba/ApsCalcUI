using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    class BarrelCountItem
    {
        public int ID { get; }
        public string Text { get; }

        public BarrelCountItem(int id, string text)
        {
            ID = id;
            Text = text;
        }
    }
}
