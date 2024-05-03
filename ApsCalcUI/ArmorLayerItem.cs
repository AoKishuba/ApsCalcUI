using PenCalc;

namespace ApsCalcUI
{
    class ArmorLayerItem
    {
        public string Name { get; }
        public Layer Layer { get; }

        public ArmorLayerItem(string name, Layer layer)
        {
            Name = name;
            Layer = layer;
        }
    }
}
