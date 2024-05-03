using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    class VariableModuleItem
    {
        public int Index { get; }
        public string Name { get; }

        public VariableModuleItem(int index, string name)
        {
            Index = index;
            Name = name;
        }


        /// <summary>
        /// Creates a list of all variable modules
        /// </summary>
        public static IEnumerable<VariableModuleItem> GenerateBodyList()
        {
            for (int index = 0; index < Module.AllModules.Length; index++)
            {
                if (Module.AllModules[index].CanBeVariable)
                {
                    VariableModuleItem varMod = new(index, Module.AllModules[index].Name);

                    yield return varMod;
                }
            }
        }
    }
}
