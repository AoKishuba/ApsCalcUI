using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ApsCalcUITests")]

namespace ApsCalcUI
{
    public readonly record struct Neighborhood(float CenterGP, float CenterRG, float BottomRG, float TopRG);

    public struct ModuleConfig
    {
        public LoaderBracket Bracket;
        public int HeadIndex;
        public float[] VariableModCounts;
        public float GPCount;
        public float RGCount;
    }

    public enum LoaderType { Regular, Belt, Dif }

    public readonly record struct LoaderBracket(float MinLengthMMExclusive, float MaxLengthMMInclusive, LoaderType LoaderType)
    {
        public string DisplayName => LoaderType switch
        {
            LoaderType.Dif => "DIF",
            LoaderType.Belt => $"{MaxLengthMMInclusive / 1000f:0}m (belt)",
            LoaderType.Regular => $"{MaxLengthMMInclusive / 1000f:0}m",
            _ => throw new InvalidOperationException()
        };
    }

    // Damage types
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
        /// <param name="headIndexList">List of module indices for every module to be used as head</param>
        /// <param name="baseModule">Special base module, if any</param>
        /// <param name="fixedModuleCounts">An array of integers representing number of shells at that index in module list</param>
        /// <param name="fixedModuleTotal">Minimum number of modules on every shell</param>
        /// <param name="variableModuleIndices">Module indices of modules to be used in varying numbers in testing</param>
        /// <param name="regularClipsPerLoader">Directly-connected clips for each regular loader</param>
        /// <param name="regularInputsPerLoader">Ammo inputs per regular loader/clip cluster</param>
        /// <param name="beltfedClipsPerLoader">Clips per beltfed loader</param>
        /// <param name="beltfedInputsPerLoader">Inputs per beltfed loader/clip cluster</param>
        /// <param name="usesAmmoEjector">Whether loader cluster uses ammo ejector</param>
        /// <param name="maxGPInput">Max desired number of gunpowder casings</param>
        /// <param name="casingIncrement">Amount of GP and/or RG casing added between tests</param>
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
        /// <param name="barrelLengthHardLimit">Whether barrel length limit is a hard DQ or merely prompts dividing DPS by impact area</param>
        /// <param name="maxBarrelLength">Max barrel length in m or calibers</param>
        /// <param name="barrelLengthLimitType">Whether to limit barrel length by m or calibers (multiples of gauge)</param>
        /// <param name="verboseOutputIsChecked">Whether to show volume and cost numbers for debugging/details</param>
        /// <param name="rawNumberOutputIsChecked">Do not round numbers in output if true</param>
        /// <param name="columnDelimiter">Character used to separate columns in .csv output; either comma or semicolon</param>
        public ShellCalc(
            int barrelCount,
            float gauge,
            float gaugeMultiplier,
            List<int> headIndexList,
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
            float casingIncrement,
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
            bool barrelLengthHardLimit,
            bool verboseOutputIsChecked,
            bool rawNumberOutputIsChecked,
            char columnDelimiter
            )
        {
            BarrelCount = barrelCount;
            Gauge = gauge;
            GaugeMultiplier = gaugeMultiplier;
            HeadIndexList = headIndexList;
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
            CasingIncrement = casingIncrement;
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
                // Limit by barrel length for full propellant burn
                MaxGP = MathF.Min(maxGPInput, MaxBarrelLengthInM / 2.2f / MathF.Pow(Gauge / 1000f, 0.55f));
            }
            else
            {
                MaxGP = MaxGPInput;
            }
            BarrelLengthHardLimit = barrelLengthHardLimit;
            VerboseOutputIsChecked = verboseOutputIsChecked;
            RawNumberOutputIsChecked = rawNumberOutputIsChecked;
            ColumnDelimiter = columnDelimiter;
            LoaderBrackets = firingPieceIsDif ? [new LoaderBracket(0, float.MaxValue, LoaderType.Dif)]
                : [
                    new LoaderBracket(0, 1000, LoaderType.Belt),
                    new LoaderBracket(0, 1000, LoaderType.Regular),
                    new LoaderBracket(1000, 2000, LoaderType.Regular),
                    new LoaderBracket(2000, 3000, LoaderType.Regular),
                    new LoaderBracket(3000, 4000, LoaderType.Regular),
                    new LoaderBracket(4000, 5000, LoaderType.Regular),
                    new LoaderBracket(5000, 6000, LoaderType.Regular),
                    new LoaderBracket(6000, 7000, LoaderType.Regular),
                    new LoaderBracket(7000, 8000, LoaderType.Regular),
                ];

