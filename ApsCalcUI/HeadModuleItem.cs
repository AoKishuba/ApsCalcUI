using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    class HeadModuleItem
    {
        public int Index { get; }
        public string Name { get; }

        public HeadModuleItem(int index, string name)
        {
            Index = index;
            Name = name;
        }


        /// <summary>
        /// Creates a list of all modules which can be used as heads
        /// </summary>
        public static IEnumerable<HeadModuleItem> GenerateHeadList()
        {
            for (int index = 0; index < Module.AllModules.Length; index++)
            {
                if (Module.AllModules[index].ModulePosition == Module.Position.Head
                    || (Module.AllModules[index].ModulePosition == Module.Position.Middle && Module.AllModules[index].CanBeVariable))
                {
                    HeadModuleItem head = new(index,Module.AllModules[index].Name);

                    yield return head;
                }
            }
        }
    }
}
