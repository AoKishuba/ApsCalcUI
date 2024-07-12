using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace ApsCalcUI
{
    public struct ModuleConfig
    {
        public int HeadIndex;
        public float[] BodyModCounts;
        public float GPMax;
        public float RGCount;
    }

    // Damage types.  Enum is faster than strings.
    public enum DamageType : int
    {
        Kinetic,
        EMP,
        Frag,
        HE,
        HEAT,
        Incendiary,
        Disruptor,
        MD,
        Smoke
    }

    // Barrel length limit parameter
    public enum BarrelLengthLimit : int
    {
        FixedLength,
        Calibers
    }

    public enum TestType : int
    {
        DpsPerVolume = 0,
        DpsPerCost = 1
    }

    public class ShellCalc
    {
        /// <summary>
        /// Takes shell parameters and calculates performance of shell permutations.
        /// </summary>
        /// <param name="barrelCount">Number of barrels</param>
        /// <param name="gauge">Desired gauge in mm</param>
        /// <param name="gaugeMultiplier">(gauge / 500mm)^1.8; used for many calculations</param>
        /// <param name="headList">List of module indices for every module to be used as head</param>
        /// <param name="baseModule">The special base module, if any</param>
        /// <param name="fixedModuleCounts">An array of integers representing number of shells at that index in module list</param>
        /// <param name="fixedModuleTotal">Minimum number of modules on every shell</param>
        /// <param name="variableModuleIndices">Module indices of modules to be used in varying numbers in testing</param>
        /// <param name="regularClipsPerLoader">Directly-connected clips for each regular loader</param>
        /// <param name="regularInputsPerLoader">Ammo inputs per regular loader/clip cluster</param>
        /// <param name="beltfedClipsPerLoader">Clips per beltfed loader</param>
        /// <param name="beltfedInputsPerLoader">Inputs per beltfed loader/clip cluster</param>
        /// <param name="usesAmmoEjector">Whether loader cluster uses ammo ejector</param>
        /// <param name="maxGPInput">Max desired number of gunpowder casings</param>
        /// <param name="gpIncrement">Amount of GP added between tests</param>
        /// <param name="maxRGInput">Max desired number of railgun casings</param>
        /// <param name="minShellLengthInput">Min desired shell length in mm, exclusive</param>
        /// <param name="maxShellLengthInput">Max desired shell length in mm, inclusive</param>
        /// <param name="maxDrawInput">Max desired rail draw</param>
        /// <param name="maxRecoilInput">Max desired recoil, including rail and GP</param>
        /// <param name="minVelocityInput">Min desired velocity</param>
        /// <param name="minEffectiveRangeInput">Min desired effective range</param>
        /// <param name="impactAngleFromPerpendicularDegrees">Angle of impact from perpendicular, in °</param>
        /// <param name="sabotAngleMultiplier">KD multiplier from impact angle for sabot head</param>
        /// <param name="nonSabotAngleMultiplier">KD multiplier from impact angle</param>
        /// <param name="targetAC">Armor class of target for kinetic damage calculations</param>
        /// <param name="damageType">DamageType Enum, determines which damage type is optimized</param>
        /// <param name="fragConeAngle">Frag cone angle in °</param>
        /// <param name="fragAngleMultiplier">(2 + sqrt(angle °)) / 16</param>
        /// <param name="minDisruptor">Minimum allowed disruptor shield reduction</param>
        /// <param name="targetArmorScheme">Target armor scheme</param>
        /// <param name="testType">0 for DPS per volume, 1 for DPS per cost</param>
        /// <param name="testIntervalMinutes">Test interval in min</param>
        /// <param name="storagePerVolume">Material storage per container volume</param>
        /// <param name="storagePerCost">Material storage per container cost</param>
        /// <param name="enginePpm">Engine power per material</param>
        /// <param name="enginePpv">Engine power per volume</param>
        /// <param name="enginePpc">Engine power per block cost</param>
        /// <param name="engineUsesFuel">Whether engine uses special Fuel storage</param>
        /// <param name="firingPieceIsDif">Whether gun is using Direct Input Feed</param>
        /// <param name="gunUsesRecoilAbsorbers">Whether gun uses recoil absorbers; less inaccuracy, higher cost and volume</param>
        /// <param name="maxInaccuracy">Max allowed inaccuracy within barrel length limits</param>
        /// <param name="rateOfFireRpm">Rate of fire in rounds per minute</param>
        /// <param name="limitBarrelLength">Whether to limit max barrel length</param>
        /// <param name="maxBarrelLength">Max barrel length in m or calibers</param>
        /// <param name="barrelLengthLimitType">Whether to limit barrel length by m or calibers (multiples of gauge)</param>
        /// <param name="verboseOutputIsChecked">Whether to show volume and cost numbers for debugging/details</param>
        /// <param name="rawNumberOutputIsChecked">Do not round numbers in output if true</param>
        /// <param name="columnDelimiter">Character used to separate columns in .csv output; either comma or semicolon</param>
        public ShellCalc(
            int barrelCount,
            float gauge,
            float gaugeMultiplier,
            List<int> headList,
            Module baseModule,
            float[] fixedModuleCounts,
            float fixedModuleTotal,
            int[] variableModuleIndices,
            int regularClipsPerLoader,
            int regularInputsPerLoader,
            int beltfedClipsPerLoader,
            int beltfedInputsPerLoader,
            bool usesAmmoEjector,
            float maxGPInput,
            float gpIncrement,
            float maxRGInput,
            float minShellLengthInput,
            float maxShellLengthInput,
            float maxDrawInput,
            float maxRecoilInput,
            float minVelocityInput,
            float minEffectiveRangeInput,
            float impactAngleFromPerpendicularDegrees,
            float sabotAngleMultiplier,
            float nonSabotAngleMultiplier,
            float targetAC,
            DamageType damageType,
            float fragConeAngle,
            float fragAngleMultiplier,
            float minDisruptor,
            Scheme targetArmorScheme,
            TestType testType,
            int testIntervalMinutes,
            float storagePerVolume,
            float storagePerCost,
            float enginePpm,
            float enginePpv,
            float enginePpc,
            bool engineUsesFuel,
            bool firingPieceIsDif,
            bool gunUsesRecoilAbsorbers,
            float maxInaccuracy,
            float rateOfFireRpm,
            bool limitBarrelLength,
            float maxBarrelLength,
            BarrelLengthLimit barrelLengthLimitType,
            bool verboseOutputIsChecked,
            bool rawNumberOutputIsChecked,
            char columnDelimiter
            )
        {
            BarrelCount = barrelCount;
            Gauge = gauge;
            GaugeMultiplier = gaugeMultiplier;
            HeadList = headList;
            BaseModule = baseModule;
            FixedModuleCounts = fixedModuleCounts;
            FixedModuleTotal = fixedModuleTotal;
            VariableModuleIndices = variableModuleIndices;
            RegularClipsPerLoader = regularClipsPerLoader;
            RegularInputsPerLoader = regularInputsPerLoader;
            BeltfedClipsPerLoader = beltfedClipsPerLoader;
            BeltfedInputsPerLoader = beltfedInputsPerLoader;
            UsesAmmoEjector = usesAmmoEjector;
            MaxGPInput = maxGPInput;
            GPIncrement = gpIncrement;
            MaxRGInput = maxRGInput;
            MinShellLength = minShellLengthInput;
            MaxShellLength = maxShellLengthInput;
            MaxDrawInput = maxDrawInput;
            MaxRecoilInput = maxRecoilInput;
            MinVelocityInput = minVelocityInput;
            MinEffectiveRangeInput = minEffectiveRangeInput;
            ImpactAngleFromPerpendicularDegrees = impactAngleFromPerpendicularDegrees;
            SabotAngleMultiplier = sabotAngleMultiplier;
            NonSabotAngleMultiplier = nonSabotAngleMultiplier;
            TargetAC = targetAC;
            DamageType = damageType;
            FragConeAngle = fragConeAngle;
            FragAngleMultiplier = fragAngleMultiplier;
            MinDisruptor = minDisruptor;
            TargetArmorScheme = targetArmorScheme;
            TestType = testType;
            TestIntervalMinutes = testIntervalMinutes;
            TestIntervalSeconds = testIntervalMinutes * 60;
            StoragePerVolume = storagePerVolume;
            StoragePerCost = storagePerCost;
            EnginePpm = enginePpm;
            EnginePpv = enginePpv;
            EnginePpc = enginePpc;
            EngineUsesFuel = engineUsesFuel;
            FiringPieceIsDif = firingPieceIsDif;
            GunUsesRecoilAbsorbers = gunUsesRecoilAbsorbers;
            MaxInaccuracy = maxInaccuracy;
            RateOfFireRpm = rateOfFireRpm;
            LimitBarrelLength = limitBarrelLength;
            if (limitBarrelLength && barrelLengthLimitType == BarrelLengthLimit.Calibers)
            {
                MaxBarrelLengthInCalibers = maxBarrelLength;
                MaxBarrelLengthInM = maxBarrelLength * gauge / 1000f;
            }
            else if (limitBarrelLength && barrelLengthLimitType == BarrelLengthLimit.FixedLength)
            {
                MaxBarrelLengthInM = maxBarrelLength;
            }
            BarrelLengthLimitType = barrelLengthLimitType;

            if (LimitBarrelLength)
            {
                MaxGP = MathF.Min(maxGPInput, MaxBarrelLengthInM / 2.2f / MathF.Pow(Gauge / 1000f, 0.55f));
            }
            else
            {
                MaxGP = maxGPInput;
            }
            VerboseOutputIsChecked = verboseOutputIsChecked;
            RawNumberOutputIsChecked = rawNumberOutputIsChecked;
            ColumnDelimiter = columnDelimiter;
        }

        public int BarrelCount { get; }
        public float Gauge { get; }
        public float GaugeMultiplier { get; }
        public List<int> HeadList { get; }
        public Module BaseModule { get; }
        public float[] FixedModuleCounts { get; }
        public float FixedModuleTotal { get; }
        public int[] VariableModuleIndices { get; }
        public int RegularClipsPerLoader { get; }
        public int RegularInputsPerLoader { get; }
        public int BeltfedClipsPerLoader { get; }
        public int BeltfedInputsPerLoader { get; }
        public bool UsesAmmoEjector { get; }
        public float MaxGPInput { get; }
        public float MaxGP { get; }
        public float GPIncrement { get; }
        public float MaxRGInput { get; }
        public float MinShellLength { get; }
        public float MaxShellLength { get; }
        public float MaxDrawInput { get; }
        public float MaxRecoilInput { get; }
        public float MinVelocityInput { get; }
        public float MinEffectiveRangeInput { get; }
        public float ImpactAngleFromPerpendicularDegrees { get; }
        public float SabotAngleMultiplier { get; }
        public float NonSabotAngleMultiplier { get; }
        public float TargetAC { get; }
        public DamageType DamageType { get; }
        public float FragConeAngle { get; }
        public float FragAngleMultiplier { get; }
        public float MinDisruptor { get; }
        public Scheme TargetArmorScheme { get; }
        public TestType TestType { get; }
        public int TestIntervalMinutes { get; }
        public int TestIntervalSeconds { get; }
        public float StoragePerVolume { get; }
        public float StoragePerCost { get; }
        public float EnginePpm { get; }
        public float EnginePpv { get; }
        public float EnginePpc { get; }
        public bool EngineUsesFuel { get; }
        public bool FiringPieceIsDif { get; }
        public bool GunUsesRecoilAbsorbers { get; }
        public float MaxInaccuracy { get; }
        public float RateOfFireRpm { get; }
        public bool LimitBarrelLength { get; }
        public float MaxBarrelLengthInM { get; }
        public float MaxBarrelLengthInCalibers { get; }
        public BarrelLengthLimit BarrelLengthLimitType { get; }
        public bool VerboseOutputIsChecked { get; }
        public bool RawNumberOutputIsChecked { get; }
        public char ColumnDelimiter { get; }


        // Store top-DPS shells by loader length
        readonly LoaderSize Belt = new("1m (belt)", 0, 1000f);
        readonly LoaderSize Dif = new("DIF", 0, 10_000f);
        readonly LoaderSize[] RegularSizes =
        [
            new("1m", 0f, 1000f),
            new("2m", 1000f, 2000f),
            new("3m", 2000f, 3000f),
            new("4m", 3000f, 4000f),
            new("5m", 4000f, 5000f),
            new("6m", 5000f, 6000f),
            new("7m", 6000f, 7000f),
            new("8m", 7000f, 8000f)
        ];

        public Dictionary<string, Shell> TopDpsShells { get; set; } = [];
        public List<Shell> TopShellsLocal { get; set; } = [];

        private IEnumerable<ModuleConfig> GenerateModConfigs()
        {
            float maxModuleCount = 20f - FixedModuleTotal;
            for (int headIndex = 0; headIndex < HeadList.Count; headIndex++)
            {
                float rgMax = MathF.Min(MaxRGInput, maxModuleCount);
                for (float rgCount = 0; rgCount <= rgMax; rgCount++)
                {
                    float var0Max = maxModuleCount - rgCount;
                    for (float var0Count = 0; var0Count <= var0Max; var0Count++)
                    {
                        float var1Max = VariableModuleIndices[1] == VariableModuleIndices[0] ?
                            0 : maxModuleCount - rgCount - var0Count;
                        for (float var1Count = 0; var1Count <= var1Max; var1Count++)
                        {
                            float var2Max = VariableModuleIndices[2] == VariableModuleIndices[0] ?
                                0 : maxModuleCount - rgCount - var0Count - var1Count;
                            for (float var2Count = 0; var2Count <= var2Max; var2Count++)
                            {
                                float var3Max = VariableModuleIndices[3] == VariableModuleIndices[0] ?
                                    0 : maxModuleCount - rgCount - var0Count - var1Count - var2Count;
                                for (float var3Count = 0; var3Count <= var3Max; var3Count++)
                                {
                                    float var4Max = VariableModuleIndices[4] == VariableModuleIndices[0] ?
                                        0 : maxModuleCount - rgCount - var0Count - var1Count - var2Count - var3Count;
                                    for (float var4Count = 0; var4Count <= var4Max; var4Count++)
                                    {
                                        float var5Max = VariableModuleIndices[5] == VariableModuleIndices[0] ?
                                            0 : 
                                            maxModuleCount
                                            - rgCount
                                            - var0Count
                                            - var1Count
                                            - var2Count
                                            - var3Count
                                            - var4Count;
                                        for (float var5Count = 0; var5Count <= var5Max; var5Count++)
                                        {
                                            float var6Max = VariableModuleIndices[6] == VariableModuleIndices[0] ?
                                                0 :
                                                maxModuleCount
                                                - rgCount
                                                - var0Count
                                                - var1Count
                                                - var2Count
                                                - var3Count
                                                - var4Count
                                                - var5Count;
                                            for (float var6Count = 0; var6Count <= var6Max; var6Count++)
                                            {
                                                float var7Max = VariableModuleIndices[7] == VariableModuleIndices[0] ?
                                                    0 :
                                                    maxModuleCount
                                                    - rgCount
                                                    - var0Count
                                                    - var1Count
                                                    - var2Count
                                                    - var3Count
                                                    - var4Count
                                                    - var5Count
                                                    - var6Count;
                                                for (float var7Count = 0; var7Count <= var7Max; var7Count++)
                                                {
                                                    float var8Max = VariableModuleIndices[8] == VariableModuleIndices[0] ?
                                                        0 :
                                                        maxModuleCount
                                                        - rgCount
                                                        - var0Count
                                                        - var1Count
                                                        - var2Count
                                                        - var3Count
                                                        - var4Count
                                                        - var5Count
                                                        - var6Count
                                                        - var7Count;
                                                    for (float var8Count = 0; var8Count <= var8Max; var8Count++)
                                                    {
                                                        float gpMax = MathF.Min(MaxGP,
                                                            maxModuleCount
                                                            - rgCount
                                                            - var0Count
                                                            - var1Count
                                                            - var2Count
                                                            - var3Count
                                                            - var4Count
                                                            - var5Count
                                                            - var6Count
                                                            - var7Count
                                                            - var8Count);
                                                        yield return new ModuleConfig
                                                        {
                                                            GPMax = gpMax,
                                                            RGCount = rgCount,
                                                            HeadIndex = HeadList[headIndex],
                                                            BodyModCounts =
                                                            [
                                                                var0Count,
                                                                var1Count,
                                                                var2Count,
                                                                var3Count,
                                                                var4Count,
                                                                var5Count,
                                                                var6Count,
                                                                var7Count,
                                                                var8Count
                                                            ]
                                                        };
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates minimum GP needed to achieve given velocity and effective range
        /// </summary>
        public float CalculateMinimumGPForVelocityandRange(Shell shellUnderTesting)
        {
            shellUnderTesting.RailDraw = GunUsesRecoilAbsorbers ?
                MathF.Min(shellUnderTesting.MaxDraw, MaxDrawInput)
                : MathF.Min(MathF.Min(MaxRecoilInput, shellUnderTesting.MaxDraw), MaxDrawInput);

            shellUnderTesting.CalculateRecoil();
            // Calculate effective time
            float gravityCompensatorCount = 0;
            int modIndex = 0;
            foreach (float modCount in shellUnderTesting.BodyModuleCounts)
            {
                if (Module.AllModules[modIndex] == Module.GravCompensator)
                {
                    gravityCompensatorCount = shellUnderTesting.BodyModuleCounts[modIndex];
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            float effectiveTime =
                10f
                * shellUnderTesting.OverallVelocityModifier
                * (shellUnderTesting.ProjectileLength / 1000f)
                * (1f + gravityCompensatorCount);

            // Determine whether range or velocity is limiting factor
            float minVelocity = MathF.Max(MinVelocityInput, MinEffectiveRangeInput / effectiveTime);

            // Calculate GP recoil required for either range or velocity
            float minGPRecoilForVelocity = MathF.Pow(minVelocity / shellUnderTesting.OverallVelocityModifier, 2)
                * GaugeMultiplier
                * shellUnderTesting.ProjectileLength
                / Gauge
                / 85f
                - shellUnderTesting.RailDraw;

            // Calculate min GP from recoil
            float minGPForVelocity = minGPRecoilForVelocity / GaugeMultiplier / 2500f;

            float minGP = MathF.Max(0, minGPForVelocity);

            return minGP;
        }


        void OptimizeGPAndRail(Shell shellUnderTesting,
            float minGP,
            float maxGP,
            Dictionary<DamageType, float> referenceDict)
        {
            float minDraw;
            float maxDraw;
            float optimalGP = minGP;

            // Binary search to find optimal draw without testing every value
            float midRangeLower;
            float midRangeLowerScore;
            float midRangeUpper;
            float midRangeUpperScore;
            float topOfRange = maxGP;
            float bottomOfRange = minGP;

            float maxRecoil = GunUsesRecoilAbsorbers ?
                MaxRecoilInput : shellUnderTesting.CalculateMaxDrawForInaccuracy();

            while (bottomOfRange < topOfRange)
            {
                midRangeLower = (topOfRange + bottomOfRange) / 2f;
                midRangeUpper = midRangeLower + 0.01f;

                shellUnderTesting.GPCasingCount = midRangeLower;
                shellUnderTesting.CalculateRecoil();
                minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange();
                maxDraw = GunUsesRecoilAbsorbers ?
                    MathF.Min(MathF.Min(MaxRecoilInput - shellUnderTesting.GPRecoil, shellUnderTesting.MaxDraw), MaxDrawInput)
                    : MathF.Min(MathF.Min(MathF.Min(shellUnderTesting.CalculateMaxDrawForInaccuracy(), MaxRecoilInput - shellUnderTesting.GPRecoil), shellUnderTesting.MaxDraw), MaxDrawInput);
                maxDraw = MathF.Min(maxDraw, MaxRecoilInput - shellUnderTesting.GPRecoil);
                if (minDraw > maxDraw)
                {
                    midRangeLowerScore = 0;
                }
                else
                {
                    OptimizeRailDraw(shellUnderTesting, maxDraw, minDraw, referenceDict);
                    shellUnderTesting.CalculateDpsByType(DamageType);
                    midRangeLowerScore = referenceDict[DamageType];
                }


                shellUnderTesting.GPCasingCount = midRangeUpper;
                shellUnderTesting.CalculateRecoil();
                minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange();
                maxDraw = MathF.Min(shellUnderTesting.MaxDraw, MaxDrawInput);
                maxDraw = MathF.Min(maxDraw, MaxRecoilInput - shellUnderTesting.GPRecoil);
                OptimizeRailDraw(shellUnderTesting, maxDraw, minDraw, referenceDict);
                shellUnderTesting.CalculateDpsByType(DamageType);
                midRangeUpperScore = referenceDict[DamageType];
                if (minDraw > maxDraw)
                {
                    midRangeLowerScore = 0;
                }
                else
                {
                    OptimizeRailDraw(shellUnderTesting, maxDraw, minDraw, referenceDict);
                    shellUnderTesting.CalculateDpsByType(DamageType);
                    midRangeUpperScore = referenceDict[DamageType];
                }

                if (midRangeLowerScore >= midRangeUpperScore)
                {
                    topOfRange = midRangeLower;
                    optimalGP = midRangeLower;
                }
                else
                {
                    bottomOfRange = midRangeUpper;
                    optimalGP = midRangeUpper;
                }
            }

            shellUnderTesting.GPCasingCount = optimalGP;
        }

        /// <summary>
        /// Runs nested binary search algorithm on rail draw to determine optimum for a given GP count
        /// </summary>
        /// <param name="shellUnderTesting">Shell being tested (normal or belt)</param>
        /// <param name="maxDraw">Max allowed rail draw</param>
        /// <param name="minDraw">Min allowed rail draw</param>
        /// <param name="referenceDict">DPS per Cost or DPS per Volume, depending on test type</param>
        void OptimizeRailDraw(Shell shellUnderTesting,
            float maxDraw,
            float minDraw,
            Dictionary<DamageType, float> referenceDict)
        {
            float optimalDraw = minDraw;

            if (maxDraw > 0)
            {
                // Binary search to find optimal draw without testing every value
                float midRangeLower;
                float midRangeLowerScore;
                float midRangeUpper;
                float midRangeUpperScore;
                float topOfRange = maxDraw;
                float bottomOfRange = minDraw;

                while (bottomOfRange < topOfRange)
                {
                    midRangeLower = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                    midRangeUpper = midRangeLower + 1f;

                    shellUnderTesting.RailDraw = midRangeLower;
                    shellUnderTesting.CalculateDpsByType(DamageType);
                    midRangeLowerScore = referenceDict[DamageType];

                    shellUnderTesting.RailDraw = midRangeUpper;
                    shellUnderTesting.CalculateDpsByType(DamageType);
                    midRangeUpperScore = referenceDict[DamageType];

                    if (midRangeLowerScore >= midRangeUpperScore)
                    {
                        topOfRange = midRangeLower;
                        optimalDraw = midRangeLower;
                    }
                    else
                    {
                        bottomOfRange = midRangeUpper;
                        optimalDraw = midRangeUpper;
                    }
                }
            }
            shellUnderTesting.RailDraw = optimalDraw;
        }

        /// <summary>
        /// Compares input shell to current top shells and keeps winner. Does not include belt
        /// </summary>
        /// <param name="shellUnderTesting">Shell being tested</param>
        /// <param name="referenceDict">DPS per cost or per volume depending on test type</param>
        void CompareToTopShells(Shell shellUnderTesting, Dictionary<DamageType, float> referenceDict)
        {
            // Check performance against top shells
            shellUnderTesting.CalculateVelocity();
            shellUnderTesting.CalculateEffectiveRange();
            shellUnderTesting.CalculateDpsByType(DamageType);

            if (FiringPieceIsDif)
            {
                Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                    Dif.TopShell.DpsPerVolumeDict : Dif.TopShell.DpsPerCostDict;
                if (referenceDict[DamageType] > topReferenceDict[DamageType])
                {
                    Dif.TopShell = shellUnderTesting;
                }
            }
            else if (shellUnderTesting.IsBelt)
            {
                Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                    Belt.TopShell.DpsPerVolumeDict : Belt.TopShell.DpsPerCostDict;
                if (referenceDict[DamageType] > topReferenceDict[DamageType])
                {
                    Belt.TopShell = shellUnderTesting;
                }
            }
            else
            {
                for (int loaderSizeIndex = 0; loaderSizeIndex < RegularSizes.Length; loaderSizeIndex++)
                {
                    LoaderSize topSize = RegularSizes[loaderSizeIndex];
                    if (shellUnderTesting.TotalLength > topSize.MinLength && shellUnderTesting.TotalLength <= topSize.MaxLength)
                    {
                        Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                            topSize.TopShell.DpsPerVolumeDict : topSize.TopShell.DpsPerCostDict;
                        if (referenceDict[DamageType] > topReferenceDict[DamageType])
                        {
                            topSize.TopShell = shellUnderTesting;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates over possible configurations and stores the best according to test parameters
        /// </summary>
        public void ShellTest()
        {
            // Set up target armor scheme for testing
            TargetArmorScheme.CalculateLayerAC();

            foreach (ModuleConfig modConfig in GenerateModConfigs())
            {
                Shell shellUnderTesting = new(
                    BarrelCount,
                    Gauge,
                    GaugeMultiplier,
                    false,
                    Module.AllModules[modConfig.HeadIndex],
                    BaseModule,
                    RegularClipsPerLoader,
                    RegularInputsPerLoader,
                    BeltfedClipsPerLoader,
                    BeltfedInputsPerLoader,
                    UsesAmmoEjector,
                    modConfig.RGCount,
                    RateOfFireRpm,
                    GunUsesRecoilAbsorbers,
                    FiringPieceIsDif
                    )
                {
                    TargetAC = TargetAC,
                    ImpactAngleFromPerpendicularDegrees = ImpactAngleFromPerpendicularDegrees,
                    TestIntervalSeconds = TestIntervalSeconds,
                    StoragePerVolume = StoragePerVolume,
                    StoragePerCost = StoragePerCost,
                    EnginePpm = EnginePpm,
                    EnginePpv = EnginePpv,
                    EnginePpc = EnginePpc,
                    EngineUsesFuel = EngineUsesFuel,
                    TargetArmorScheme = TargetArmorScheme,
                    MinVelocityInput = MinVelocityInput,
                    MinRangeInput = MinEffectiveRangeInput,
                    MaxBarrelLengthInM = MaxBarrelLengthInM,
                    DesiredInaccuracy = MaxInaccuracy,
                    FragAngleMultiplier = FragAngleMultiplier
                };
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                // Add variable modules
                for (int i = 0; i <  modConfig.BodyModCounts.Length; i++)
                {
                    int moduleIndex = VariableModuleIndices[i];
                    float moduleCount = modConfig.BodyModCounts[i];
                    shellUnderTesting.BodyModuleCounts[moduleIndex] += moduleCount;
                }

                shellUnderTesting.CalculateLengths();
                shellUnderTesting.CalculateRecoil();
                bool lengthWithinBounds = true;
                if (LimitBarrelLength
                    && shellUnderTesting.ProjectileLength 
                        > shellUnderTesting.CalculateMaxProjectileLengthForInaccuracy())
                {
                    lengthWithinBounds = false;
                }
                if (shellUnderTesting.TotalLength <= MinShellLength || shellUnderTesting.TotalLength > MaxShellLength)
                {
                    lengthWithinBounds = false;
                }


                if (lengthWithinBounds)
                {
                    shellUnderTesting.CalculateVelocityModifier();
                    shellUnderTesting.CalculateMaxDraw();

                    float minGP = CalculateMinimumGPForVelocityandRange(shellUnderTesting);
                    // GP already limited by inaccuracy; limit by recoil
                    float maxGP = MathF.Min(MaxGP, MaxRecoilInput / GaugeMultiplier / 2500f);

                    for (int sizeIndex = 0; sizeIndex < RegularSizes.Length; sizeIndex++)
                    {
                        LoaderSize loaderSize = RegularSizes[sizeIndex];

                    }


                    if (!shellUnderTesting.GunUsesRecoilAbsorbers)
                    {
                        maxDraw = MathF.Min(maxDraw, shellUnderTesting.CalculateMaxDrawForInaccuracy());
                    }
                    float minDraw = shellUnderTesting.CalculateMinimumDrawForVelocityandRange();

                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime();
                        shellUnderTesting.CalculateDamageModifierByType(DamageType);
                        shellUnderTesting.SabotAngleMultiplier = SabotAngleMultiplier;
                        shellUnderTesting.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
                        shellUnderTesting.CalculateDamageByType(DamageType);

                        // Users can enter minimum disruptor values even when optimizing for other damage types
                        if (MinDisruptor > 0 && DamageType != DamageType.Disruptor)
                        {
                            shellUnderTesting.CalculateDamageByType(DamageType.EMP);
                            shellUnderTesting.CalculateDamageByType(DamageType.Disruptor);
                        }

                        if ((shellUnderTesting.DamageDict[DamageType.Disruptor] >= MinDisruptor) || MinDisruptor == 0)
                        {
                            shellUnderTesting.CalculateCooldownTime();
                            shellUnderTesting.CalculateCoolerVolumeAndCost();
                            shellUnderTesting.CalculateLoaderVolumeAndCost();
                            shellUnderTesting.CalculateVariableVolumesAndCosts();

                            // Determine which "DPS Per" dictionary will be used for testing
                            Dictionary<DamageType, float> referenceDict = TestType == TestType.DpsPerVolume ?
                                shellUnderTesting.DpsPerVolumeDict : shellUnderTesting.DpsPerCostDict;
                            // Determine optimal rail draw
                            OptimizeRailDraw(shellUnderTesting, maxDraw, minDraw, referenceDict);
                            CompareToTopShells(shellUnderTesting, referenceDict);

                            // Beltfed testing
                            if (shellUnderTesting.TotalLength <= 1000f && !FiringPieceIsDif)
                            {
                                Shell shellUnderTestingBelt = new(
                                    BarrelCount,
                                    Gauge,
                                    GaugeMultiplier,
                                    true,
                                    Module.AllModules[modConfig.HeadIndex],
                                    BaseModule,
                                    RegularClipsPerLoader,
                                    RegularInputsPerLoader,
                                    BeltfedClipsPerLoader,
                                    BeltfedInputsPerLoader,
                                    UsesAmmoEjector,
                                    modConfig.RGCount,
                                    RateOfFireRpm,
                                    GunUsesRecoilAbsorbers,
                                    FiringPieceIsDif)
                                {
                                    TargetAC = TargetAC,
                                    ImpactAngleFromPerpendicularDegrees = ImpactAngleFromPerpendicularDegrees,
                                    TestIntervalSeconds = TestIntervalSeconds,
                                    StoragePerVolume = StoragePerVolume,
                                    StoragePerCost = StoragePerCost,
                                    EnginePpm = EnginePpm,
                                    EnginePpv = EnginePpv,
                                    EnginePpc = EnginePpc,
                                    EngineUsesFuel = EngineUsesFuel,
                                    TargetArmorScheme = TargetArmorScheme,
                                    MinVelocityInput = MinVelocityInput,
                                    MinRangeInput = MinEffectiveRangeInput,
                                    MaxBarrelLengthInM = MaxBarrelLengthInM,
                                    DesiredInaccuracy = MaxInaccuracy,
                                    FragAngleMultiplier = FragAngleMultiplier
                                };
                                FixedModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);


                                // Add variable modules
                                for (int i = 0; i < modConfig.BodyModCounts.Length; i++)
                                {
                                    shellUnderTestingBelt.BodyModuleCounts[i] += modConfig.BodyModCounts[i];
                                }

                                // Beltfed loaders cannot use ejectors
                                int modIndex = 0;
                                foreach (float modCount in shellUnderTestingBelt.BodyModuleCounts)
                                {
                                    if (Module.AllModules[modIndex] == Module.Defuse)
                                    {
                                        shellUnderTestingBelt.BodyModuleCounts[modIndex] = 0f;
                                        break;
                                    }
                                    else
                                    {
                                        modIndex++;
                                    }
                                }
                                shellUnderTestingBelt.CalculateLengths();
                                shellUnderTestingBelt.CalculateVelocityModifier();
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateMaxDraw();
                                shellUnderTestingBelt.CalculateReloadTime();
                                shellUnderTestingBelt.CalculateVariableVolumesAndCosts();
                                shellUnderTestingBelt.CalculateCooldownTime();
                                shellUnderTestingBelt.CalculateDamageModifierByType(DamageType);
                                shellUnderTestingBelt.SabotAngleMultiplier = SabotAngleMultiplier;
                                shellUnderTestingBelt.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
                                shellUnderTestingBelt.CalculateDamageByType(DamageType);
                                shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                                shellUnderTestingBelt.CalculateCoolerVolumeAndCost();

                                // Binary search to find optimal draw without testing every value
                                // Determine which "DPS Per" dictionary will be used for testing
                                Dictionary<DamageType, float> referenceDictBelt = TestType == TestType.DpsPerVolume ?
                                    shellUnderTestingBelt.DpsPerVolumeDict : shellUnderTestingBelt.DpsPerCostDict;
                                OptimizeRailDraw(shellUnderTestingBelt, maxDraw, minDraw, referenceDictBelt);
                                CompareToTopShells(shellUnderTestingBelt, referenceDictBelt);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds current top-performing shells to TopShells list for comparison with other lists
        /// Note that DPS is used only to determine whether a shell has been assigned to a particular length slot
        /// </summary>
        public void AddTopShellsToLocalList()
        {
            if (Dif.TopShell.DpsDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Dif.TopShell);
            }

            if (Belt.TopShell.DpsDict[DamageType] > 0)
            {
                TopShellsLocal.Add(Belt.TopShell);
            }

            for (int loaderSizeIndex = 0; loaderSizeIndex < RegularSizes.Length; loaderSizeIndex++)
            {
                LoaderSize topSize = RegularSizes[loaderSizeIndex];
                if (topSize.TopShell.DpsDict[DamageType] > 0)
                {
                    TopShellsLocal.Add(topSize.TopShell);
                }
            }
        }


        /// <summary>
        /// Finds top shells in given list.  Used in multithreading.
        /// </summary>
        /// <param name="shellBag"></param>
        public void FindTopShellsInList(ConcurrentBag<Shell> shellBag)
        {
            foreach (Shell rawShell in shellBag)
            {
                Dictionary<DamageType, float> referenceDict = TestType == TestType.DpsPerVolume ?
                    rawShell.DpsPerVolumeDict : rawShell.DpsPerCostDict;
                if (FiringPieceIsDif)
                {
                    Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                        Dif.TopShell.DpsPerVolumeDict : Dif.TopShell.DpsPerCostDict;
                    if (referenceDict[DamageType] > topReferenceDict[DamageType])
                    {
                        Dif.TopShell = rawShell;
                    }
                }
                else if (rawShell.IsBelt)
                {
                    Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                        Belt.TopShell.DpsPerVolumeDict : Belt.TopShell.DpsPerCostDict;
                    if (referenceDict[DamageType] > topReferenceDict[DamageType])
                    {
                        Belt.TopShell = rawShell;
                    }
                }
                else
                {
                    for (int loaderSizeIndex = 0; loaderSizeIndex < RegularSizes.Length; loaderSizeIndex++)
                    {
                        LoaderSize topSize = RegularSizes[loaderSizeIndex];
                        if (rawShell.TotalLength > topSize.MinLength && rawShell.TotalLength <= topSize.MaxLength)
                        {
                            Dictionary<DamageType, float> topReferenceDict = TestType == TestType.DpsPerVolume ?
                                topSize.TopShell.DpsPerVolumeDict : topSize.TopShell.DpsPerCostDict;
                            if (referenceDict[DamageType] > topReferenceDict[DamageType])
                            {
                                topSize.TopShell = rawShell;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds current top-performing shells to TopShells dictionary for writing to file
        /// Note that DPS is used only to determine whether a shell has been assigned to a length slot
        /// </summary>
        public void AddTopShellsToDictionary()
        {
            if (Dif.TopShell.DpsDict[DamageType] > 0)
            {
                TopDpsShells.Add(Dif.Name, Dif.TopShell);
            }

            if (Belt.TopShell.DpsDict[DamageType] > 0)
            {
                TopDpsShells.Add(Belt.Name, Belt.TopShell);
            }

            for (int loaderSizeIndex = 0; loaderSizeIndex < RegularSizes.Length; loaderSizeIndex++)
            {
                LoaderSize topSize = RegularSizes[loaderSizeIndex];
                if (topSize.TopShell.DpsDict[DamageType] > 0)
                {
                    TopDpsShells.Add(topSize.Name, topSize.TopShell);
                }
            }
        }

        /// <summary>
        /// Formats output value before adding to .csv row list
        /// </summary>
        /// <param name="stringList">List of values forming .csv row</param>
        /// <param name="rawNumber">Raw output value</param>
        /// <param name="decimalPlaces">Number of decimal places used for ingame display</param>
        /// <param name="isPercent">Whether rawNumber represents a % (ie uptime or shield reduction)</param>
        void AddValueToList(List<string> stringList, float rawNumber, int decimalPlaces, bool isPercent = false)
        {
            string formatString = isPercent ? "P" : "F";
            if (!RawNumberOutputIsChecked)
            {
                formatString += decimalPlaces.ToString();
            }

            stringList.Add(rawNumber.ToString(formatString));
        }

        /// <summary>
        /// Write top shell information
        /// </summary>
        public void WriteTopShells(float minGauge, float maxGauge)
        {
            bool showGP = MaxGPInput > 0;

            bool showRG = MaxRGInput > 0;

            bool showDraw = MaxDrawInput > 0;

            // Determine module and damage types to show
            Dictionary<DamageType, bool> dtToShow = new()
            {
                { DamageType.Kinetic, true },
                { DamageType.EMP, false },
                { DamageType.MD, false },
                { DamageType.Frag, false },
                { DamageType.HE, false },
                { DamageType.HEAT, false },
                { DamageType.Incendiary, false },
                { DamageType.Disruptor, false },
                { DamageType.Smoke, false }
            };

            List<int> modsToShow = [];

            dtToShow[DamageType] = true;
            for (int index = 0; index < FixedModuleCounts.Length; index++)
            {
                if (FixedModuleCounts[index] > 0 || VariableModuleIndices.Contains(index))
                {
                    modsToShow.Add(index);
                    if (Module.AllModules[index] == Module.EmpBody)
                    {
                        dtToShow[DamageType.EMP] = true;
                    }
                    else if (Module.AllModules[index] == Module.MDBody)
                    {
                        dtToShow[DamageType.MD] = true;
                    }
                    else if (Module.AllModules[index] == Module.FragBody)
                    {
                        dtToShow[DamageType.Frag] = true;
                    }
                    else if (Module.AllModules[index] == Module.HEBody)
                    {
                        dtToShow[DamageType.HE] = true;
                    }
                    else if (Module.AllModules[index] == Module.IncendiaryBody)
                    {
                        dtToShow[DamageType.Incendiary] = true;
                    }
                    else if (Module.AllModules[index] == Module.SmokeBody)
                    {
                        dtToShow[DamageType.Smoke] = true;
                    }
                }
            }

            foreach (int index in HeadList)
            {
                if (Module.AllModules[index] == Module.EmpHead || Module.AllModules[index] == Module.EmpBody)
                {
                    dtToShow[DamageType.EMP] = true;
                }
                else if (Module.AllModules[index] == Module.MDHead || Module.AllModules[index] == Module.MDBody)
                {
                    dtToShow[DamageType.MD] = true;
                }
                else if (Module.AllModules[index] == Module.FragHead || Module.AllModules[index] == Module.FragBody)
                {
                    dtToShow[DamageType.Frag] = true;
                }
                else if (Module.AllModules[index] == Module.HEHead || Module.AllModules[index] == Module.HEBody)
                {
                    dtToShow[DamageType.HE] = true;
                }
                else if (Module.AllModules[index] == Module.IncendiaryHead || Module.AllModules[index] == Module.IncendiaryBody)
                {
                    dtToShow[DamageType.Incendiary] = true;
                }
                else if (Module.AllModules[index] == Module.ShapedChargeHead)
                {
                    dtToShow[DamageType.HEAT] = true;
                }
                else if (Module.AllModules[index] == Module.Disruptor)
                {
                    dtToShow[DamageType.Disruptor] = true;
                }
                else if (Module.AllModules[index] == Module.SmokeBody)
                {
                    dtToShow[DamageType.Smoke] = true;
                }
            }

            WriteTopShellsToFile(minGauge, maxGauge, showGP, showRG, showDraw, dtToShow, modsToShow);
        }


        /// <summary>
        /// Write to file statistics of top shells
        /// </summary>
        void WriteTopShellsToFile(
            float minGauge,
            float maxGauge,
            bool showGP,
            bool showRG,
            bool showDraw,
            Dictionary<DamageType, bool> dtToShow,
            List<int> modsToShow)

        {
            // Create filename from current time
            string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ff") + ".csv";

            using var writer = new StreamWriter(fileName, append: true);
            FileStream fs = (FileStream)writer.BaseStream;

            writer.WriteLine("\nTest Parameters");

            writer.WriteLine("Barrels" + ColumnDelimiter + BarrelCount);
            if (minGauge == maxGauge)
            {
                writer.WriteLine("Gauge (mm)" + ColumnDelimiter + minGauge);
            }
            else
            {
                writer.WriteLine("Min gauge (mm)" + ColumnDelimiter + minGauge);
                writer.WriteLine("Max gauge (mm)" + ColumnDelimiter + maxGauge);
            }

            writer.WriteLine("Impact angle (°)" + ColumnDelimiter + ImpactAngleFromPerpendicularDegrees);

            if (HeadList.Count == 1)
            {
                writer.WriteLine("Head" + ColumnDelimiter + Module.AllModules[HeadList[0]].Name);
            }
            else
            {
                writer.WriteLine("Heads:");
                foreach (int headIndex in HeadList)
                {
                    writer.WriteLine(Module.AllModules[headIndex].Name);
                }
            }

            if (BaseModule != null)
            {
                writer.WriteLine("Base" + ColumnDelimiter + BaseModule.Name);
                if (BaseModule == Module.Tracer)
                {
                    writer.WriteLine("ROF (RPM)" + ColumnDelimiter + RateOfFireRpm);
                }
            }
            else
            {
                writer.WriteLine("No special base module");
            }

            writer.WriteLine("Fixed module(s):");

            int modIndex = 0;
            foreach (float modCount in FixedModuleCounts)
            {
                if (modCount > 0)
                {
                    writer.WriteLine(Module.AllModules[modIndex].Name + ColumnDelimiter + modCount);
                }
                modIndex++;
            }

            // Remove duplicate variable mod indices
            List<int> uniqueVarModIndices = VariableModuleIndices.Distinct().ToList();
            writer.WriteLine("Variable module(s):");
            foreach (int index in uniqueVarModIndices)
            {
                writer.WriteLine(Module.AllModules[index].Name);
            }

            writer.WriteLine("Clips per loader" + ColumnDelimiter + RegularClipsPerLoader);
            writer.WriteLine("Inputs per loader" + ColumnDelimiter + RegularInputsPerLoader);
            if (UsesAmmoEjector)
            {
                writer.WriteLine("Ammo ejector");
            }
            writer.WriteLine("Clips per loader (beltfed)" + ColumnDelimiter + BeltfedClipsPerLoader);
            writer.WriteLine("Inputs per loader (beltfed)" + ColumnDelimiter + BeltfedInputsPerLoader);

            writer.WriteLine("Max GP casings" + ColumnDelimiter + MaxGPInput);
            if (MaxGPInput > 0)
            {
                writer.WriteLine("GP Increment" + ColumnDelimiter + GPIncrement);
            }
            writer.WriteLine("Max RG casings" + ColumnDelimiter + MaxRGInput);
            writer.WriteLine("Max draw" + ColumnDelimiter + MaxDrawInput);
            if (MaxDrawInput > 0)
            {
                writer.WriteLine("Engine PPM" + ColumnDelimiter + EnginePpm);
                writer.WriteLine("Engine PPV" + ColumnDelimiter + EnginePpv);
                writer.WriteLine("Engine PPC" + ColumnDelimiter + EnginePpc);
                writer.WriteLine("Fuel engine" + ColumnDelimiter + EngineUsesFuel);
            }
            writer.WriteLine("Max recoil" + ColumnDelimiter + MaxRecoilInput);
            writer.WriteLine("Min length (mm)" + ColumnDelimiter + MinShellLength);
            writer.WriteLine("Max length (mm)" + ColumnDelimiter + MaxShellLength);
            writer.WriteLine("Min velocity (m/s)" + ColumnDelimiter + MinVelocityInput);
            writer.WriteLine("Min effective range (m)" + ColumnDelimiter + MinEffectiveRangeInput);
            if (LimitBarrelLength)
            {
                writer.WriteLine("Max inaccuracy (°)" + ColumnDelimiter + MaxInaccuracy);
                if (BarrelLengthLimitType == BarrelLengthLimit.Calibers)
                {
                    writer.WriteLine("Max barrel length (calibers)" + ColumnDelimiter + MaxBarrelLengthInCalibers);
                }
                else if (BarrelLengthLimitType == BarrelLengthLimit.FixedLength)
                {
                    writer.WriteLine("Max barrel length (m)" + ColumnDelimiter + MaxBarrelLengthInM);
                }
            }
            writer.WriteLine("Test interval (min)" + ColumnDelimiter + TestIntervalMinutes);
            if (FiringPieceIsDif)
            {
                writer.WriteLine("Gun is using Direct Input Feed");
            }

            if (!GunUsesRecoilAbsorbers)
            {
                writer.WriteLine("Gun is NOT using recoil absorbers");
            }

            // Determine whether to show target armor scheme
            bool pendepth = false;
            foreach (Layer layer in TargetArmorScheme.LayerList)
            {
                if (layer != Layer.Air)
                {
                    pendepth = true;
                }
            }
            if (pendepth)
            {
                writer.WriteLine("Target armor scheme:");
                foreach (Layer layer in TargetArmorScheme.LayerList)
                {
                    writer.WriteLine(layer.Name);
                }
            }

            // Determine optimized damage type
            if (DamageType == DamageType.Kinetic)
            {
                writer.WriteLine("Damage type" + ColumnDelimiter + "Kinetic");
            }
            else if (DamageType == DamageType.Disruptor)
            {
                writer.WriteLine("Damage type" + ColumnDelimiter + "Disruptor");
            }
            else if (DamageType == DamageType.Frag)
            {
                writer.WriteLine("Damage type" + ColumnDelimiter + "Frag");
            }
            else
            {
                writer.WriteLine("Damage type" + ColumnDelimiter + (DamageType)(int)DamageType);
            }

            // Display common stats for all calculated damage types
            if (dtToShow[DamageType.Kinetic])
            {
                writer.WriteLine("Target AC" + ColumnDelimiter + TargetAC);
            }
            if (dtToShow[DamageType.Disruptor])
            {
                writer.WriteLine("Min disruptor strength" + ColumnDelimiter + MinDisruptor);
            }
            if (dtToShow[DamageType.Frag])
            {
                writer.WriteLine("Frag cone angle (°)" + ColumnDelimiter + FragConeAngle);
            }

            if (TestType == TestType.DpsPerVolume)
            {
                writer.WriteLine("Testing for DPS / volume");
            }
            else if (TestType == TestType.DpsPerCost)
            {
                writer.WriteLine("Testing for DPS / cost");
            }

            if (RawNumberOutputIsChecked)
            {
                writer.WriteLine("Outputting raw numbers");
            }
            else
            {
                writer.WriteLine("Rounding numbers to match values shown ingame");
            }
            writer.WriteLine();


            // Determine whether any shells met test criteria
            bool shellsToPrint = false;
            foreach (string shellName in TopDpsShells.Keys)
            {
                if (TopDpsShells[shellName] != null)
                {
                    shellsToPrint = true;
                }
            }

            if (!shellsToPrint)
            {
                writer.WriteLine("No shells meet test criteria. Check test parameters.");
            }
            else
            {
                writer.WriteLine("Shells");
                foreach (KeyValuePair<string, Shell> topShellPair in TopDpsShells)
                {
                    // Calculate barrel lengths
                    topShellPair.Value.CalculateRequiredBarrelLengths();
                    if (dtToShow[DamageType.Disruptor]
                        || dtToShow[DamageType.EMP]
                        || dtToShow[DamageType.MD]
                        || dtToShow[DamageType.Frag]
                        || dtToShow[DamageType.HE]
                        || dtToShow[DamageType.HEAT]
                        || dtToShow[DamageType.Incendiary])
                    {
                        topShellPair.Value.CalculateChemModifier();
                    }

                    // Calculate all damage and DPS -- including those not used for optimizing
                    foreach (DamageType dt in dtToShow.Keys)
                    {
                        if (dtToShow[dt])
                        {
                            topShellPair.Value.CalculateDamageByType(dt);
                            topShellPair.Value.CalculateDpsByType(dt);
                        }
                    }
                    topShellPair.Value.GetModuleCounts();
                }

                List<string> loaderSizeList =
                [
                    " ", .. TopDpsShells.Keys
                ];
                writer.WriteLine(string.Join(ColumnDelimiter, loaderSizeList));

                List<string> gaugeList =
                [
                    "Gauge (mm)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(gaugeList, topShell.Gauge, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, gaugeList));

                List<string> totalLengthList =
                [
                    "Total length (mm)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(totalLengthList, topShell.TotalLength, 3);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, totalLengthList));

                List<string> lengthWithoutCasingsList =
                [
                    "Length without casings (mm)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(lengthWithoutCasingsList, topShell.ProjectileLength, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, lengthWithoutCasingsList));

                List<string> totalModulesList =
                [
                    "Total modules"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(totalModulesList, topShell.ModuleCountTotal, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, totalModulesList));

                if (showGP)
                {
                    List<string> gpCasingList =
                    [
                        "GP casing"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(gpCasingList, topShell.GPCasingCount, 2);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, gpCasingList));
                }
                if (showRG)
                {
                    List<string> rgCasingList =
                    [
                        "RG casing"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(rgCasingList, topShell.RGCasingCount, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, rgCasingList));
                }
                if (showDraw)
                {
                    List<string> railDrawList =
                    [
                        "Rail draw"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(railDrawList, topShell.RailDraw, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, railDrawList));
                }

                foreach (int index in modsToShow)
                {
                    List<string> modCountList =
                    [
                        Module.AllModules[index].Name
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(modCountList, topShell.BodyModuleCounts[index], 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, modCountList));
                }

                List<string> headList =
                [
                    "Head"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    headList.Add(topShell.HeadModule.Name);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, headList));

                foreach (DamageType dt in dtToShow.Keys)
                {
                    if (dtToShow[dt])
                    {
                        List<string> dpsPerVolumeList =
                        [
                            (DamageType)(int)dt + " DPS per volume"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            dpsPerVolumeList.Add(topShell.DpsPerVolumeDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsPerVolumeList));

                        List<string> dpsPerCostList =
                        [
                            (DamageType)(int)dt + " DPS per cost"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            dpsPerCostList.Add(topShell.DpsPerCostDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsPerCostList));
                    }
                }

                writer.WriteLine();
                writer.WriteLine("Shell Stats");
                // Recoil = draw if no GP
                if (showGP)
                {
                    List<string> recoilList =
                    [
                        "Recoil"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(recoilList, topShell.TotalRecoil, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, recoilList));
                }

                List<string> velocityModifierList =
                [
                    "Velocity modifier"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(velocityModifierList, topShell.OverallVelocityModifier, 2);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, velocityModifierList));

                List<string> velocityList =
                [
                    "Velocity (m/s)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(velocityList, topShell.Velocity, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, velocityList));

                List<string> effectiveRangeList =
                [
                    "Effective range (m)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(effectiveRangeList, topShell.EffectiveRange, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, effectiveRangeList));

                List<string> inaccuracyModifierList =
                [
                    "Inaccuracy modifier"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(inaccuracyModifierList, topShell.OverallInaccuracyModifier, 0, true);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, inaccuracyModifierList));

                List<string> barrelLengthInaccuracyList =
                [
                    "Barrel length for inaccuracy (m)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(barrelLengthInaccuracyList, topShell.BarrelLengthForInaccuracy, 1);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, barrelLengthInaccuracyList));

                if (showGP)
                {
                    List<string> barrelLengthPropellantBurnList =
                    [
                        "Barrel length for propellant burn (m)"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(barrelLengthPropellantBurnList, topShell.BarrelLengthForPropellant, 1);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, barrelLengthPropellantBurnList));
                }

                foreach (DamageType dt in dtToShow.Keys)
                {
                    if (dtToShow[dt])
                    {
                        if (dt == DamageType.Kinetic)
                        {
                            List<string> kdModifierList =
                            [
                                "KD modifier"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(kdModifierList, topShell.OverallKineticDamageModifier, 2);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, kdModifierList));

                            List<string> rawKDList =
                            [
                                "Raw KD"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(rawKDList, topShell.RawKD, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, rawKDList));

                            List<string> apModifierList =
                            [
                                "AP modifier"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(apModifierList, topShell.OverallArmorPierceModifier, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, apModifierList));

                            List<string> apList =
                            [
                                "AP"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(apList, topShell.ArmorPierce, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, apList));

                            List<string> kdAPList =
                            [
                                "KD * AP"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(kdAPList, topShell.ArmorPierce * topShell.RawKD, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, kdAPList));

                            List<string> kdMultiplierList =
                            [
                                "KD multiplier from angle"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                if (topShell.HeadModule == Module.HollowPoint || TargetAC == 20f)
                                {
                                    int hpAngleMultiplier = 1;
                                    AddValueToList(kdMultiplierList, hpAngleMultiplier, 2);
                                }
                                else if (topShell.HeadModule == Module.SabotHead)
                                {
                                    AddValueToList(kdMultiplierList, topShell.SabotAngleMultiplier, 2);
                                }
                                else
                                {
                                    AddValueToList(kdMultiplierList, topShell.NonSabotAngleMultiplier, 2);
                                }
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, kdMultiplierList));
                        }
                        else if (dt == DamageType.Frag)
                        {
                            List<string> fragCountList =
                            [
                                "Frag count"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(fragCountList, topShell.FragCount, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fragCountList));

                            List<string> damagePerFragList =
                            [
                                "Damage per frag"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(damagePerFragList, topShell.DamagePerFrag, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, damagePerFragList));
                        }
                        else if (dt == DamageType.MD)
                        {
                            List<string> mdExplosionRadiusList =
                            [
                                "MD explosion radius (m)"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(mdExplosionRadiusList, topShell.MDExplosionRadius, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, mdExplosionRadiusList));
                        }
                        else if (dt == DamageType.HE)
                        {
                            List<string> rawHEDamageList =
                            [
                                "Raw HE damage"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(rawHEDamageList, topShell.RawHE, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, rawHEDamageList));

                            List<string> heExplosionRadiusList =
                            [
                                "HE explosion radius (m)"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                AddValueToList(heExplosionRadiusList, topShell.HEExplosionRadius, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, heExplosionRadiusList));
                        }

                        List<string> damageList =
                        [
                            (DamageType)(int)dt + " damage"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            AddValueToList(damageList, topShell.DamageDict[dt], 0);
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, damageList));
                    }
                }

                List<string> shellReloadTimeList =
                [
                    "Shell reload time (s)"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(shellReloadTimeList, topShell.ShellReloadTime, 2);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, shellReloadTimeList));

                if (RegularClipsPerLoader > 0)
                {
                    List<string> clusterReloadTimeList =
                    [
                        "Cluster reload time (s)"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        AddValueToList(clusterReloadTimeList, topShell.ClusterReloadTime, 2);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, clusterReloadTimeList));
                }

                List<string> uptimeList =
                [
                    "Uptime"
                ];
                foreach (Shell topShell in TopDpsShells.Values)
                {
                    AddValueToList(uptimeList, topShell.Uptime, 0, true);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, uptimeList));

                foreach (DamageType dt in dtToShow.Keys)
                {
                    if (dtToShow[dt])
                    {
                        List<string> dpsList =
                        [
                            (DamageType)(int)dt + " DPS"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            dpsList.Add(topShell.DpsDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsList));
                    }
                }

                if (VerboseOutputIsChecked)
                {
                    writer.WriteLine();
                    writer.WriteLine("Volume Stats");
                    List<string> loaderVolumeList =
                    [
                        "Loader volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        loaderVolumeList.Add(topShell.LoaderVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, loaderVolumeList));

                    if (showGP)
                    {
                        List<string> coolerVolumeList =
                        [
                        "Cooler volume"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            coolerVolumeList.Add(topShell.CoolerVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, coolerVolumeList));
                    }

                    if (showDraw)
                    {
                        List<string> chargerVolumeList =
                        [
                            "Charger volume"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            chargerVolumeList.Add(topShell.ChargerVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, chargerVolumeList));

                        List<string> engineVolumeList =
                        [
                            "Engine volume"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            engineVolumeList.Add(topShell.EngineVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, engineVolumeList));

                        if (EngineUsesFuel)
                        {
                            List<string> fuelAccessVolumeList =
                            [
                                "Fuel access volume"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                fuelAccessVolumeList.Add(topShell.FuelAccessVolume.ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fuelAccessVolumeList));
                        }


                        List<string> fuelStorageVolumeList =
                        [
                            "Fuel storage volume"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            fuelStorageVolumeList.Add(topShell.FuelStorageVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelStorageVolumeList));
                    }

                    List<string> recoilVolumeList =
                    [
                        "Recoil volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        recoilVolumeList.Add(topShell.RecoilVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, recoilVolumeList));

                    List<string> ammoAccessVolumeList =
                    [
                        "Ammo access volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        ammoAccessVolumeList.Add(topShell.AmmoAccessVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoAccessVolumeList));

                    List<string> ammoStorageVolumeList =
                    [
                        "Ammo storage volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        ammoStorageVolumeList.Add(topShell.AmmoStorageVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoStorageVolumeList));

                    List<string> totalVolumeList =
                    [
                        "Total volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        totalVolumeList.Add(topShell.VolumePerLoader.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, totalVolumeList));

                    writer.WriteLine();
                    writer.WriteLine("Cost Stats");

                    List<string> costPerShellList =
                    [
                        "Cost per shell"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        costPerShellList.Add(topShell.CostPerShell.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, costPerShellList));

                    List<string> loaderCostList =
                    [
                        "Loader cost"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        loaderCostList.Add(topShell.LoaderCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, loaderCostList));

                    if (showGP)
                    {
                        List<string> coolerCostList =
                        [
                            "Cooler cost"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            coolerCostList.Add(topShell.CoolerCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, coolerCostList));
                    }

                    if (showDraw)
                    {
                        List<string> chargerCostList =
                        [
                            "Charger cost"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            chargerCostList.Add(topShell.ChargerCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, chargerCostList));

                        List<string> fuelBurnedList =
                        [
                            "Fuel burned"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            fuelBurnedList.Add(topShell.FuelBurned.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelBurnedList));

                        List<string> engineCostList =
                        [
                            "Engine cost"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            engineCostList.Add(topShell.EngineCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, engineCostList));

                        if (EngineUsesFuel)
                        {
                            List<string> fuelAccessCostList =
                            [
                                "Fuel access cost"
                            ];
                            foreach (Shell topShell in TopDpsShells.Values)
                            {
                                fuelAccessCostList.Add(topShell.FuelAccessCost.ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fuelAccessCostList));
                        }

                        List<string> fuelStorageCostList =
                        [
                            "Fuel storage cost"
                        ];
                        foreach (Shell topShell in TopDpsShells.Values)
                        {
                            fuelStorageCostList.Add(topShell.FuelStorageCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelStorageCostList));
                    }

                    List<string> recoilCostList =
                    [
                        "Recoil cost"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        recoilCostList.Add(topShell.RecoilCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, recoilCostList));

                    List<string> ammoUsedList =
                    [
                        "Ammo used"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        ammoUsedList.Add(topShell.AmmoUsed.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoUsedList));

                    List<string> ammoAccessCostList =
                    [
                        "Ammo access cost"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        ammoAccessCostList.Add(topShell.AmmoAccessCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoAccessCostList));

                    List<string> ammoStorageCostList =
                    [
                        "Ammo storage cost"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        ammoStorageCostList.Add(topShell.AmmoStorageCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoStorageCostList));

                    List<string> totalCostList =
                    [
                        "Total cost"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        totalCostList.Add(topShell.CostPerLoader.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, totalCostList));

                    List<string> costPerVolumeList =
                    [
                        "Cost per volume"
                    ];
                    foreach (Shell topShell in TopDpsShells.Values)
                    {
                        costPerVolumeList.Add(topShell.CostPerVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, costPerVolumeList));
                }
            }
        }
    }
}
