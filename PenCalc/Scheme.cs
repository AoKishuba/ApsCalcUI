using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PenCalc
{
    public class Scheme
    {
        /// <summary>
        /// Stores the armor configuration and runs calculations for pendepth
        /// </summary>
        public Scheme() { }

        // List of layers
        public List<Layer> LayerList { get; set; } = new();

        // Maximum useful AC
        public float MaxAC { get; set; }


        /// <summary>
        /// Calculates AC of each layer, taking into account structural bonuses
        /// </summary>
        public void CalculateLayerAC()
        {
            // Add structural bonus, if applicable
            for (int layerIndex = 0; layerIndex < LayerList.Count - 1; layerIndex++)
            {
                if (LayerList[layerIndex + 1].GivesACBonus)
                {
                    LayerList[layerIndex].AC = LayerList[layerIndex].RawAC + LayerList[layerIndex + 1].ACBonus;
                }
                else
                {
                    LayerList[layerIndex].AC = LayerList[layerIndex].RawAC;
                }

                // Update max useful AC
                MaxAC = Math.Max(MaxAC, LayerList[layerIndex].AC);
            }

            // Last layer is left at default
        }


        /// <summary>
        /// Retrieves KD required to pen armor at given AP
        /// </summary>
        /// <param name="ap">AP of incoming shell</param>
        /// <param name="impactAngle">Impact angle of incoming shell from perpendicular, in °</param>
        /// <param name="shellIsSabotHead">Whether incoming shell has Sabot head (for effective angle bonus)</param>
        /// <returns>Required KD to pen</returns>
        public float GetRequiredKD(float ap, float impactAngle, bool shellIsSabotHead)
        {
            float requiredKD = 0;

            if (LayerList.Count > 0)
            {
                float baseAngle = 0;
                foreach (Layer layer in LayerList)
                {
                    // Angle only resets at airgaps
                    if (!layer.GivesACBonus)
                    {
                        baseAngle = layer.BaseAngle;
                    }

                    float hpMultiplier = Math.Max(1, layer.AC / ap);
                    if (!shellIsSabotHead)
                    {
                        // Cos uses radian, angles given in deg
                        requiredKD += layer.HP / MathF.Abs(MathF.Cos((impactAngle + baseAngle) * MathF.PI / 180f)) * hpMultiplier;
                    }
                    else
                    {
                        // Sabot head uses 3/4 effective impact angle; baked into deg → rad conversion
                        requiredKD += layer.HP / MathF.Abs(MathF.Cos((impactAngle + baseAngle) * MathF.PI / 240f)) * hpMultiplier;
                    }
                }
            }

            return requiredKD;
        }


        /// <summary>
        /// Calculates thump damage required to destroy all armor at given AP
        /// </summary>
        /// <param name="ap">AP of incoming shell</param>
        /// <returns>Required thump damage to destroy entire scheme</returns>
        public float GetRequiredThump(float ap)
        {
            float requiredTD = 0;

            if (LayerList.Count > 0)
            {
                foreach (Layer layer in LayerList)
                {
                    float hpMultiplier = Math.Max(1, layer.RawAC / ap);
                    requiredTD += layer.HP * hpMultiplier;
                }
            }
            return requiredTD;
        }
    }
}