            // Per-bracket best shells, populated lazily as search finds feasible
            // candidates. A bracket key is absent until a shell is recorded for it.
            TopShells = [];
        }

        public int BarrelCount { get; }
        public float Gauge { get; }
        public float GaugeMultiplier { get; }
        public List<int> HeadIndexList { get; }
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
        public float CasingIncrement { get; }
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
        public bool BarrelLengthHardLimit { get; }
        public bool VerboseOutputIsChecked { get; }
        public bool RawNumberOutputIsChecked { get; }
        public char ColumnDelimiter { get; }


        // Store top-DPS shells by loader length
        public LoaderBracket[] LoaderBrackets { get; }
        public Dictionary<LoaderBracket, Shell> TopShells { get; }

        private IEnumerable<ModuleConfig> GenerateModConfigs()
        {
            float maxModuleCount = 20f - FixedModuleTotal;
            for (int headIndex = 0; headIndex < HeadIndexList.Count; headIndex++)
            {
                // Use ints to avoid cumulative float errors
                float gpMax = MathF.Min(MaxGP, maxModuleCount);
                int maxGPIncrementCount = (int)Math.Floor(gpMax / CasingIncrement);
                for (int gpIncrementCount = 0; gpIncrementCount <= maxGPIncrementCount; gpIncrementCount++)
                {
                    float gpCount = CasingIncrement * gpIncrementCount;

                    float rgMax = MathF.Min(MaxRGInput, MathF.Floor(maxModuleCount - gpCount));
                    int maxRGIncrementCount = (int)Math.Floor(rgMax / CasingIncrement);
                    for (int rgIncrementCount = 0; rgIncrementCount <= maxRGIncrementCount; rgIncrementCount++)
                    {
                        float rgCount = CasingIncrement * rgIncrementCount;

                        float var0Max = maxModuleCount - gpCount - rgCount;
                        for (float var0Count = 0; var0Count <= var0Max; var0Count++)
                        {
                            float var1Max = VariableModuleIndices[1] == VariableModuleIndices[0] ?
                                0 : maxModuleCount - gpCount - rgCount - var0Count;
                            for (float var1Count = 0; var1Count <= var1Max; var1Count++)
                            {
                                float var2Max = VariableModuleIndices[2] == VariableModuleIndices[0] ?
                                    0 : maxModuleCount - gpCount - rgCount - var0Count - var1Count;
                                for (float var2Count = 0; var2Count <= var2Max; var2Count++)
                                {
                                    float var3Max = VariableModuleIndices[3] == VariableModuleIndices[0] ?
                                        0 : maxModuleCount - gpCount - rgCount - var0Count - var1Count - var2Count;
                                    for (float var3Count = 0; var3Count <= var3Max; var3Count++)
                                    {
                                        float var4Max = VariableModuleIndices[4] == VariableModuleIndices[0] ?
                                            0 : maxModuleCount - gpCount - rgCount - var0Count - var1Count - var2Count - var3Count;
                                        for (float var4Count = 0; var4Count <= var4Max; var4Count++)
                                        {
                                            float var5Max = VariableModuleIndices[5] == VariableModuleIndices[0] ?
                                                0 :
                                                maxModuleCount
                                                - gpCount
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
                                                    - gpCount
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
                                                        - gpCount
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
                                                            - gpCount
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
                                                            yield return new ModuleConfig
                                                            {
                                                                GPCount = gpCount,
                                                                RGCount = rgCount,
                                                                HeadIndex = HeadIndexList[headIndex],
                                                                VariableModCounts =
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
        }

        /// <summary>
        /// Iterates over possible configurations and stores best according to test parameters
        /// </summary>
        public void ShellTest()
        {
            // Set up target armor scheme for testing
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
                    modConfig.GPCount,
                    modConfig.RGCount,
                    RateOfFireRpm,
                    GunUsesRecoilAbsorbers,
                    FiringPieceIsDif
                    );
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                // Add variable modules
                for (int i = 0; i < modConfig.VariableModCounts.Length; i++)
                {
                    int moduleIndex = VariableModuleIndices[i];
                    float moduleCount = modConfig.VariableModCounts[i];
                    shellUnderTesting.BodyModuleCounts[moduleIndex] += moduleCount;
                }

                shellUnderTesting.CalculateLengths();
                shellUnderTesting.GetModuleCounts();
                shellUnderTesting.CalculateRecoil();
                bool lengthWithinBounds = true;
                if (LimitBarrelLength
                    && BarrelLengthHardLimit
                    && shellUnderTesting.ProjectileLength
                        > shellUnderTesting.CalculateMaxProjectileLengthForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy))
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
                    shellUnderTesting.CalculateDamageModifierByType(DamageType);
                    shellUnderTesting.CalculateMaxDraw();

                    // Physical draw limit
                    float maxDraw = MathF.Min(shellUnderTesting.MaxDrawShell, MaxDrawInput);
                    // Limit by recoil. Projectile draw is 1 : 1 with recoil; draw applied to RG casings is affected by multiplier
                    float maxRailRecoil = MaxRecoilInput - shellUnderTesting.GPRecoil;
                    float maxCasingDrawForRecoil = MathF.Min(maxRailRecoil / shellUnderTesting.RGCasingFeltRecoilMultiplier, shellUnderTesting.MaxDrawCasing);
                    float maxCasingRecoil = maxCasingDrawForRecoil * shellUnderTesting.RGCasingFeltRecoilMultiplier;
                    float maxProjectileDrawForRecoil = maxRailRecoil - maxCasingRecoil;
                    float maxDrawForRecoil = maxCasingDrawForRecoil + maxProjectileDrawForRecoil;
                    maxDraw = MathF.Min(maxDraw, maxDrawForRecoil);
                    // Limit by inaccuracy
                    if (!shellUnderTesting.GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
                    {
                        maxDraw = MathF.Min(maxDraw, shellUnderTesting.CalculateMaxDrawForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy, shellUnderTesting.MaxDrawCasing));
                    }

                    float minVelocity = MathF.Max(MinVelocityInput, TargetArmorScheme.CalculateMinVelocityToPenetrate(shellUnderTesting, ImpactAngleFromPerpendicularDegrees));
                    float minDraw = shellUnderTesting.CalculateMinDrawForVelocityAndRange(minVelocity, MinEffectiveRangeInput);
                    maxDraw = MathF.Floor(maxDraw);
                    minDraw = MathF.Ceiling(minDraw);

                    if (maxDraw >= minDraw)
                    {
                        shellUnderTesting.CalculateReloadTime(TestIntervalSeconds);
                        shellUnderTesting.SabotAngleMultiplier = SabotAngleMultiplier;
                        shellUnderTesting.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
                        shellUnderTesting.CalculateDamageByType(DamageType, FragAngleMultiplier);

                        // Users can enter minimum disruptor values even when optimizing for other damage types
                        if (MinDisruptor > 0 && DamageType != DamageType.Disruptor)
                        {
                            shellUnderTesting.CalculateDamageByType(DamageType.EMP, FragAngleMultiplier);
                            shellUnderTesting.CalculateDamageByType(DamageType.Disruptor, FragAngleMultiplier);
                        }

                        if ((shellUnderTesting.DamageDict[DamageType.Disruptor] >= MinDisruptor) || MinDisruptor == 0)
                        {
                            shellUnderTesting.CalculateCooldownTime();
                            shellUnderTesting.CalculateCoolerVolumeAndCost();
                            shellUnderTesting.CalculateLoaderVolumeAndCost();
                            shellUnderTesting.CalculateVariableVolumesAndCosts(TestIntervalSeconds, StoragePerVolume, StoragePerCost);

                            // Determine which "DPS Per" dictionary will be used for testing
                            Dictionary<DamageType, float> referenceDict;
                            if (LimitBarrelLength && !BarrelLengthHardLimit)
                            {
                                referenceDict = TestType == TestType.DpsPerVolume ?
                                    shellUnderTesting.DpsPerVolumePerAreaDict : shellUnderTesting.DpsPerCostPerAreaDict;
                            }
                            else
                            {
                                referenceDict = TestType == TestType.DpsPerVolume ?
                                    shellUnderTesting.DpsPerVolumeDict : shellUnderTesting.DpsPerCostDict;
                            }

                            // Determine optimal rail draw
                            float optimalDraw = maxDraw > 0 ?
                                CalculateOptimalRailDraw(shellUnderTesting, maxDraw, minDraw, referenceDict)
                                : 0;
                            shellUnderTesting.RailDraw = optimalDraw;
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
                                    modConfig.GPCount,
                                    modConfig.RGCount,
                                    RateOfFireRpm,
                                    GunUsesRecoilAbsorbers,
                                    FiringPieceIsDif);
                                shellUnderTesting.BodyModuleCounts.CopyTo(shellUnderTestingBelt.BodyModuleCounts, 0);

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
                                shellUnderTestingBelt.GetModuleCounts();
                                shellUnderTestingBelt.CalculateRequiredBarrelLengths(MaxInaccuracy);
                                shellUnderTestingBelt.CalculateVelocityModifier();
                                shellUnderTestingBelt.CalculateRecoil();
                                shellUnderTestingBelt.CalculateMaxDraw();
                                shellUnderTestingBelt.CalculateReloadTime(TestIntervalSeconds);
                                shellUnderTestingBelt.CalculateVariableVolumesAndCosts(
                                    TestIntervalSeconds,
                                    StoragePerVolume,
                                    StoragePerCost);
                                shellUnderTestingBelt.CalculateCooldownTime();
                                shellUnderTestingBelt.CalculateDamageModifierByType(DamageType);
                                shellUnderTestingBelt.SabotAngleMultiplier = SabotAngleMultiplier;
                                shellUnderTestingBelt.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
                                shellUnderTestingBelt.CalculateDamageByType(DamageType, FragAngleMultiplier);
                                shellUnderTestingBelt.CalculateLoaderVolumeAndCost();
                                shellUnderTestingBelt.CalculateCoolerVolumeAndCost();

                                // Beltfed does not use ejectors, so draw requirements may be different
                                float maxDrawBelt = MathF.Min(shellUnderTestingBelt.MaxDrawShell, MaxDrawInput);
                                float maxRailRecoilBelt = MaxRecoilInput - shellUnderTestingBelt.GPRecoil;
                                float maxCasingDrawForRecoilBelt = MathF.Min(
                                    maxRailRecoilBelt / shellUnderTestingBelt.RGCasingFeltRecoilMultiplier,
                                    shellUnderTestingBelt.MaxDrawCasing);
                                float maxCasingRecoilBelt = maxCasingDrawForRecoilBelt * shellUnderTestingBelt.RGCasingFeltRecoilMultiplier;
                                float maxProjectileDrawForRecoilBelt = maxRailRecoilBelt - maxCasingRecoilBelt;
                                maxDrawBelt = MathF.Min(maxDrawBelt, maxCasingDrawForRecoilBelt + maxProjectileDrawForRecoilBelt);
                                if (!shellUnderTestingBelt.GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
                                    maxDrawBelt = MathF.Min(maxDrawBelt,
                                        shellUnderTestingBelt.CalculateMaxDrawForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy, shellUnderTestingBelt.MaxDrawCasing));

                                float minVelocityBelt = MathF.Max(MinVelocityInput,
                                    TargetArmorScheme.CalculateMinVelocityToPenetrate(shellUnderTestingBelt, ImpactAngleFromPerpendicularDegrees));
                                float minDrawBelt = shellUnderTestingBelt.CalculateMinDrawForVelocityAndRange(minVelocityBelt, MinEffectiveRangeInput);

                                maxDrawBelt = MathF.Floor(maxDrawBelt);
                                minDrawBelt = MathF.Ceiling(minDrawBelt);

                                if (maxDrawBelt >= minDrawBelt)
                                {
                                    // Binary search to find optimal draw without testing every value
                                    // Determine which "DPS Per" dictionary will be used for testing
                                    Dictionary<DamageType, float> referenceDictBelt;
                                    if (!BarrelLengthHardLimit && LimitBarrelLength)
                                    {
                                        referenceDictBelt = TestType == TestType.DpsPerVolume ?
                                            shellUnderTestingBelt.DpsPerVolumePerAreaDict : shellUnderTestingBelt.DpsPerCostPerAreaDict;
                                    }
                                    else
                                    {
                                        referenceDictBelt = TestType == TestType.DpsPerVolume ?
                                            shellUnderTestingBelt.DpsPerVolumeDict : shellUnderTestingBelt.DpsPerCostDict;
                                    }
                                    optimalDraw = maxDrawBelt > 0 ?
                                        CalculateOptimalRailDraw(shellUnderTestingBelt, maxDrawBelt, minDrawBelt, referenceDictBelt)
                                        : 0;

                                    shellUnderTestingBelt.RailDraw = optimalDraw;
                                    shellUnderTestingBelt.CalculateDpsByType(
                                        DamageType, TargetAC, TestIntervalSeconds, StoragePerVolume, StoragePerCost,
                                        EnginePpm, EnginePpv, EnginePpc, EngineUsesFuel, TargetArmorScheme,
                                        ImpactAngleFromPerpendicularDegrees);
                                    if (LimitBarrelLength && !BarrelLengthHardLimit)
                                    {
                                        shellUnderTestingBelt.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
                                    }

                                    if (TryGetBracket(shellUnderTestingBelt.TotalLength, LoaderType.Belt, out LoaderBracket beltBracket))
                                    {
                                        // No incumbent yet for this bracket => any feasible shell wins.
                                        float incumbentScoreBelt;
                                        if (LimitBarrelLength && !BarrelLengthHardLimit)
                                        {
                                            incumbentScoreBelt = TopShells.TryGetValue(beltBracket, out Shell incumbentBelt)
                                                ? (TestType == TestType.DpsPerVolume
                                                    ? incumbentBelt.DpsPerVolumePerAreaDict[DamageType]
                                                    : incumbentBelt.DpsPerCostPerAreaDict[DamageType])
                                                : float.NegativeInfinity;
                                        }
                                        else
                                        {
                                            incumbentScoreBelt = TopShells.TryGetValue(beltBracket, out Shell incumbentBelt)
                                                ? (TestType == TestType.DpsPerVolume
                                                    ? incumbentBelt.DpsPerVolumeDict[DamageType]
                                                    : incumbentBelt.DpsPerCostDict[DamageType])
                                                : float.NegativeInfinity;
                                        }
                                        if (referenceDictBelt[DamageType] > incumbentScoreBelt)
                                        {
                                            shellUnderTestingBelt.CalculateInaccuracyAtBarrelLength(MaxBarrelLengthInM, MinEffectiveRangeInput);
                                            TopShells[beltBracket] = shellUnderTestingBelt;
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
        /// Finds loader bracket whose loader type and length range contain given shell length.
        /// </summary>
        bool TryGetBracket(float totalLength, LoaderType loaderType, out LoaderBracket bracket)
        {
            foreach (LoaderBracket candidate in LoaderBrackets)
            {
                if (candidate.LoaderType == loaderType
                    && totalLength > candidate.MinLengthMMExclusive
                    && totalLength <= candidate.MaxLengthMMInclusive)
                {
                    bracket = candidate;
                    return true;
                }
            }
            bracket = default;
            return false;
        }

        /// <summary>
        /// Result sink for brute-force path. Re-evaluates shell at its chosen draw, maps it to its loader
        /// bracket by total length, and records it in TopShells if it beats incumbent.
        /// </summary>
        void CompareToTopShells(Shell shellUnderTesting, Dictionary<DamageType, float> referenceDict)
        {
            shellUnderTesting.CalculateDpsByType(
                DamageType,
                TargetAC,
                TestIntervalSeconds,
                StoragePerVolume,
                StoragePerCost,
                EnginePpm,
                EnginePpv,
                EnginePpc,
                EngineUsesFuel,
                TargetArmorScheme,
                ImpactAngleFromPerpendicularDegrees);
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
            }

            LoaderType loaderType = FiringPieceIsDif ? LoaderType.Dif : LoaderType.Regular;
            if (!TryGetBracket(shellUnderTesting.TotalLength, loaderType, out LoaderBracket bracket))
            {
                return; // length outside any tracked bracket
            }

            float incumbentScore;
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                incumbentScore = TopShells.TryGetValue(bracket, out Shell incumbent)
                    ? (TestType == TestType.DpsPerVolume
                        ? incumbent.DpsPerVolumePerAreaDict[DamageType]
                        : incumbent.DpsPerCostPerAreaDict[DamageType])
                    : float.NegativeInfinity;
            }
            else
            {
                incumbentScore = TopShells.TryGetValue(bracket, out Shell incumbent)
                    ? (TestType == TestType.DpsPerVolume
                        ? incumbent.DpsPerVolumeDict[DamageType]
                        : incumbent.DpsPerCostDict[DamageType])
                    : float.NegativeInfinity;
            }

            if (referenceDict[DamageType] > incumbentScore)
            {
                shellUnderTesting.CalculateInaccuracyAtBarrelLength(MaxBarrelLengthInM, MinEffectiveRangeInput);
                TopShells[bracket] = shellUnderTesting;
            }
        }

        /// <summary>
        /// Generate valid body module configurations for each loader bracket being tested.
        /// </summary>
        /// <returns>Module configurations with bracket assigned; casings and rail draw to be added by caller</returns>
        IEnumerable<ModuleConfig> GenerateProjectiles()
        {
            float[] scratchCounts = new float[VariableModuleIndices.Length];

            foreach (int headIndex in HeadIndexList)
            {
                Shell shellUnderTesting = new(
                    BarrelCount,
                    Gauge,
                    GaugeMultiplier,
                    false,
                    Module.AllModules[headIndex],
                    BaseModule,
                    RegularClipsPerLoader,
                    RegularInputsPerLoader,
                    BeltfedClipsPerLoader,
                    BeltfedInputsPerLoader,
                    UsesAmmoEjector,
                    0, // Casings will come later
                    0, // Casings will come later
                    RateOfFireRpm,
                    GunUsesRecoilAbsorbers,
                    FiringPieceIsDif);

                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);
                bool usesDefuse = shellUnderTesting.BodyModuleCounts[Module.DefuseIndex] > 0f;

                foreach (LoaderBracket bracket in LoaderBrackets)
                {
                    ResetToFixedCounts(shellUnderTesting, bracket, usesDefuse);
                    shellUnderTesting.GetModuleCounts();
                    shellUnderTesting.CalculateLengths();

                    int maxVariableModuleCount = 20 - (int)shellUnderTesting.ModuleCountTotal;
                    float maxTotalLengthForBracket = MathF.Min(MaxShellLength, bracket.MaxLengthMMInclusive);
                    float maxProjectileLengthForBracket = maxTotalLengthForBracket;
                    if (LimitBarrelLength && BarrelLengthHardLimit)
                    {
                        maxProjectileLengthForBracket = MathF.Min(
                            maxProjectileLengthForBracket,
                            shellUnderTesting.CalculateMaxProjectileLengthForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy));
                    }
                    float maxVariableLengthForBracket = maxProjectileLengthForBracket - shellUnderTesting.ProjectileLength;

                    foreach (ModuleConfig config in EnumerateVariableSlots(
                        shellUnderTesting,
                        bracket,
                        headIndex,
                        usesDefuse,
                        scratchCounts,
                        depth: 0,
                        remainingModuleBudget: maxVariableModuleCount,
                        remainingLengthBudget: maxVariableLengthForBracket,
                        maxTotalLengthForBracket: maxTotalLengthForBracket))
                    {
                        yield return config;
                    }
                }
            }
        }

        /// <summary>
        /// Recursive helper for GenerateProjectiles.
        /// </summary>
        private IEnumerable<ModuleConfig> EnumerateVariableSlots(
            Shell shellUnderTesting,
            LoaderBracket bracket,
            int headIndex,
            bool usesDefuse,
            float[] scratchCounts,
            int depth,
            int remainingModuleBudget,
            float remainingLengthBudget,
            float maxTotalLengthForBracket)
        {
            if (depth == VariableModuleIndices.Length)
            {
                ResetToFixedCounts(shellUnderTesting, bracket, usesDefuse);
                for (int i = 0; i < VariableModuleIndices.Length; i++)
                {
                    shellUnderTesting.BodyModuleCounts[VariableModuleIndices[i]] += scratchCounts[i];
                }
                shellUnderTesting.CalculateLengths();
                float maxCasingLengthForBracket = MathF.Floor(maxTotalLengthForBracket - shellUnderTesting.ProjectileLength);

                if (PassesVelocityAndRecoilChecks(shellUnderTesting, maxCasingLengthForBracket))
                {
                    yield return new ModuleConfig
                    {
                        Bracket = bracket,
                        HeadIndex = headIndex,
                        GPCount = 0,
                        RGCount = 0,
                        VariableModCounts = (float[])scratchCounts.Clone()
                    };
                }
                yield break;
            }

            // Deduplicate variable module indices
            // This can be removed if old brute-force module config generation is ever removed
            bool duplicateOfEarlierSlot = false;
            for (int prior = 0; prior < depth; prior++)
            {
                if (VariableModuleIndices[prior] == VariableModuleIndices[depth])
                {
                    duplicateOfEarlierSlot = true;
                    break;
                }
            }

            Module mod = Module.AllModules[VariableModuleIndices[depth]];
            float modLength = MathF.Min(mod.MaxLength, Gauge);
            int maxCount = duplicateOfEarlierSlot
                ? 0
                : (int)MathF.Min(remainingModuleBudget, MathF.Floor(remainingLengthBudget / modLength));

            for (int count = 0; count <= maxCount; count++)
            {
                scratchCounts[depth] = count;
                foreach (ModuleConfig config in EnumerateVariableSlots(
                    shellUnderTesting,
                    bracket,
                    headIndex,
                    usesDefuse,
                    scratchCounts,
                    depth + 1,
                    remainingModuleBudget - count,
                    remainingLengthBudget - count * modLength,
                    maxTotalLengthForBracket))
                {
                    yield return config;
                }
            }
        }

        /// <summary>
        /// Resets BodyModuleCounts to FixedModuleCounts
        /// </summary>
        private void ResetToFixedCounts(Shell shell, LoaderBracket bracket, bool usesDefuse)
        {
            FixedModuleCounts.CopyTo(shell.BodyModuleCounts, 0);
            shell.BodyModuleCounts[Module.DefuseIndex] =
                (bracket.LoaderType == LoaderType.Regular && usesDefuse) ? 1f : 0f;
        }

        /// <summary>
        /// Computes maximum total recoil achievable for any feasible (gp, rg, pd) allocation.
        /// Solves a 3-variable LP exactly by enumerating all C(9, 3) = 84 vertex candidates.
        /// </summary>
        internal static float MaxAchievableTotalRecoil(
            float gpRecoilPerCasing,
            float drawPerMod,
            float casingDrawMultiplier,
            float casingFeltRecoilMultiplier,
            float maxGPCap,
            float maxRGCap,
            float maxCasings,
            float maxDrawInput,
            float maxDrawProjectile,
            float maxFeltRecoil)
        {
            float rgDrawCoef = drawPerMod * casingDrawMultiplier;
            float rgFeltCoef = rgDrawCoef * casingFeltRecoilMultiplier;

            // 9 constraints in form a*gp + b*rg + c*pd <= d.
            // Columns: 0=a, 1=b, 2=c, 3=d.
            float[,] cons =
                {
                {  1f,                1f,         0f, maxCasings        }, // C1
                {  0f,                1f,         0f, maxRGCap           }, // C2
                {  1f,                0f,         0f, maxGPCap           }, // C3
                {  0f,                rgDrawCoef, 1f, maxDrawInput       }, // C4
                {  0f,                0f,         1f, maxDrawProjectile  }, // C5
                {  gpRecoilPerCasing, rgFeltCoef, 1f, maxFeltRecoil      }, // C6
                { -1f,                0f,         0f, 0f                 }, // C7: gp >= 0
                {  0f,               -1f,         0f, 0f                 }, // C8: rg >= 0
                {  0f,                0f,        -1f, 0f                 }, // C9: pd >= 0
            };

            float best = 0f;  // origin (0,0,0) is always feasible with objective 0
            const float singularThreshold = 1e-9f;
            const float feasEps = 1e-4f;

            for (int i = 0; i < 9; i++)
                for (int j = i + 1; j < 9; j++)
                    for (int k = j + 1; k < 9; k++)
                    {
                        float a0 = cons[i, 0], b0 = cons[i, 1], c0 = cons[i, 2], d0 = cons[i, 3];
                        float a1 = cons[j, 0], b1 = cons[j, 1], c1 = cons[j, 2], d1 = cons[j, 3];
                        float a2 = cons[k, 0], b2 = cons[k, 1], c2 = cons[k, 2], d2 = cons[k, 3];

                        float det =
                            a0 * (b1 * c2 - c1 * b2) -
                            b0 * (a1 * c2 - c1 * a2) +
                            c0 * (a1 * b2 - b1 * a2);

                        if (MathF.Abs(det) < singularThreshold) continue;

                        float gp = (
                            d0 * (b1 * c2 - c1 * b2) -
                            b0 * (d1 * c2 - c1 * d2) +
                            c0 * (d1 * b2 - b1 * d2)
                        ) / det;

                        float rg = (
                            a0 * (d1 * c2 - c1 * d2) -
                            d0 * (a1 * c2 - c1 * a2) +
                            c0 * (a1 * d2 - d1 * a2)
                        ) / det;

                        float pd = (
                            a0 * (b1 * d2 - d1 * b2) -
                            b0 * (a1 * d2 - d1 * a2) +
                            d0 * (a1 * b2 - b1 * a2)
                        ) / det;

                        // Feasibility check against all 9 constraints
                        bool feasible = true;
                        for (int c = 0; c < 9; c++)
                        {
                            float lhs = cons[c, 0] * gp + cons[c, 1] * rg + cons[c, 2] * pd;
                            float tolerance = feasEps * MathF.Max(1f, MathF.Abs(cons[c, 3]));
                            if (lhs > cons[c, 3] + tolerance)
                            {
                                feasible = false;
                                break;
                            }
                        }

                        if (feasible)
                        {
                            float obj = gpRecoilPerCasing * gp + rgDrawCoef * rg + pd;
                            if (obj > best) best = obj;
                        }
                    }

            return best;
        }

        bool PassesVelocityAndRecoilChecks(Shell shellUnderTesting, float maxCasingLength)
        {
            // Calculate min recoil
            shellUnderTesting.CalculateLengths();
            shellUnderTesting.GetModuleCounts();
            shellUnderTesting.CalculateMaxDraw();
            shellUnderTesting.CalculateVelocityModifier();
            shellUnderTesting.CalculateDamageModifierByType(DamageType.Kinetic);
            float minVelocity = MathF.Max(MinVelocityInput, TargetArmorScheme.CalculateMinVelocityToPenetrate(shellUnderTesting, ImpactAngleFromPerpendicularDegrees));
            float minTotalRecoil = shellUnderTesting.CalculateMinRecoilForVelocityAndRange(minVelocity, MinEffectiveRangeInput);
            float maxFeltRecoil = (!GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
                ? MathF.Min(MaxRecoilInput, shellUnderTesting.CalculateMaxFeltRecoilForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy))
                : MaxRecoilInput;

            float slotBudget = 20f - shellUnderTesting.ModuleCountTotal
                             + shellUnderTesting.GPCasingCount
                             + shellUnderTesting.RGCasingCount;
            float lengthBudget = maxCasingLength / Gauge;
            float maxCasings = MathF.Min(slotBudget, lengthBudget);

            float maxAchievable = MaxAchievableTotalRecoil(
                gpRecoilPerCasing: shellUnderTesting.GPRecoilPerCasing,
                drawPerMod: shellUnderTesting.DrawPerProjectileModule,
                casingDrawMultiplier: shellUnderTesting.RGCasingDrawMultiplier,
                casingFeltRecoilMultiplier: shellUnderTesting.RGCasingFeltRecoilMultiplier,
                maxGPCap: MaxGP,
                maxRGCap: MaxRGInput,
                maxCasings: maxCasings,
                maxDrawInput: MaxDrawInput,
                maxDrawProjectile: shellUnderTesting.MaxDrawProjectile,
                maxFeltRecoil: maxFeltRecoil);

            return maxAchievable >= minTotalRecoil;
        }

        /// <summary>
        /// Runs optimized algorithms to determine optimal draw depending on damage type
        /// </summary>
        /// <param name="shellUnderTesting">Shell being tested</param>
        /// <param name="maxDraw">Max draw allowed by shell, recoil, and user-input limits</param>
        /// <param name="minDraw">Min draw for velocity, effective range, and armor penetration</param>
        /// <param name="referenceDict">DPS per Cost or Volume</param>
        float CalculateOptimalRailDraw2(Shell shellUnderTesting,
            float maxDraw,
            float minDraw,
            Dictionary<DamageType, float> referenceDict)
        {
            if (DamageType != DamageType.Kinetic)
            {
                return minDraw;
            }
            else
            {
                // For kinetic, optimal draw will always be one of six fixed points
                // First three points determined by shell
                HashSet<float> drawCandidates = [
                    minDraw,
                    maxDraw,
                    MathF.Min(maxDraw, MathF.Max(minDraw, shellUnderTesting.MaxDrawCasing))];
                // Clamp calculated candidate point to integer grid (no fractional draw)
                // Test floor and ceiling for thoroughness
                void AddCandidate(float draw)
                {
                    drawCandidates.Add(MathF.Min(maxDraw, MathF.Max(minDraw, MathF.Floor(draw))));
                    drawCandidates.Add(MathF.Min(maxDraw, MathF.Max(minDraw, MathF.Ceiling(draw))));
                }

                (float score, float denominator) Evaluate(float draw)
                {
                    shellUnderTesting.RailDraw = draw;
                    shellUnderTesting.CalculateDpsByType(
                        DamageType,
                        TargetAC,
                        TestIntervalSeconds,
                        StoragePerVolume,
                        StoragePerCost,
                        EnginePpm,
                        EnginePpv,
                        EnginePpc,
                        EngineUsesFuel,
                        TargetArmorScheme,
                        ImpactAngleFromPerpendicularDegrees);
                    if (LimitBarrelLength && !BarrelLengthHardLimit)
                    {
                        shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
                    }
                    float score = referenceDict[DamageType];
                    float denominator = TestType == TestType.DpsPerVolume
                        ? shellUnderTesting.VolumePerLoader
                        : shellUnderTesting.CostPerLoader;

                    return (score, denominator);
                }

                float gpRecoil = shellUnderTesting.GPCasingCount * shellUnderTesting.GPRecoilPerCasing;

                // Over-penetration interior optimum d = A/B - 2*GPRecoil of sqrt(V)/(A + B*d) on one
                // affine piece of the denominator. Recover (A, B) from the denominator at the two
                // piece endpoints (max separation -> best conditioned). NaN if the piece is empty or
                // the denominator does not increase across it (no interior maximum).
                float FindStationaryPoint(float pieceLo, float pieceHi)
                {
                    if (pieceHi <= pieceLo)
                    {
                        return float.NaN;
                    }
                    float denomLo = Evaluate(pieceLo).denominator;
                    float denomHi = Evaluate(pieceHi).denominator;
                    float slopeB = (denomHi - denomLo) / (pieceHi - pieceLo);
                    if (slopeB <= 0f)
                    {
                        return float.NaN;
                    }
                    float interceptA = denomLo - slopeB * pieceLo;
                    return interceptA / slopeB - 2f * gpRecoil;
                }

                // Fourth point: AP = AC
                if (shellUnderTesting.OverallArmorPierceModifier > 0f && TargetAC > 0f)
                {
                    float velocityForAP = TargetAC / shellUnderTesting.OverallArmorPierceModifier / 0.0175f;
                    float recoilForAP = shellUnderTesting.CalculateMinTotalRecoilForVelocity(velocityForAP);
                    AddCandidate(recoilForAP - gpRecoil);
                }

                // Fifth point: piecewise intercept where draw is all in casings
                if (shellUnderTesting.MaxDrawCasing > minDraw)
                {
                    float casingIntercept = FindStationaryPoint(MathF.Max(minDraw, 0f), shellUnderTesting.MaxDrawCasing);
                    if (!float.IsNaN(casingIntercept))
                    {
                        AddCandidate(casingIntercept);
                    }
                }

                // Sixth point: piecewise intercept where draw overflows into projectile
                if (shellUnderTesting.MaxDrawCasing < maxDraw)
                {
                    float projectileIntercept = FindStationaryPoint(shellUnderTesting.MaxDrawCasing, maxDraw);
                    if (!float.IsNaN(projectileIntercept))
                    {
                        AddCandidate(projectileIntercept);
                    }
                }

                // Find best candidate
                float optimalDraw = minDraw;
                float bestScore = float.NegativeInfinity;
                foreach (float candidate in drawCandidates)
                {
                    float score = Evaluate(candidate).score;
                    if (score > bestScore || (score >= bestScore && candidate < optimalDraw))
                    {
                        optimalDraw = candidate;
                        bestScore = score;
                    }
                }

                return optimalDraw;
            }
        }

        /// <summary>
        /// Runs nested binary search algorithm on rail draw to determine optimum for a given GP count
        /// </summary>
        /// <param name="shellUnderTesting">Shell being tested (normal or belt)</param>
        float CalculateOptimalRailDraw(Shell shellUnderTesting,
            float maxDraw,
            float minDraw,
            Dictionary<DamageType, float> referenceDict)
        {
            if (DamageType != DamageType.Kinetic)
            {
                return MathF.Min(maxDraw, MathF.Ceiling(minDraw));
            }

            // Shortcut impossible requirement
            shellUnderTesting.RailDraw = maxDraw;
            shellUnderTesting.CalculateDpsByType(
                DamageType,
                TargetAC,
                TestIntervalSeconds,
                StoragePerVolume,
                StoragePerCost,
                EnginePpm,
                EnginePpv,
                EnginePpc,
                EngineUsesFuel,
                TargetArmorScheme,
                ImpactAngleFromPerpendicularDegrees);
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
            }

            if (referenceDict[DamageType] == 0)
            {
                return 0;
            }

            // Binary gradient ascent to find optimal draw without testing every value
            float optimalDraw = maxDraw;
            float midRange;
            float midRangeScore;
            float midRangePlus;
            float midRangePlusScore;
            float topOfRange = maxDraw;
            float bottomOfRange = minDraw;

            while (bottomOfRange + 1 < topOfRange)
            {
                midRange = MathF.Floor((topOfRange + bottomOfRange) / 2f);
                midRangePlus = midRange + 1;

                shellUnderTesting.RailDraw = midRange;
                shellUnderTesting.CalculateDpsByType(
                    DamageType,
                    TargetAC,
                    TestIntervalSeconds,
                    StoragePerVolume,
                    StoragePerCost,
                    EnginePpm,
                    EnginePpv,
                    EnginePpc,
                    EngineUsesFuel,
                    TargetArmorScheme,
                    ImpactAngleFromPerpendicularDegrees);
                if (LimitBarrelLength && !BarrelLengthHardLimit)
                {
                    shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
                }
                midRangeScore = referenceDict[DamageType];

                shellUnderTesting.RailDraw = midRangePlus;
                shellUnderTesting.CalculateDpsByType(
                    DamageType,
                    TargetAC,
                    TestIntervalSeconds,
                    StoragePerVolume,
                    StoragePerCost,
                    EnginePpm,
                    EnginePpv,
                    EnginePpc,
                    EngineUsesFuel,
                    TargetArmorScheme,
                    ImpactAngleFromPerpendicularDegrees);
                if (LimitBarrelLength && !BarrelLengthHardLimit)
                {
                    shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
                }
                midRangePlusScore = referenceDict[DamageType];

                if (midRangePlusScore == 0)
                {
                    bottomOfRange = midRangePlus;
                }
                else if (midRangeScore >= midRangePlusScore)
                {
                    topOfRange = midRange;
                    optimalDraw = midRange;
                }
                else
                {
                    bottomOfRange = midRangePlus;
                    optimalDraw = midRangePlus;
                }
            }

            // minDraw never gets checked above
            shellUnderTesting.RailDraw = optimalDraw;
            shellUnderTesting.CalculateDpsByType(
                DamageType,
                TargetAC,
                TestIntervalSeconds,
                StoragePerVolume,
                StoragePerCost,
                EnginePpm,
                EnginePpv,
                EnginePpc,
                EngineUsesFuel,
                TargetArmorScheme,
                ImpactAngleFromPerpendicularDegrees);
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
            }
            float optimalScore = referenceDict[DamageType];

            shellUnderTesting.RailDraw = minDraw;
            shellUnderTesting.CalculateDpsByType(
                DamageType,
                TargetAC,
                TestIntervalSeconds,
                StoragePerVolume,
                StoragePerCost,
                EnginePpm,
                EnginePpv,
                EnginePpc,
                EngineUsesFuel,
                TargetArmorScheme,
                ImpactAngleFromPerpendicularDegrees);
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
            }
            if (referenceDict[DamageType] >= optimalScore)
            {
                optimalDraw = minDraw;
            }

            return optimalDraw;
        }

        /// <summary>
        /// Find DPS/Cost or DPS/Volume of given shell
        /// </summary>
        /// <param name="gpCount">GP casing count</param>
        /// <param name="rgCount">RG casing count</param>
        /// <returns>DPS per Cost or DPS per Volume, depending on test type</returns>
        (float score, Shell shell) ShellTest2(
            Module head,
            float[] variableModCounts,
            float gpCount,
            float rgCount,
            LoaderBracket bracket)
        {
            bool isBelt = bracket.LoaderType == LoaderType.Belt;
            float score = 0;
            Shell shellUnderTesting = new(
                BarrelCount,
                Gauge,
                GaugeMultiplier,
                isBelt,
                head,
                BaseModule,
                RegularClipsPerLoader,
                RegularInputsPerLoader,
                BeltfedClipsPerLoader,
                BeltfedInputsPerLoader,
                UsesAmmoEjector,
                gpCount,
                rgCount,
                RateOfFireRpm,
                GunUsesRecoilAbsorbers,
                FiringPieceIsDif
                );
            FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);
            bool usesDefuse = shellUnderTesting.BodyModuleCounts[Module.DefuseIndex] > 0f;

            ResetToFixedCounts(shellUnderTesting, bracket, usesDefuse);

            // Add variable modules
            for (int i = 0; i < variableModCounts.Length; i++)
            {
                int moduleIndex = VariableModuleIndices[i];
                float moduleCount = variableModCounts[i];
                shellUnderTesting.BodyModuleCounts[moduleIndex] += moduleCount;
            }

            shellUnderTesting.GetModuleCounts();
            shellUnderTesting.CalculateLengths();
            shellUnderTesting.CalculateVelocityModifier();
            shellUnderTesting.CalculateDamageModifierByType(DamageType);
            shellUnderTesting.CalculateMaxDraw();
            shellUnderTesting.CalculateRecoil(); // GPRecoil is 0 on a fresh shell until calculated

            // Physical draw limit
            float maxDraw = MathF.Min(shellUnderTesting.MaxDrawShell, MaxDrawInput);
            // Limit by recoil. Projectile draw is 1 : 1 with recoil; draw applied to RG casings is affected by multiplier
            float maxRailRecoil = MaxRecoilInput - shellUnderTesting.GPRecoil;
            float maxCasingDrawForRecoil = MathF.Min(maxRailRecoil / shellUnderTesting.RGCasingFeltRecoilMultiplier, shellUnderTesting.MaxDrawCasing);
            float maxCasingRecoil = maxCasingDrawForRecoil * shellUnderTesting.RGCasingFeltRecoilMultiplier;
            float maxProjectileDrawForRecoil = maxRailRecoil - maxCasingRecoil;
            float maxDrawForRecoil = maxCasingDrawForRecoil + maxProjectileDrawForRecoil;
            maxDraw = MathF.Min(maxDraw, maxDrawForRecoil);
            // Limit by inaccuracy
            if (!shellUnderTesting.GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
            {
                maxDraw = MathF.Min(maxDraw, shellUnderTesting.CalculateMaxDrawForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy, shellUnderTesting.MaxDrawCasing));
            }
            float minVelocity = MathF.Max(MinVelocityInput,
                TargetArmorScheme.CalculateMinVelocityToPenetrate(shellUnderTesting, ImpactAngleFromPerpendicularDegrees));
            float minDraw = shellUnderTesting.CalculateMinDrawForVelocityAndRange(minVelocity, MinEffectiveRangeInput);

            maxDraw = MathF.Floor(maxDraw);
            minDraw = MathF.Ceiling(minDraw);
            if (maxDraw >= minDraw)
            {
                shellUnderTesting.CalculateReloadTime(TestIntervalSeconds);
                shellUnderTesting.SabotAngleMultiplier = SabotAngleMultiplier;
                shellUnderTesting.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
                shellUnderTesting.CalculateDamageByType(DamageType, FragAngleMultiplier);

                // Users can enter minimum disruptor values even when optimizing for other damage types
                if (MinDisruptor > 0 && DamageType != DamageType.Disruptor)
                {
                    shellUnderTesting.CalculateDamageByType(DamageType.EMP, FragAngleMultiplier);
                    shellUnderTesting.CalculateDamageByType(DamageType.Disruptor, FragAngleMultiplier);
                }

                if ((shellUnderTesting.DamageDict[DamageType.Disruptor] >= MinDisruptor) || MinDisruptor == 0)
                {
                    shellUnderTesting.CalculateCooldownTime();
                    shellUnderTesting.CalculateCoolerVolumeAndCost();
                    shellUnderTesting.CalculateLoaderVolumeAndCost();
                    shellUnderTesting.CalculateVariableVolumesAndCosts(TestIntervalSeconds, StoragePerVolume, StoragePerCost);

                    // Determine which "DPS Per" dictionary will be used for testing
                    Dictionary<DamageType, float> referenceDict;
                    if (LimitBarrelLength && !BarrelLengthHardLimit)
                    {
                        referenceDict = TestType == TestType.DpsPerVolume ?
                            shellUnderTesting.DpsPerVolumePerAreaDict : shellUnderTesting.DpsPerCostPerAreaDict;
                    }
                    else
                    {
                        referenceDict = TestType == TestType.DpsPerVolume ?
                            shellUnderTesting.DpsPerVolumeDict : shellUnderTesting.DpsPerCostDict;
                    }
                    // Determine optimal rail draw
                    float optimalDraw = maxDraw > 0 ?
                        CalculateOptimalRailDraw2(shellUnderTesting, maxDraw, minDraw, referenceDict)
                        : 0;
                    shellUnderTesting.RailDraw = optimalDraw;
                    shellUnderTesting.CalculateDpsByType(
                        DamageType,
                        TargetAC,
                        TestIntervalSeconds,
                        StoragePerVolume,
                        StoragePerCost,
                        EnginePpm,
                        EnginePpv,
                        EnginePpc,
                        EngineUsesFuel,
                        TargetArmorScheme,
                        ImpactAngleFromPerpendicularDegrees);
                    if (LimitBarrelLength && !BarrelLengthHardLimit)
                    {
                        shellUnderTesting.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
                    }
                    score = referenceDict[DamageType];
                }
            }

            return (score, shellUnderTesting);
        }

        /// <summary>
        /// Scores a (gp, rg) point on an already-configured scratch shell, changing only casing counts.
        /// Avoids creating new shells for every coördinate on (gp, rg) graph
        /// </summary>
        private float ScoreCasingsOnScratch(Shell shell, float gpCount, float rgCount, float minVelocity)
        {
            shell.GPCasingCount = gpCount;
            shell.RGCasingCount = rgCount;

            shell.CalculateLengths();
            shell.CalculateMaxDraw();
            shell.CalculateRecoil(); // GPRecoil is stale from the previous (gp, rg) point until recalculated

            float maxDraw = MathF.Min(shell.MaxDrawShell, MaxDrawInput);
            float maxRailRecoil = MaxRecoilInput - shell.GPRecoil;
            float maxCasingDrawForRecoil = MathF.Min(maxRailRecoil / shell.RGCasingFeltRecoilMultiplier, shell.MaxDrawCasing);
            float maxCasingRecoil = maxCasingDrawForRecoil * shell.RGCasingFeltRecoilMultiplier;
            float maxProjectileDrawForRecoil = maxRailRecoil - maxCasingRecoil;
            maxDraw = MathF.Min(maxDraw, maxCasingDrawForRecoil + maxProjectileDrawForRecoil);
            if (!shell.GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
                maxDraw = MathF.Min(maxDraw, shell.CalculateMaxDrawForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy, shell.MaxDrawCasing));

            float minDraw = shell.CalculateMinDrawForVelocityAndRange(minVelocity, MinEffectiveRangeInput);

            maxDraw = MathF.Floor(maxDraw);
            minDraw = MathF.Ceiling(minDraw);
            if (maxDraw < minDraw)
                return 0f;

            shell.CalculateReloadTime(TestIntervalSeconds);
            shell.SabotAngleMultiplier = SabotAngleMultiplier;
            shell.NonSabotAngleMultiplier = NonSabotAngleMultiplier;
            shell.CalculateDamageByType(DamageType, FragAngleMultiplier);

            if (MinDisruptor > 0 && DamageType != DamageType.Disruptor)
            {
                shell.CalculateDamageByType(DamageType.EMP, FragAngleMultiplier);
                shell.CalculateDamageByType(DamageType.Disruptor, FragAngleMultiplier);
            }
            if (MinDisruptor != 0 && shell.DamageDict[DamageType.Disruptor] < MinDisruptor)
                return 0f;

            shell.CalculateCooldownTime();
            shell.CalculateCoolerVolumeAndCost();
            shell.CalculateLoaderVolumeAndCost();
            shell.CalculateVariableVolumesAndCosts(TestIntervalSeconds, StoragePerVolume, StoragePerCost);

            Dictionary<DamageType, float> referenceDict;
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                referenceDict = TestType == TestType.DpsPerVolume
                    ? shell.DpsPerVolumePerAreaDict : shell.DpsPerCostPerAreaDict;
            }
            else
            {
                referenceDict = TestType == TestType.DpsPerVolume
                    ? shell.DpsPerVolumeDict : shell.DpsPerCostDict;
            }

            shell.RailDraw = maxDraw > 0
                ? CalculateOptimalRailDraw2(shell, maxDraw, minDraw, referenceDict) : 0;
            shell.CalculateDpsByType(
                DamageType, TargetAC, TestIntervalSeconds, StoragePerVolume, StoragePerCost,
                EnginePpm, EnginePpv, EnginePpc, EngineUsesFuel, TargetArmorScheme,
                ImpactAngleFromPerpendicularDegrees);
            shell.CalculateEffectiveRange();
            if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                shell.CalculateDpsPerAreaByType(MaxBarrelLengthInM, MinEffectiveRangeInput);
            }

            return referenceDict[DamageType];
        }

        /// <summary>
        /// Given an RG increment, compute (min, max) GP-increment range that satisfies
        /// bracket length and recoil constraints. Returns (0, -1) when range is empty
        /// </summary>
        internal static (int minGPInc, int maxGPInc) CalculateGPIncrementBounds(
            int rgInc,
            float gridSpacing,
            float minCasingCountForBracket,
            float maxCasingCountForBracket,
            float minCasingRecoil,
            float maxFeltRecoil,
            float drawPerCasing,
            float feltRecoilPerCasing,
            float gpRecoilPerCasing,
            float maxGPCap)
        {
            float rgCount = rgInc * gridSpacing;
            float minGPForLength = minCasingCountForBracket - rgCount;
            float minGPForRecoil = (minCasingRecoil - rgCount * drawPerCasing) / gpRecoilPerCasing;
            float maxGPForLength = maxCasingCountForBracket - rgCount;
            float maxGPForRecoil = (maxFeltRecoil - rgCount * feltRecoilPerCasing) / gpRecoilPerCasing;

            float minGP = MathF.Max(0f, MathF.Max(minGPForLength, minGPForRecoil));
            float maxGP = MathF.Min(maxGPCap, MathF.Min(maxGPForLength, maxGPForRecoil));

            return ((int)MathF.Ceiling(minGP / gridSpacing), (int)MathF.Ceiling(maxGP / gridSpacing));
        }

        internal static HashSet<Neighborhood> FindCoarsePeaks(
            int maxRGIncrementCount,
            float gridSpacing,
            Func<int, (int minGPInc, int maxGPInc)> gpRangeAtRG,
            Func<float, float, float> score)
        {
            HashSet<Neighborhood> peaks = [];

            // Each grid point is read as a center once and as a neighbor by up to four
            // adjacent cells. Cache by integer coördinate so score() runs once per point.
            Dictionary<(int gpInc, int rgInc), float> scoreCache = [];
            float Scored(int gpInc, int rgInc)
            {
                if (scoreCache.TryGetValue((gpInc, rgInc), out float cached))
                    return cached;
                float computed = score(gpInc * gridSpacing, rgInc * gridSpacing);
                scoreCache[(gpInc, rgInc)] = computed;
                return computed;
            }

            for (int rgInc = 0; rgInc <= maxRGIncrementCount; rgInc++)
            {
                (int minGPInc, int maxGPInc) = gpRangeAtRG(rgInc);
                for (int gpInc = minGPInc; gpInc <= maxGPInc; gpInc++)
                {
                    float gp = gpInc * gridSpacing;
                    float rg = rgInc * gridSpacing;
                    float centerScore = Scored(gpInc, rgInc);

                    bool hasLeft = gpInc > minGPInc;
                    bool hasRight = gpInc < maxGPInc;

                    bool hasBottom = false;
                    if (rgInc > 0)
                    {
                        (int loBelow, int hiBelow) = gpRangeAtRG(rgInc - 1);
                        hasBottom = gpInc >= loBelow && gpInc <= hiBelow;
                    }

                    bool hasTop = false;
                    if (rgInc < maxRGIncrementCount)
                    {
                        (int loAbove, int hiAbove) = gpRangeAtRG(rgInc + 1);
                        hasTop = gpInc >= loAbove && gpInc <= hiAbove;
                    }

                    bool isPeak =
                        centerScore > 0
                     && (!hasLeft || Scored(gpInc - 1, rgInc) <= centerScore)
                     && (!hasRight || Scored(gpInc + 1, rgInc) <= centerScore)
                     && (!hasBottom || Scored(gpInc, rgInc - 1) <= centerScore)
                     && (!hasTop || Scored(gpInc, rgInc + 1) <= centerScore);

                    if (isPeak)
                    {
                        float bottomRG = Math.Max(rgInc - 1, 0) * gridSpacing;
                        float topRG = Math.Min(rgInc + 1, maxRGIncrementCount) * gridSpacing;
                        peaks.Add(new Neighborhood(gp, rg, bottomRG, topRG));
                    }
                }
            }
            return peaks;
        }

        /// <summary>
        /// Fine-grid refinement within a single Neighborhood. Returns best-scoring
        /// (gp, rg) point and its score. Score function is parameterized for testability.
        /// </summary>
        internal static (float gp, float rg, float score) RefineToFinePeak(
            Neighborhood hood,
            float spacing,
            Func<float, (float minGP, float maxGP)> gpBoundsAtRG,
            Func<float, float, float> score)
        {
            int bottomRGInc = (int)MathF.Ceiling(hood.BottomRG / spacing);
            int topRGInc = (int)MathF.Floor(hood.TopRG / spacing);

            float bestGP = 0f;
            float bestRG = 0f;
            float bestScore = float.NegativeInfinity;

            for (int rgInc = bottomRGInc; rgInc <= topRGInc; rgInc++)
            {
                float rg = spacing * rgInc;
                (float minGP, float maxGP) = gpBoundsAtRG(rg);
                int minGPInc = (int)MathF.Ceiling(minGP / spacing);
                int maxGPInc = (int)MathF.Floor(maxGP / spacing);

                for (int gpInc = minGPInc; gpInc <= maxGPInc; gpInc++)
                {
                    float gp = spacing * gpInc;
                    float currentScore = score(gp, rg);
                    if (currentScore > bestScore)
                    {
                        bestGP = gp;
                        bestRG = rg;
                        bestScore = currentScore;
                    }
                }
            }
            return (bestGP, bestRG, bestScore);
        }

        /// <summary>
        /// Test shells using refined search algorithm
        /// </summary>
        public void BigTest()
        {
            foreach (ModuleConfig modConfig in GenerateProjectiles())
            {
                bool isBelt = modConfig.Bracket.LoaderType == LoaderType.Belt;
                Shell shellUnderTesting = new(
                    BarrelCount,
                    Gauge,
                    GaugeMultiplier,
                    isBelt,
                    Module.AllModules[modConfig.HeadIndex],
                    BaseModule,
                    RegularClipsPerLoader,
                    RegularInputsPerLoader,
                    BeltfedClipsPerLoader,
                    BeltfedInputsPerLoader,
                    UsesAmmoEjector,
                    0, // no casings yet
                    0, // no casings yet
                    RateOfFireRpm,
                    GunUsesRecoilAbsorbers,
                    FiringPieceIsDif
                    );
                FixedModuleCounts.CopyTo(shellUnderTesting.BodyModuleCounts, 0);

                // Add variable modules
                for (int i = 0; i < modConfig.VariableModCounts.Length; i++)
                {
                    int moduleIndex = VariableModuleIndices[i];
                    float moduleCount = modConfig.VariableModCounts[i];
                    shellUnderTesting.BodyModuleCounts[moduleIndex] += moduleCount;
                }

                shellUnderTesting.CalculateLengths();
                shellUnderTesting.GetModuleCounts();
                shellUnderTesting.CalculateMaxDraw();
                shellUnderTesting.CalculateVelocityModifier();
                float minShellRecoilForVelocityAndRange = shellUnderTesting.CalculateMinRecoilForVelocityAndRange(MinVelocityInput, MinEffectiveRangeInput);
                float minCasingRecoil = minShellRecoilForVelocityAndRange - MathF.Min(MaxDrawInput, shellUnderTesting.MaxDrawShell);

                float drawPerCasing = shellUnderTesting.DrawPerProjectileModule * shellUnderTesting.RGCasingDrawMultiplier;
                float gpRecoilPerCasing = shellUnderTesting.GPRecoilPerCasing;

                float maxCasingCountForModule = 20f - shellUnderTesting.ModuleCountTotal;
                float maxFeltRecoil = (!GunUsesRecoilAbsorbers && LimitBarrelLength && BarrelLengthHardLimit)
                    ? MathF.Min(MaxRecoilInput, shellUnderTesting.CalculateMaxFeltRecoilForInaccuracy(MaxBarrelLengthInM, MaxInaccuracy))
                    : MaxRecoilInput;

                float feltRecoilPerCasing = shellUnderTesting.DrawPerProjectileModule
                    * shellUnderTesting.RGCasingDrawMultiplier
                    * shellUnderTesting.RGCasingFeltRecoilMultiplier;
                float maxRGCasings = MathF.Min(MaxRGInput, maxFeltRecoil / feltRecoilPerCasing);

                float minLengthForBracket = MathF.Max(modConfig.Bracket.MinLengthMMExclusive, MinShellLength);
                float maxLengthForBracket = MathF.Min(modConfig.Bracket.MaxLengthMMInclusive, MaxShellLength);
                float minCasingCountForBracket = MathF.Max(0f, (minLengthForBracket - shellUnderTesting.ProjectileLength) / Gauge + 0.01f);
                float maxCasingCountForBracket = MathF.Min(maxCasingCountForModule, (maxLengthForBracket - shellUnderTesting.ProjectileLength) / Gauge);

                float gridSpacing = MathF.Max(1f, CasingIncrement);
                float fineSearchSpacing = CasingIncrement;
                int maxRGIncrementCount = (int)MathF.Ceiling(MathF.Min(maxRGCasings, maxCasingCountForBracket) / gridSpacing);

                // Capture loop locals for lambdas so they bind cleanly
                Module headForScoring = shellUnderTesting.HeadModule;
                float[] varCountsForScoring = modConfig.VariableModCounts;

                (int, int) coarseGPRange(int rgInc) => CalculateGPIncrementBounds(
                    rgInc, gridSpacing,
                    minCasingCountForBracket, maxCasingCountForBracket,
                    minCasingRecoil, maxFeltRecoil,
                    drawPerCasing, feltRecoilPerCasing, gpRecoilPerCasing,
                    MaxGP);

                (float, float) fineGPBounds(float rgCount)
                {
                    float minGPForLength = minCasingCountForBracket - rgCount;
                    float minGPForRecoil = (minCasingRecoil - rgCount * drawPerCasing) / gpRecoilPerCasing;
                    float maxGPForLength = maxCasingCountForBracket - rgCount;
                    float maxGPForRecoil = (maxFeltRecoil - rgCount * feltRecoilPerCasing) / gpRecoilPerCasing;
                    float minGP = MathF.Max(0f, MathF.Max(minGPForLength, minGPForRecoil));
                    float maxGP = MathF.Min(MaxGP, MathF.Min(maxGPForLength, maxGPForRecoil));
                    return (minGP, maxGP);
                }

                shellUnderTesting.CalculateDamageModifierByType(DamageType);
                float minVelocity = MathF.Max(MinVelocityInput,
                    TargetArmorScheme.CalculateMinVelocityToPenetrate(shellUnderTesting, ImpactAngleFromPerpendicularDegrees));

                float scoreFunc(float gp, float rg) => ScoreCasingsOnScratch(shellUnderTesting, gp, rg, minVelocity);

                HashSet<Neighborhood> peaks = FindCoarsePeaks(maxRGIncrementCount, gridSpacing, coarseGPRange, scoreFunc);

                foreach (Neighborhood hood in peaks)
                {
                    (float gp, float rg, float bestScore) = RefineToFinePeak(hood, fineSearchSpacing, fineGPBounds, scoreFunc);

                    float incumbentScore = TopShells.TryGetValue(modConfig.Bracket, out Shell incumbentShell)
                        ? (TestType == TestType.DpsPerCost
                            ? incumbentShell.DpsPerCostDict[DamageType]
                            : incumbentShell.DpsPerVolumeDict[DamageType])
                        : float.NegativeInfinity;

                    if (bestScore > incumbentScore)
                    {
                        Shell winner = ShellTest2(headForScoring, varCountsForScoring, gp, rg, modConfig.Bracket).shell;
                        winner.CalculateInaccuracyAtBarrelLength(MaxBarrelLengthInM, MinEffectiveRangeInput);
                        TopShells[modConfig.Bracket] = winner;
                    }
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
        public void WriteTopShells(float minGauge, float maxGauge, TimeSpan ts, bool useBruteForce)
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

            foreach (int index in HeadIndexList)
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

            WriteTopShellsToFile(minGauge, maxGauge, showGP, showRG, showDraw, dtToShow, modsToShow, ts, useBruteForce);
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
            List<int> modsToShow,
            TimeSpan ts,
            bool useBruteForce)

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

            if (HeadIndexList.Count == 1)
            {
                writer.WriteLine("Head" + ColumnDelimiter + Module.AllModules[HeadIndexList[0]].Name);
            }
            else
            {
                writer.WriteLine("Heads:");
                foreach (int headIndex in HeadIndexList)
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
            List<int> uniqueVarModIndices = [.. VariableModuleIndices.Distinct()];
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
            if (MaxGPInput > 0 || MaxRGInput > 0)
            {
                writer.WriteLine("Casing Increment" + ColumnDelimiter + CasingIncrement);
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
            if (!LimitBarrelLength || BarrelLengthHardLimit)
            {
                // Min effective range becomes engagement range for soft barrel length limit tests
                writer.WriteLine("Min effective range (m)" + ColumnDelimiter + MinEffectiveRangeInput);
            }
            if (LimitBarrelLength && BarrelLengthHardLimit)
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
            else if (LimitBarrelLength && !BarrelLengthHardLimit)
            {
                writer.WriteLine("Soft barrel length limit");
                writer.WriteLine("Dividing DPS by impact area at " + MinEffectiveRangeInput + " m");
                if (BarrelLengthLimitType == BarrelLengthLimit.Calibers)
                {
                    writer.WriteLine("Max barrel length for propellant burn (calibers)" + ColumnDelimiter + MaxBarrelLengthInCalibers);
                }
                else
                {
                    writer.WriteLine("Max barrel length for propellant burn (m)" + ColumnDelimiter + MaxBarrelLengthInM);
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

            if (useBruteForce)
            {
                writer.WriteLine("Using brute force to test all possible shells");
            }

            writer.WriteLine($"Test completed in {ts}");
            writer.WriteLine();


            // Per-bracket winners in deterministic column order (LoaderBrackets order),
            // filtered to brackets that produced a feasible shell.
            List<string> bracketNames = [];
            List<Shell> bracketWinners = [];
            foreach (LoaderBracket bracket in LoaderBrackets)
            {
                if (TopShells.TryGetValue(bracket, out Shell winner)
                    && winner.DpsDict[DamageType] > 0)
                {
                    bracketNames.Add(bracket.DisplayName);
                    bracketWinners.Add(winner);
                }
            }

            // Determine whether any shells met test criteria
            bool shellsToPrint = bracketWinners.Count > 0;

            if (!shellsToPrint)
            {
                writer.WriteLine("No shells meet test criteria. Check test parameters.");
            }
            else
            {
                writer.WriteLine("Shells");
                foreach (Shell topShell in bracketWinners)
                {
                    // Calculate barrel lengths
                    topShell.CalculateRequiredBarrelLengths(MaxInaccuracy);
                    float shellMaxBarrelLengthInM = BarrelLengthLimitType == BarrelLengthLimit.Calibers
                        ? MaxBarrelLengthInCalibers * topShell.Gauge / 1000f
                        : MaxBarrelLengthInM;
                    if (dtToShow[DamageType.Disruptor]
                        || dtToShow[DamageType.EMP]
                        || dtToShow[DamageType.MD]
                        || dtToShow[DamageType.Frag]
                        || dtToShow[DamageType.HE]
                        || dtToShow[DamageType.HEAT]
                        || dtToShow[DamageType.Incendiary])
                    {
                        topShell.CalculateChemModifier();
                    }

                    // Calculate all damage and DPS -- including those not used for optimizing
                    foreach (DamageType dt in dtToShow.Keys)
                    {
                        if (dtToShow[dt])
                        {
                            topShell.CalculateDamageByType(dt, FragAngleMultiplier);
                            topShell.CalculateDpsByType(
                                dt,
                                TargetAC,
                                TestIntervalSeconds,
                                StoragePerVolume,
                                StoragePerCost,
                                EnginePpm,
                                EnginePpv,
                                EnginePpc,
                                EngineUsesFuel,
                                TargetArmorScheme,
                                ImpactAngleFromPerpendicularDegrees);
                            if (LimitBarrelLength && !BarrelLengthHardLimit)
                            {
                                topShell.CalculateDpsPerAreaByType(shellMaxBarrelLengthInM, MinEffectiveRangeInput);
                            }
                        }
                    }
                    if (LimitBarrelLength)
                    {
                        // Refresh inaccuracy stat (or calculate for first time if using hard length limits)
                        topShell.CalculateInaccuracyAtBarrelLength(shellMaxBarrelLengthInM, MinEffectiveRangeInput);
                    }
                    topShell.GetModuleCounts();
                }

                List<string> loaderSizeList =
                [
                    " ", .. bracketNames
                ];
                writer.WriteLine(string.Join(ColumnDelimiter, loaderSizeList));

                List<string> gaugeList =
                [
                    "Gauge (mm)"
                ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(gaugeList, topShell.Gauge, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, gaugeList));

                List<string> totalLengthList =
                [
                    "Total length (mm)"
                ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(totalLengthList, topShell.TotalLength, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, totalLengthList));

                List<string> lengthWithoutCasingsList =
                [
                    "Length without casings (mm)"
                ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(lengthWithoutCasingsList, topShell.ProjectileLength, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, lengthWithoutCasingsList));

                List<string> totalModulesList =
                [
                    "Total modules"
                ];
                foreach (Shell topShell in bracketWinners)
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
                    foreach (Shell topShell in bracketWinners)
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
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(rgCasingList, topShell.RGCasingCount, 2);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, rgCasingList));
                }

                foreach (int index in modsToShow)
                {
                    List<string> modCountList =
                    [
                        Module.AllModules[index].Name
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(modCountList, topShell.BodyModuleCounts[index], 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, modCountList));
                }

                List<string> headList =
                [
                    "Head"
                ];
                foreach (Shell topShell in bracketWinners)
                {
                    headList.Add(topShell.HeadModule.Name);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, headList));

                if (showDraw)
                {
                    List<string> railDrawList =
                    [
                        "Rail draw"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(railDrawList, topShell.RailDraw, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, railDrawList));
                }

                // Total recoil = draw if no GP
                if (MaxGPInput > 0)
                {
                    List<string> totalRecoilList =
                        [
                        "Total Recoil"
                        ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(totalRecoilList, topShell.TotalRecoil, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, totalRecoilList));
                }

                // RG casings enjoy a reduction in felt recoil from rail draw
                if (MaxRGInput > 0)
                {
                    List<string> feltRecoilList =
                    [
                        "Felt Recoil"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(feltRecoilList, topShell.FeltRecoil, 0);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, feltRecoilList));
                }


                List<string> velocityModifierList =
                    [
                    "Velocity modifier"
                    ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(velocityModifierList, topShell.OverallVelocityModifier, 2);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, velocityModifierList));

                List<string> velocityList =
                    [
                    "Velocity (m/s)"
                    ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(velocityList, topShell.Velocity, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, velocityList));

                List<string> effectiveRangeList =
                    [
                    "Effective range (m)"
                    ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(effectiveRangeList, topShell.EffectiveRange, 0);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, effectiveRangeList));

                List<string> inaccuracyModifierList =
                    [
                    "Inaccuracy modifier"
                    ];
                foreach (Shell topShell in bracketWinners)
                {
                    AddValueToList(inaccuracyModifierList, topShell.OverallInaccuracyModifier, 0, true);
                }
                writer.WriteLine(string.Join(ColumnDelimiter, inaccuracyModifierList));

                if (LimitBarrelLength && BarrelLengthLimitType == BarrelLengthLimit.Calibers)
                {
                    // Limit in calibers translates to a different length in m for each gauge
                    List<string> maxBarrelLengthList =
                        [
                        BarrelLengthHardLimit
                            ? "Max barrel length (m)"
                            : "Max barrel length for propellant burn (m)"
                        ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(maxBarrelLengthList, MaxBarrelLengthInCalibers * topShell.Gauge / 1000f, 1);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, maxBarrelLengthList));
                }

                List<string> barrelLengthInaccuracyList =
                    [
                    "Barrel length for inaccuracy (m)"
                    ];
                foreach (Shell topShell in bracketWinners)
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
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(barrelLengthPropellantBurnList, topShell.BarrelLengthForPropellant, 1);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, barrelLengthPropellantBurnList));
                }

                if (LimitBarrelLength)
                {
                    List<string> inaccuracyAtLengthList =
                        [
                        "Inaccuracy at max barrel length (°)"
                        ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(inaccuracyAtLengthList, topShell.InaccuracyAtBarrelLengthLimit, 2);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, inaccuracyAtLengthList));
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
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(kdModifierList, topShell.OverallKineticDamageModifier, 2);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, kdModifierList));

                            List<string> rawKDList =
                            [
                                "Raw KD"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(rawKDList, topShell.RawKD, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, rawKDList));

                            List<string> apModifierList =
                            [
                                "AP modifier"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(apModifierList, topShell.OverallArmorPierceModifier, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, apModifierList));

                            List<string> apList =
                            [
                                "AP"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(apList, topShell.ArmorPierce, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, apList));

                            List<string> kdAPList =
                            [
                                "KD * AP"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(kdAPList, topShell.ArmorPierce * topShell.RawKD, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, kdAPList));

                            List<string> kdMultiplierList =
                            [
                                "KD multiplier from angle"
                            ];
                            foreach (Shell topShell in bracketWinners)
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
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(fragCountList, topShell.FragCount, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fragCountList));

                            List<string> damagePerFragList =
                            [
                                "Damage per frag"
                            ];
                            foreach (Shell topShell in bracketWinners)
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
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(mdExplosionRadiusList, topShell.MDExplosionRadius, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, mdExplosionRadiusList));
                        }
                        else if (dt == DamageType.HE)
                        {
                            List<string> rawHEDamageList =
                            [
                                "Raw HE damage"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(rawHEDamageList, topShell.RawHE, 0);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, rawHEDamageList));

                            List<string> heExplosionRadiusList =
                            [
                                "HE explosion radius (m)"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                AddValueToList(heExplosionRadiusList, topShell.HEExplosionRadius, 1);
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, heExplosionRadiusList));
                        }

                        List<string> damageList =
                        [
                            (DamageType)(int)dt + " damage"
                        ];
                        foreach (Shell topShell in bracketWinners)
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
                foreach (Shell topShell in bracketWinners)
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
                    foreach (Shell topShell in bracketWinners)
                    {
                        AddValueToList(clusterReloadTimeList, topShell.ClusterReloadTime, 2);
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, clusterReloadTimeList));
                }

                List<string> uptimeList =
                    [
                    "Uptime"
                    ];
                foreach (Shell topShell in bracketWinners)
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
                        foreach (Shell topShell in bracketWinners)
                        {
                            dpsList.Add(topShell.DpsDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsList));
                    }
                }

                foreach (DamageType dt in dtToShow.Keys)
                {
                    if (dtToShow[dt])
                    {
                        List<string> dpsPerVolumeList =
                        [
                            (DamageType)(int)dt + " DPS per volume"
                        ];
                        foreach (Shell topShell in bracketWinners)
                        {
                            dpsPerVolumeList.Add(topShell.DpsPerVolumeDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsPerVolumeList));

                        List<string> dpsPerCostList =
                        [
                            (DamageType)(int)dt + " DPS per cost"
                        ];
                        foreach (Shell topShell in bracketWinners)
                        {
                            dpsPerCostList.Add(topShell.DpsPerCostDict[dt].ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, dpsPerCostList));
                    }
                }

                if (LimitBarrelLength && !BarrelLengthHardLimit)
                {
                    List<string> impactAreaList =
                        [
                            "Impact area (m^2)"
                        ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        impactAreaList.Add(topShell.ImpactArea.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, impactAreaList));

                    foreach (DamageType dt in dtToShow.Keys)
                    {
                        if (dtToShow[dt])
                        {
                            List<string> dpsPerVolumeList =
                            [
                                (DamageType)(int)dt + " DPS per volume per area"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                dpsPerVolumeList.Add(topShell.DpsPerVolumePerAreaDict[dt].ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, dpsPerVolumeList));

                            List<string> dpsPerCostList =
                            [
                                (DamageType)(int)dt + " DPS per cost per area"
                            ];
                            foreach (Shell topShell in bracketWinners)
                            {
                                dpsPerCostList.Add(topShell.DpsPerCostPerAreaDict[dt].ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, dpsPerCostList));
                        }
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
                    foreach (Shell topShell in bracketWinners)
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
                        foreach (Shell topShell in bracketWinners)
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
                        foreach (Shell topShell in bracketWinners)
                        {
                            chargerVolumeList.Add(topShell.ChargerVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, chargerVolumeList));

                        List<string> engineVolumeList =
                        [
                            "Engine volume"
                        ];
                        foreach (Shell topShell in bracketWinners)
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
                            foreach (Shell topShell in bracketWinners)
                            {
                                fuelAccessVolumeList.Add(topShell.FuelAccessVolume.ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fuelAccessVolumeList));
                        }


                        List<string> fuelStorageVolumeList =
                        [
                            "Fuel storage volume"
                        ];
                        foreach (Shell topShell in bracketWinners)
                        {
                            fuelStorageVolumeList.Add(topShell.FuelStorageVolume.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelStorageVolumeList));
                    }

                    List<string> recoilVolumeList =
                    [
                        "Recoil volume"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        recoilVolumeList.Add(topShell.RecoilVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, recoilVolumeList));

                    List<string> ammoAccessVolumeList =
                    [
                        "Ammo access volume"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        ammoAccessVolumeList.Add(topShell.AmmoAccessVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoAccessVolumeList));

                    List<string> ammoStorageVolumeList =
                    [
                        "Ammo storage volume"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        ammoStorageVolumeList.Add(topShell.AmmoStorageVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoStorageVolumeList));

                    List<string> totalVolumeList =
                    [
                        "Total volume"
                    ];
                    foreach (Shell topShell in bracketWinners)
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
                    foreach (Shell topShell in bracketWinners)
                    {
                        costPerShellList.Add(topShell.CostPerShell.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, costPerShellList));

                    List<string> loaderCostList =
                    [
                        "Loader cost"
                    ];
                    foreach (Shell topShell in bracketWinners)
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
                        foreach (Shell topShell in bracketWinners)
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
                        foreach (Shell topShell in bracketWinners)
                        {
                            chargerCostList.Add(topShell.ChargerCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, chargerCostList));

                        List<string> fuelBurnedList =
                        [
                            "Fuel burned"
                        ];
                        foreach (Shell topShell in bracketWinners)
                        {
                            fuelBurnedList.Add(topShell.FuelBurned.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelBurnedList));

                        List<string> engineCostList =
                        [
                            "Engine cost"
                        ];
                        foreach (Shell topShell in bracketWinners)
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
                            foreach (Shell topShell in bracketWinners)
                            {
                                fuelAccessCostList.Add(topShell.FuelAccessCost.ToString());
                            }
                            writer.WriteLine(string.Join(ColumnDelimiter, fuelAccessCostList));
                        }

                        List<string> fuelStorageCostList =
                        [
                            "Fuel storage cost"
                        ];
                        foreach (Shell topShell in bracketWinners)
                        {
                            fuelStorageCostList.Add(topShell.FuelStorageCost.ToString());
                        }
                        writer.WriteLine(string.Join(ColumnDelimiter, fuelStorageCostList));
                    }

                    List<string> recoilCostList =
                    [
                        "Recoil cost"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        recoilCostList.Add(topShell.RecoilCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, recoilCostList));

                    List<string> ammoUsedList =
                    [
                        "Ammo used"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        ammoUsedList.Add(topShell.AmmoUsed.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoUsedList));

                    List<string> ammoAccessCostList =
                    [
                        "Ammo access cost"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        ammoAccessCostList.Add(topShell.AmmoAccessCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoAccessCostList));

                    List<string> ammoStorageCostList =
                    [
                        "Ammo storage cost"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        ammoStorageCostList.Add(topShell.AmmoStorageCost.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, ammoStorageCostList));

                    List<string> totalCostList =
                    [
                        "Total cost"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        totalCostList.Add(topShell.CostPerLoader.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, totalCostList));

                    List<string> costPerVolumeList =
                    [
                        "Cost per volume"
                    ];
                    foreach (Shell topShell in bracketWinners)
                    {
                        costPerVolumeList.Add(topShell.CostPerVolume.ToString());
                    }
                    writer.WriteLine(string.Join(ColumnDelimiter, costPerVolumeList));
                }
            }
        }
    }
}