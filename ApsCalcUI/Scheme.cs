using System;
using System.Collections.Generic;

namespace ApsCalcUI
{
    public class Scheme
    {
        /// <summary>
        /// Stores the armor configuration and runs calculations for pendepth
        /// </summary>
        public Scheme() { }

        // List of layers
        public List<Layer> LayerList { get; set; } = [];

        // Maximum useful AC
        public float MaxAC { get; set; }


        /// <summary>
        /// Calculates AC of each layer, taking into account structural bonus
        /// </summary>
        public void CalculateLayerAC()
        {
            // Add structural bonus, if applicable
            for (int layerIndex = 0; layerIndex < LayerList.Count - 1; layerIndex++)
            {
                Layer currentLayer = LayerList[layerIndex];
                Layer nextLayer = LayerList[layerIndex + 1];
                currentLayer.AC = nextLayer.GivesACBonus ? currentLayer.RawAC + nextLayer.ACBonus : currentLayer.RawAC;

                // Update max useful AC
                MaxAC = Math.Max(MaxAC, currentLayer.AC);
            }

            // Last layer is left at default
            LayerList[^1].AC = LayerList[^1].RawAC;
        }


        /// <summary>
        /// Calculates KD required to pen armor at given AP
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
        /// Minimum value of Shell.Velocity at which the shell would penetrate this scheme.
        /// Returns 0 for an empty scheme. Returns float.PositiveInfinity if the shell has
        /// zero AP or zero KD coefficient (no velocity will pen).
        /// Assumes shell modifiers (OverallVelocityModifier, OverallKineticDamageModifier,
        /// OverallArmorPierceModifier, EffectiveProjectileModuleCount) and
        /// per-layer AC have already been computed by the caller.
        /// </summary>
        public float CalculateMinVelocityToPenetrate(Shell shell, float impactAngleFromPerpendicularDegrees)
        {
            if (LayerList.Count == 0) return 0f;

            // AP / V
            float alpha = shell.OverallArmorPierceModifier * 0.0175f;
            // KD / V
            float beta = shell.GaugeMultiplier
                       * shell.EffectiveProjectileModuleCount
                       * shell.OverallKineticDamageModifier
                       * 0.16f
                       * Shell.ApsModifier;
            if (shell.HeadModule != Module.HollowPoint)
            {
                beta *= MathF.Pow(500f / MathF.Max(shell.Gauge, 100f), 0.15f);
            }

            if (alpha <= 0f || beta <= 0f) return float.PositiveInfinity;

            // KD-path coefficients (with angle factor, layer.AC)
            float angleDivisor = shell.HeadModule == Module.SabotHead ? 240f : 180f;
            int layerCount = LayerList.Count;
            float[] hpArray = new float[layerCount];
            float[] acArray = new float[layerCount];
            float baseAngle = 0f;
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                Layer layer = LayerList[layerIndex];
                if (!layer.GivesACBonus) baseAngle = layer.BaseAngle;
                hpArray[layerIndex] = layer.HP / MathF.Abs(MathF.Cos((impactAngleFromPerpendicularDegrees + baseAngle) * MathF.PI / angleDivisor));
                acArray[layerIndex] = layer.AC;
            }
            float vKD = SolveMinPenVelocity(hpArray, acArray, alpha, beta);

            // HollowPoint also has a thump-destroy path (no angle factor, RawAC)
            if (shell.HeadModule == Module.HollowPoint)
            {
                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    hpArray[layerIndex] = LayerList[layerIndex].HP;
                    acArray[layerIndex] = LayerList[layerIndex].RawAC;
                }
                float vThump = SolveMinPenVelocity(hpArray, acArray, alpha, beta);
                return MathF.Min(vKD, vThump);
            }

            return vKD;
        }

        private static float SolveMinPenVelocity(float[] hpArray, float[] acArray, float alpha, float beta)
        {
            int layerCount = hpArray.Length;

            // Sort layer indices by AC ascending so breakpoints v_k = ac[idx[k]] / alpha increase
            int[] idx = new int[layerCount];
            for (int i = 0; i < layerCount; i++) idx[i] = i;
            Array.Sort(idx, (a, b) => acArray[a].CompareTo(acArray[b]));

            // Initial sums at k = 0 (no layers activated)
            float S = 0f;
            float T = 0f;
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++) T += hpArray[layerIndex] * acArray[layerIndex];

            for (int k = 0; k <= layerCount; k++)
            {
                float vCandidate = (S + MathF.Sqrt(S * S + 4f * beta * T / alpha)) / (2f * beta);

                float lo = k == 0 ? 0f : acArray[idx[k - 1]] / alpha;
                float hi = k == layerCount ? float.PositiveInfinity : acArray[idx[k]] / alpha;

                if (vCandidate >= lo && vCandidate <= hi)
                {
                    return vCandidate;
                }

                if (k < layerCount)
                {
                    int j = idx[k];
                    S += hpArray[j];
                    T -= hpArray[j] * acArray[j];
                }
            }

            // Monotonicity guarantees one of the intervals matches; this is unreachable.
            return float.PositiveInfinity;
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
