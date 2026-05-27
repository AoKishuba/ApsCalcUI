using NUnit.Framework;
using ApsCalcUI;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework.Internal;

namespace ApsCalcUITests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void ShellTest()
        {
            int barrelCount = 1;
            float gauge = 490f;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            bool isBelt = false;
            Module headModule = Module.APHead;
            Module baseModule = Module.BaseBleeder;
            int clipsPerLoader = 2;
            int inputsPerLoader = 1;
            bool usesAmmoEjector = false;
            float gpCasingCount = 1.5f;
            float rgCasingCount = 0.5f;
            float rateOfFireRpm = 100f;
            bool gunUsesRecoilAbsorbers = true;
            bool isDif = false;
            float raildraw = 500f;

            float impactAngle = 0f;
            float nonSabotAngleMultiplier = MathF.Abs(MathF.Cos(impactAngle * MathF.PI / 180));
            float sabotAngleMultiplier = MathF.Abs(MathF.Cos(impactAngle * MathF.PI / 240));
            float[] testModuleCounts = new float[Module.GetBodyModuleCount()];
            for (int i = 0; i < testModuleCounts.Length; i++)
            {
                testModuleCounts[i] = 1;
            }
            float desiredInaccuracy = 0.3f;
            float testIntervalSeconds = 600;
            DamageType[] dts = (DamageType[])Enum.GetValues(typeof(DamageType));
            float fragConeAngle = 60f;
            float fragAngleMultiplier = (2 + MathF.Sqrt(fragConeAngle)) / 16f;
            float storagePerVolume = 500f;
            float storagePerCost = 250f;
            float targetAC = 48f;
            float ppm = 600f;
            float ppv = 60f;
            float ppc = 5f;
            bool engineUsesFuel = true;
            Scheme testScheme = new();

            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeMultiplier,
                isBelt,
                headModule,
                baseModule,
                clipsPerLoader,
                inputsPerLoader,
                clipsPerLoader,
                inputsPerLoader,
                usesAmmoEjector,
                gpCasingCount,
                rgCasingCount,
                rateOfFireRpm,
                gunUsesRecoilAbsorbers,
                isDif
                )
            {
                RailDraw = raildraw,
                NonSabotAngleMultiplier = nonSabotAngleMultiplier,
                SabotAngleMultiplier = sabotAngleMultiplier
            };
            testModuleCounts.CopyTo(testShell.BodyModuleCounts, 0);
            testShell.CalculateLengths();
            testShell.CalculateRequiredBarrelLengths(desiredInaccuracy);
            testShell.CalculateRecoil();
            testShell.CalculateVelocityModifier();
            testShell.CalculateVelocity();
            testShell.CalculateEffectiveRange();
            testShell.CalculateMaxDraw();
            testShell.CalculateReloadTime(testIntervalSeconds);

            foreach (DamageType dt in dts)
            {
                testShell.CalculateDamageModifierByType(dt);
                testShell.CalculateDamageByType(dt, fragAngleMultiplier);
            }

            testShell.CalculateCooldownTime();
            testShell.CalculateCoolerVolumeAndCost();
            testShell.CalculateLoaderVolumeAndCost();
            testShell.CalculateVariableVolumesAndCosts(
                testIntervalSeconds,
                storagePerVolume,
                storagePerCost);
            foreach (DamageType dt in dts)
            {
                testShell.CalculateDpsByType(
                    dt,
                    targetAC,
                    testIntervalSeconds,
                    storagePerVolume,
                    storagePerCost,
                    ppm,
                    ppv,
                    ppc,
                    engineUsesFuel,
                    testScheme,
                    impactAngle);
            }
            Assert.AreEqual(testShell.OverallVelocityModifier, 1.78024387f);
            Assert.AreEqual(testShell.Velocity, 322.720734f);
            Assert.AreEqual(testShell.EffectiveRange, 62163.2344f);
            Assert.AreEqual(testShell.TotalRecoil, 4116.08154f);
            Assert.AreEqual(testShell.FeltRecoil, 3916.08154f);
            Assert.AreEqual(testShell.MaxDrawShell, 140615.141f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 1.28414631f);
            Assert.AreEqual(testShell.BarrelLengthForInaccuracy, 26.5151558f);
            Assert.AreEqual(testShell.BarrelLengthForPropellant, 2.22906041f);
            Assert.AreEqual(testShell.TotalLength, 6390.0f);
            Assert.AreEqual(testShell.ProjectileLength, 5410f);
            Assert.AreEqual(testShell.CostPerShell, 55.4023018f);
            Assert.AreEqual(testShell.ShellReloadTime, 230.588745f);
            Assert.AreEqual(testShell.CooldownTime, 36.7981911f);
            Assert.AreEqual(testShell.OverallKineticDamageModifier, 0.98008132f);
            Assert.AreEqual(testShell.RawKD, 12429.71f);
            Assert.AreEqual(testShell.OverallArmorPierceModifier, 1.40350616f);
            Assert.AreEqual(testShell.ArmorPierce, 7.92645979f);
            Assert.AreEqual(testShell.OverallChemModifier, 0.25f);
            Assert.AreEqual(testShell.RawHE, 773.504028f);
            Assert.AreEqual(testShell.HEExplosionRadius, 7.35425711f);
            Assert.AreEqual(testShell.DamageDict[DamageType.MD], 632.766663f);
            Assert.AreEqual(testShell.MDExplosionRadius, 20.772768f);
            Assert.AreEqual(testShell.FragCount, 11);
            Assert.AreEqual(testShell.DamagePerFrag, 921.103271f);
            Assert.AreEqual(testShell.DamageDict[DamageType.EMP], 415.849396f);
            Assert.AreEqual(testShell.DamageDict[DamageType.Incendiary], 1829.7373f);
            Assert.AreEqual(testShell.DamageDict[DamageType.Smoke], 964.288391f);



            float gaugeBelt = 51f;
            float gaugeMultiplierBelt = MathF.Pow(gaugeBelt / 500f, 1.8f);
            bool isBeltBelt = true;
            Shell testShellBelt = new(
                barrelCount,
                gaugeBelt,
                gaugeMultiplierBelt,
                isBeltBelt,
                headModule,
                baseModule,
                clipsPerLoader,
                inputsPerLoader,
                clipsPerLoader,
                inputsPerLoader,
                usesAmmoEjector,
                gpCasingCount,
                rgCasingCount,
                rateOfFireRpm,
                gunUsesRecoilAbsorbers,
                isDif
                )
            {
                RailDraw = raildraw,
                NonSabotAngleMultiplier = nonSabotAngleMultiplier,
                SabotAngleMultiplier = sabotAngleMultiplier
            };
            testModuleCounts.CopyTo(testShellBelt.BodyModuleCounts, 0);
            testShellBelt.CalculateLengths();
            testShellBelt.CalculateRequiredBarrelLengths(desiredInaccuracy);
            testShellBelt.CalculateRecoil();
            testShellBelt.CalculateVelocityModifier();
            testShellBelt.CalculateVelocity();
            testShellBelt.CalculateEffectiveRange();
            testShellBelt.CalculateMaxDraw();
            testShellBelt.CalculateReloadTime(testIntervalSeconds);

            foreach (DamageType dt in dts)
            {
                testShellBelt.CalculateDamageModifierByType(dt);
                testShellBelt.CalculateDamageByType(dt, fragAngleMultiplier);
            }

            testShellBelt.CalculateCooldownTime();
            testShellBelt.CalculateCoolerVolumeAndCost();
            testShellBelt.CalculateLoaderVolumeAndCost();
            testShellBelt.CalculateVariableVolumesAndCosts(
                testIntervalSeconds,
                storagePerVolume,
                storagePerCost);
            foreach (DamageType dt in dts)
            {
                testShellBelt.CalculateDpsByType(
                    dt,
                    targetAC,
                    testIntervalSeconds,
                    storagePerVolume,
                    storagePerCost,
                    ppm,
                    ppv,
                    ppc,
                    engineUsesFuel,
                    testScheme,
                    impactAngle);
            }
            Assert.AreEqual(testShellBelt.OverallVelocityModifier, 1.77499998f);
            Assert.AreEqual(testShellBelt.Velocity, 733.927124f);
            Assert.AreEqual(testShellBelt.EffectiveRange, 22589.1758f);
            Assert.AreEqual(testShellBelt.TotalRecoil, 561.59021f);
            Assert.AreEqual(testShellBelt.MaxDrawShell, 3618.42432f);
            Assert.AreEqual(testShellBelt.OverallInaccuracyModifier, 1.28250003f);
            Assert.AreEqual(testShellBelt.BarrelLengthForInaccuracy, 6.69449425f);
            Assert.AreEqual(testShellBelt.BarrelLengthForPropellant, 0.642210722f);
            Assert.AreEqual(testShellBelt.TotalLength, 969.0f);
            Assert.AreEqual(testShellBelt.ProjectileLength, 867);
            Assert.AreEqual(testShellBelt.CostPerShell, 1.43299854f);
            Assert.AreEqual(testShellBelt.CooldownTime, 1.73492801f);
            Assert.AreEqual(testShellBelt.OverallKineticDamageModifier, 0.987500012f);
            Assert.AreEqual(testShellBelt.RawKD, 948.009338f);
            Assert.AreEqual(testShellBelt.OverallArmorPierceModifier, 1.49531245f);
            Assert.AreEqual(testShellBelt.ArmorPierce, 19.2053814f);
            Assert.AreEqual(testShellBelt.OverallChemModifier, 0.25f);
            Assert.AreEqual(testShellBelt.RawHE, 19.7974224f);
            Assert.AreEqual(testShellBelt.HEExplosionRadius, 2.44896507f);
            Assert.AreEqual(testShellBelt.DamageDict[DamageType.MD], 16.1953239f);
            Assert.AreEqual(testShellBelt.MDExplosionRadius, 6.91732502f);
            Assert.AreEqual(testShellBelt.FragCount, 4);
            Assert.AreEqual(testShellBelt.DamagePerFrag, 43.1433983f);
            Assert.AreEqual(testShellBelt.DamageDict[DamageType.EMP], 7.08287239f);
            Assert.AreEqual(testShellBelt.DamageDict[DamageType.Incendiary], 31.1646385f);
            Assert.AreEqual(testShellBelt.DamageDict[DamageType.Smoke], 0f);
        }

        [Test]
        public void SchemePenetrationVelocityTest()
        {
            int barrelCount = 1;
            float gauge = 490f;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            bool isBelt = false;
            Module headModule = Module.APHead;
            Module baseModule = Module.BaseBleeder;
            int clipsPerLoader = 2;
            int inputsPerLoader = 1;
            bool usesAmmoEjector = false;
            float gpCasingCount = 1.5f;
            float rgCasingCount = 0.5f;
            float rateOfFireRpm = 100f;
            bool gunUsesRecoilAbsorbers = true;
            bool isDif = false;
            float raildraw = 500f;

            float impactAngle = 0f;
            float nonSabotAngleMultiplier = MathF.Abs(MathF.Cos(impactAngle * MathF.PI / 180));
            float sabotAngleMultiplier = MathF.Abs(MathF.Cos(impactAngle * MathF.PI / 240));
            float[] testModuleCounts = new float[Module.GetBodyModuleCount()];
            for (int i = 0; i < testModuleCounts.Length; i++)
            {
                testModuleCounts[i] = 1;
            }

            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeMultiplier,
                isBelt,
                headModule,
                baseModule,
                clipsPerLoader,
                inputsPerLoader,
                clipsPerLoader,
                inputsPerLoader,
                usesAmmoEjector,
                gpCasingCount,
                rgCasingCount,
                rateOfFireRpm,
                gunUsesRecoilAbsorbers,
                isDif
                )
            {
                RailDraw = raildraw,
                NonSabotAngleMultiplier = nonSabotAngleMultiplier,
                SabotAngleMultiplier = sabotAngleMultiplier
            };
            testModuleCounts.CopyTo(testShell.BodyModuleCounts, 0);
            testShell.CalculateLengths();
            testShell.CalculateVelocityModifier();
            testShell.CalculateDamageModifierByType(DamageType.Kinetic);

            Scheme testScheme = new();

            testScheme.LayerList.Add(Layer.Air);
            testScheme.CalculateLayerAC();
            float minVelocityToPenAir = testScheme.CalculateMinVelocityToPenetrate(testShell, impactAngle);

            testScheme.LayerList.Clear();
            testScheme.LayerList.Add(Layer.MetalBeam);
            testScheme.CalculateLayerAC();
            float minVelocityToPenMetal = testScheme.CalculateMinVelocityToPenetrate(testShell, impactAngle);
            float minRecoilToPenMetal = testShell.CalculateMinTotalRecoilForVelocity(minVelocityToPenMetal);
            testShell.TotalRecoil = minRecoilToPenMetal;
            testShell.CalculateDamageByType(DamageType.Kinetic, 0);
            float apAtMetalPen = testShell.ArmorPierce;
            float kdAtMetalPen = testShell.RawKD;
            float minKDToPenMetal = testScheme.GetRequiredKD(testShell.ArmorPierce, impactAngle, false);

            testScheme.LayerList.Clear();
            testScheme.LayerList.Add(Layer.HeavyWedgeShallow);
            testScheme.CalculateLayerAC();
            float minVelocityToPenHeavy = testScheme.CalculateMinVelocityToPenetrate(testShell, impactAngle);
            float minRecoilToPenHeavy = testShell.CalculateMinTotalRecoilForVelocity(minVelocityToPenHeavy);
            testShell.TotalRecoil = minRecoilToPenHeavy;
            testShell.CalculateDamageByType(DamageType.Kinetic, 0);
            float apAtHeavyPen = testShell.ArmorPierce;
            float kdAtHeavyPen = testShell.RawKD;
            float minKDToPenHeavy = testScheme.GetRequiredKD(testShell.ArmorPierce, impactAngle, false);

            Assert.AreEqual(minVelocityToPenAir, 0);
            Assert.IsTrue(apAtMetalPen > 0);
            Assert.IsTrue(apAtHeavyPen > 0);
            Assert.IsTrue(kdAtMetalPen >= minKDToPenMetal * 0.999f); // for floating point errors
            Assert.IsTrue(kdAtHeavyPen >= minKDToPenHeavy * 0.999f);
        }

        [Test]
        public void FindCoarsePeaks_SingleGlobalPeak_DetectsIt()
        {
            // 5x5 grid with a single peak at (2, 2)
            float[,] map = new float[5, 5];
            for (int g = 0; g < 5; g++)
                for (int r = 0; r < 5; r++)
                    map[g, r] = 10 -(g - 2) * (g - 2) - (r - 2) * (r - 2);

            float score(float gp, float rg) => map[(int)gp, (int)rg];
            (int, int) gpRange(int _) => (0, 4);

            HashSet<Neighborhood> peaks = ShellCalc.FindCoarsePeaks(4, 1f, (Func<int, (int, int)>)gpRange, score);

            Assert.That(peaks, Has.Count.EqualTo(1));
            Assert.That(peaks.Single().CenterGP, Is.EqualTo(2f));
            Assert.That(peaks.Single().CenterRG, Is.EqualTo(2f));
        }

        [Test]
        public void FindCoarsePeaks_MultipleLocalPeaks_DetectsAll()
        {
            // Two-peak landscape: peaks at (1,1) and (3,3), saddle in between
            float[,] map = {
        { 0,  1,  0,  0,  0 },
        { 1,  3,  1,  0,  0 },
        { 0,  1,  0,  1,  0 },
        { 0,  0,  1,  4,  1 },
        { 0,  0,  0,  1,  0 },
    };

            float score(float gp, float rg) => map[(int)gp, (int)rg];
            HashSet<Neighborhood> peaks = ShellCalc.FindCoarsePeaks(4, 1f,
                _ => (0, 4),
                score);

            Assert.That(peaks.Select(p => (p.CenterGP, p.CenterRG)),
                        Is.EquivalentTo(new[] { (1f, 1f), (3f, 3f) }));
        }

        [Test]
        public void RefineToFinePeak_FindsKnownMaximum()
        {
            // Synthetic continuous map: peak at (3.27, 4.18)
            static float score(float gp, float rg)
                => -MathF.Pow(gp - 3.27f, 2) - MathF.Pow(rg - 4.18f, 2);

            Neighborhood hood = new(CenterGP: 3, CenterRG: 4, BottomRG: 3, TopRG: 5);

            var (gp, rg, _) = ShellCalc.RefineToFinePeak(hood, 0.01f,
                _ => (2f, 4f),
                score);

            Assert.That(gp, Is.EqualTo(3.27f).Within(0.01f));
            Assert.That(rg, Is.EqualTo(4.18f).Within(0.01f));
        }

        [Test]
        public void FindCoarsePeaks_GlobalMaximumOnBoundary_DetectsCornerPeak()
        {
            // Peak at (0, 0), lower-left corner of a 5x5 search space.
            // Scores decline monotonically with both coordinates.
            float[,] map = {
                { 10, 5,  2,  1,  0 },
                { 5,  3,  1,  0,  0 },
                { 2,  1,  0,  0,  0 },
                { 1,  0,  0,  0,  0 },
                { 0,  0,  0,  0,  0 },
            };

            float score(float gp, float rg) => map[(int)gp, (int)rg];

            HashSet<Neighborhood> peaks = ShellCalc.FindCoarsePeaks(
                maxRGIncrementCount: 4,
                gridSpacing: 1f,
                gpRangeAtRG: _ => (0, 4),
                score: score);

            Assert.That(peaks.Select(p => (p.CenterGP, p.CenterRG)),
                        Is.EquivalentTo(new[] { (0f, 0f) }));
        }

        [Test]
        public void RefineToFinePeak_GlobalMaxOutsideRegion_FindsNearestValidGridPoint()
        {
            // Continuous score function with a unimodal peak at (3.5, 4.7).
            // Search region (gp in [3.0, 3.6], rg in [4.0, 4.5]) excludes peak.
            // Nearest valid point is (3.5, 4.5).
            static float score(float gp, float rg) =>
                100f - MathF.Pow(gp - 3.5f, 2) - MathF.Pow(rg - 4.7f, 2);

            static (float, float) gpBoundsAtRG(float _) => (3.0f, 3.6f);

            Neighborhood hood = new(
                CenterGP: 3.5f,
                CenterRG: 4.5f,
                BottomRG: 4.0f,
                TopRG: 4.5f);

            var (gp, rg, _) = ShellCalc.RefineToFinePeak(hood, 0.01f, (Func<float, (float, float)>)gpBoundsAtRG, score);

            Assert.That(gp, Is.EqualTo(3.5f).Within(0.005f));
            Assert.That(rg, Is.EqualTo(4.5f).Within(0.005f));
        }

        [Test]
        public void FullSearch_GlobalMaxOutsideFeasibleRegion_FindsNearestInBoundsPeak()
        {
            // Feasibility region:
            //   x ∈ [0, 5]         — separate x limit
            //   y ∈ [0, 8]         — separate y limit
            //   x + y ∈ [2, 10]    — min and max combined limits (min/max recoil analogue)
            //
            // Score landscape (four narrow bumps):
            //   (1.00, 1.00) → +5   inside,  far    from global max
            //   (2.00, 5.00) → +6   inside,  middle distance from global max
            //   (4.27, 4.81) → +8   inside,  closest to global max — should win
            //   (8.00, 8.00) → +10  outside (x > 5) — global max, unreachable

            float gridSize = 1f;
            float spacing = 0.01f;
            int maxRGIncrementCount = 8;

            (int, int) gpIncrementRange(int rgInc)
            {
                float y = rgInc * gridSize;
                float minX = MathF.Max(0f, 2f - y);          // x + y ≥ 2
                float maxX = MathF.Min(5f, 10f - y);         // x ≤ 5 and x + y ≤ 10
                return ((int)MathF.Ceiling(minX / gridSize),
                        (int)MathF.Floor(maxX / gridSize));
            }

            (float, float) gpBounds(float y)
            {
                float minX = MathF.Max(0f, 2f - y);
                float maxX = MathF.Min(5f, 10f - y);
                return (minX, maxX);
            }

            // f(x, y) = value − 4·((x − cx)² + (y − cy)²)
            // The factor of 4 makes each bump steep enough that adjacent bumps don't
            // interfere — every local maximum of max(f_A, f_B, f_C, f_D) is at a bump
            // centre, and no ridges between bumps register as spurious peaks.
            static float Bump(float x, float y, float cx, float cy, float value)
                => value - 4f * ((x - cx) * (x - cx) + (y - cy) * (y - cy));

            float Score(float x, float y)
            {
                float fA = Bump(x, y, 1.00f, 1.00f, 5f);
                float fB = Bump(x, y, 2.00f, 5.00f, 6f);
                float fC = Bump(x, y, 4.27f, 4.81f, 8f);
                float fD = Bump(x, y, 8.00f, 8.00f, 10f);
                return 100f + MathF.Max(MathF.Max(fA, fB), MathF.Max(fC, fD));
            }

            // Coarse pass: expect at least the three in-bounds bump centres to register
            HashSet<Neighborhood> peaks = ShellCalc.FindCoarsePeaks(
                maxRGIncrementCount, gridSize, gpIncrementRange, Score);

            Assert.That(peaks.Count, Is.GreaterThanOrEqualTo(2),
                "landscape has three in-bounds local maxima; coarse search should detect multiple peaks");

            // Fine pass on each coarse peak; track the global winner (mirrors BigTest's loop)
            float bestScore = float.NegativeInfinity;
            (float x, float y) bestLocation = (0f, 0f);
            foreach (Neighborhood hood in peaks)
            {
                var (x, y, s) = ShellCalc.RefineToFinePeak(hood, spacing, gpBounds, Score);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestLocation = (x, y);
                }
            }

            // Global max at (8, 8) is unreachable; nearest in-bounds peak is C at (4.27, 4.81)
            Assert.That(bestLocation.x, Is.EqualTo(4.27f).Within(0.005f));
            Assert.That(bestLocation.y, Is.EqualTo(4.81f).Within(0.005f));
            Assert.That(bestScore, Is.EqualTo(108f).Within(0.01f));
        }

        [Test]
        public void FullSearch_PeakOnDiagonalBoundary_NotRejectedByOutOfRowNeighbor()
        {
            // Feasibility (same shape as the BigTest casing-search constraints):
            //   x ∈ [0, 5]         — separate x limit
            //   y ∈ [0, 8]         — separate y limit
            //   x + y ∈ [2, 10]    — min and max combined limits (recoil analogue)
            //
            // Score landscape — four bump centres:
            //   (1.00, 1.00) → +5    inside, far from global max
            //   (2.00, 5.00) → +6    inside, mid-distance
            //   (5.00, 5.00) → +8    inside, sits on BOTH the x = 5 wall AND the x + y = 10 diagonal
            //   (8.00, 8.00) → +10   outside (x > 5) — global max, unreachable
            //
            // The peak at (5, 5) is the one the fine search should ultimately return:
            //   highest in-bounds value, nearest to the global max at (8, 8).

            float gridSize = 1f;
            float spacing = 0.01f;
            int maxRGIncrementCount = 8;

            (int, int) gpIncrementRange(int rgInc)
            {
                float y = rgInc * gridSize;
                float minX = MathF.Max(0f, 2f - y);
                float maxX = MathF.Min(5f, 10f - y);
                return ((int)MathF.Ceiling(minX / gridSize),
                        (int)MathF.Floor(maxX / gridSize));
            }

            (float, float) gpBounds(float y)
            {
                float minX = MathF.Max(0f, 2f - y);
                float maxX = MathF.Min(5f, 10f - y);
                return (minX, maxX);
            }

            static float Bump(float x, float y, float cx, float cy, float value)
                => value - 4f * ((x - cx) * (x - cx) + (y - cy) * (y - cy));

            static float Score(float x, float y)
            {
                float fA = Bump(x, y, 1.00f, 1.00f, 5f);
                float fB = Bump(x, y, 2.00f, 5.00f, 6f);
                float fC = Bump(x, y, 5.00f, 5.00f, 8f);
                float fD = Bump(x, y, 8.00f, 8.00f, 10f);
                float baseScore = 100f + MathF.Max(MathF.Max(fA, fB), MathF.Max(fC, fD));

                // Lure that simulates ShellTest2 returning a misleadingly high score
                // for an over-bracket configuration: any (x, y) where x + y exceeds
                // the upper-diagonal constraint gets a +50 bonus.
                if (x + y > 10f) baseScore += 50f;

                return baseScore;
            }

            HashSet<Neighborhood> peaks = ShellCalc.FindCoarsePeaks(
                maxRGIncrementCount, gridSize, gpIncrementRange, Score);

            Assert.That(peaks.Count, Is.GreaterThanOrEqualTo(2),
                "landscape has three in-bounds local maxima; coarse search should detect multiple peaks");

            // Coarse search should specifically detect (5, 5), which is the peak the
            // out-of-row top neighbour would lure us away from under the old code.
            Assert.That(peaks.Select(p => (p.CenterGP, p.CenterRG)),
                Has.Member((5f, 5f)),
                "peak at the x = 5, x + y = 10 corner should not be rejected by an out-of-row neighbour");

            float bestScore = float.NegativeInfinity;
            (float x, float y) bestLocation = (0f, 0f);
            foreach (Neighborhood hood in peaks)
            {
                var (x, y, s) = ShellCalc.RefineToFinePeak(hood, spacing, gpBounds, Score);
                if (s > bestScore)
                {
                    bestScore = s;
                    bestLocation = (x, y);
                }
            }

            Assert.That(bestLocation.x, Is.EqualTo(5.00f).Within(0.005f));
            Assert.That(bestLocation.y, Is.EqualTo(5.00f).Within(0.005f));
            Assert.That(bestScore, Is.EqualTo(108f).Within(0.01f));
        }

        [Test]
        public void ReloadTimeTest()
        {
            float[] testModuleCounts = [1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
            float gauge = 60;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            int clipsPerLoader = 2;
            int inputsPerLoader = 3;
            float gpCasingCount = 1.5f;
            float rgCasingCount = 1;
            bool isBelt = false;
            bool isDif = false;
            float testIntervalSeconds = 600;
            Shell testShell = new(
                1,
                gauge,
                gaugeMultiplier,
                isBelt,
                Module.APHead,
                Module.BaseBleeder,
                clipsPerLoader,
                inputsPerLoader,
                clipsPerLoader,
                inputsPerLoader,
                default,
                gpCasingCount,
                rgCasingCount,
                default,
                default,
                isDif);

            testModuleCounts.CopyTo(testShell.BodyModuleCounts, 0);
            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateReloadTime(testIntervalSeconds);

            Assert.AreEqual(testShell.ShellReloadTime, 11.6231985f);
            Assert.AreEqual(testShell.ClusterReloadTime, 3.87439942f);
            Assert.AreEqual(testShell.Uptime, 1f);

            isBelt = true;
            Shell testShellBelt = new(
                1,
                gauge,
                gaugeMultiplier,
                isBelt,
                Module.APHead,
                Module.BaseBleeder,
                clipsPerLoader,
                inputsPerLoader,
                clipsPerLoader,
                inputsPerLoader,
                default,
                gpCasingCount,
                rgCasingCount,
                default,
                default,
                isDif);

            testModuleCounts.CopyTo(testShellBelt.BodyModuleCounts, 0);
            testShellBelt.GaugeMultiplier = MathF.Pow(testShellBelt.Gauge / 500f, 1.8f);
            testShellBelt.CalculateLengths();
            testShellBelt.CalculateReloadTime(testIntervalSeconds);

            Assert.AreEqual(testShellBelt.ShellReloadTime, 2.45784783f);
            Assert.AreEqual(testShellBelt.ClusterReloadTime, 2.45784783f);
            Assert.AreEqual(testShellBelt.Uptime, 0.755905509f);
        }

        [Test]
        public void KineticDamageTest()
        {
            float fragConeAngle = 60f;
            float fragAngleMultiplier = (2 + MathF.Sqrt(fragConeAngle)) / 16f;
            float directHitAngleFromPerpendicularDegrees = 0f;
            float nonDirectHitAngleFromPerpendicularDegrees = 15f;

            float[] testModuleCounts = [1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
            Scheme testScheme = new();
            float gauge = 490;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            float gpCasingCount = 1.5f;
            float rgCasingCount = 1;
            bool isBelt = false;
            bool isDif = false;
            Shell testShellAP = new(
                1,
                gauge,
                gaugeMultiplier,
                isBelt,
                Module.APHead,
                Module.BaseBleeder,
                default,
                default,
                default,
                default,
                default,
                gpCasingCount,
                rgCasingCount,
                default,
                default,
                isDif)
            {
                RailDraw = 500
            };
            testModuleCounts.CopyTo(testShellAP.BodyModuleCounts, 0);
            testShellAP.GaugeMultiplier = MathF.Pow(testShellAP.Gauge / 500f, 1.8f);
            testShellAP.CalculateLengths();
            testShellAP.CalculateRecoil();
            testShellAP.CalculateMaxDraw();
            testShellAP.CalculateReloadTime(600f);

            // AP head
            testShellAP.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellAP.SabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellAP.CalculateVelocityModifier();
            testShellAP.CalculateVelocity();
            testShellAP.CalculateDamageModifierByType(DamageType.Kinetic);
            testShellAP.CalculateDamageByType(DamageType.Kinetic, fragAngleMultiplier);
            testShellAP.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                directHitAngleFromPerpendicularDegrees);
            float nonSabotDirectHit = testShellAP.DamageDict[DamageType.Kinetic];

            testShellAP.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellAP.SabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellAP.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                nonDirectHitAngleFromPerpendicularDegrees);
            float nonSabotNonDirectHit = testShellAP.DamageDict[DamageType.Kinetic];
            Assert.AreEqual(nonSabotDirectHit, testShellAP.RawKD);
            Assert.AreEqual(nonSabotDirectHit, 10413.1367f);
            Assert.AreEqual(nonSabotNonDirectHit, 10058.3174f);


            // Sabot head uses 3/4 angle
            Shell testShellSabot = new(
                1,
                gauge,
                gaugeMultiplier,
                isBelt,
                Module.SabotHead,
                Module.BaseBleeder,
                default,
                default,
                default,
                default,
                default,
                gpCasingCount,
                rgCasingCount,
                default,
                default,
                isDif)
            {
                RailDraw = 500
            };
            testModuleCounts.CopyTo(testShellSabot.BodyModuleCounts, 0);
            testShellSabot.GaugeMultiplier = MathF.Pow(testShellSabot.Gauge / 500f, 1.8f);
            testShellSabot.CalculateLengths();
            testShellSabot.CalculateRecoil();
            testShellSabot.CalculateMaxDraw();
            testShellSabot.CalculateReloadTime(600f);

            testShellSabot.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellSabot.SabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellSabot.CalculateVelocityModifier();
            testShellSabot.CalculateVelocity();
            testShellSabot.CalculateDamageModifierByType(DamageType.Kinetic);
            testShellSabot.CalculateDamageByType(DamageType.Kinetic, fragAngleMultiplier);
            testShellSabot.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                directHitAngleFromPerpendicularDegrees);
            float sabotDirectHit = testShellSabot.DamageDict[DamageType.Kinetic];

            testShellSabot.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellSabot.SabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellSabot.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                nonDirectHitAngleFromPerpendicularDegrees);
            float sabotNonDirectHit = testShellSabot.DamageDict[DamageType.Kinetic];
            Assert.AreEqual(sabotDirectHit, testShellSabot.RawKD);
            Assert.AreEqual(sabotDirectHit, 8851.16602f);
            Assert.AreEqual(sabotNonDirectHit, 8681.09277f);


            // Hollow point head ignores angle
            Shell testShellHollowPoint = new(
                1,
                gauge,
                gaugeMultiplier,
                isBelt,
                Module.HollowPoint,
                Module.BaseBleeder,
                default,
                default,
                default,
                default,
                default,
                gpCasingCount,
                rgCasingCount,
                default,
                default,
                isDif)
            {
                RailDraw = 500
            };
            testModuleCounts.CopyTo(testShellHollowPoint.BodyModuleCounts, 0);
            testShellHollowPoint.GaugeMultiplier = MathF.Pow(testShellHollowPoint.Gauge / 500f, 1.8f);
            testShellHollowPoint.CalculateLengths();
            testShellHollowPoint.CalculateRecoil();
            testShellHollowPoint.CalculateMaxDraw();
            testShellHollowPoint.CalculateReloadTime(600f);

            testShellHollowPoint.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellHollowPoint.SabotAngleMultiplier = MathF.Abs(MathF.Cos(directHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellHollowPoint.CalculateVelocityModifier();
            testShellHollowPoint.CalculateVelocity();
            testShellHollowPoint.CalculateDamageModifierByType(DamageType.Kinetic);
            testShellHollowPoint.CalculateDamageByType(DamageType.Kinetic, fragAngleMultiplier);
            testShellHollowPoint.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                directHitAngleFromPerpendicularDegrees);
            float hollowPointDirectHit = testShellHollowPoint.DamageDict[DamageType.Kinetic];

            testShellHollowPoint.NonSabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 180));
            testShellHollowPoint.SabotAngleMultiplier = MathF.Abs(MathF.Cos(nonDirectHitAngleFromPerpendicularDegrees * MathF.PI / 240));
            testShellHollowPoint.CalculateDpsByType(
                DamageType.Kinetic,
                1f,
                1800,
                500,
                250,
                600,
                60,
                5,
                false,
                testScheme,
                nonDirectHitAngleFromPerpendicularDegrees);
            float hollowPointNonDirectHit = testShellHollowPoint.DamageDict[DamageType.Kinetic];
            Assert.AreEqual(hollowPointDirectHit, testShellHollowPoint.RawKD);
            Assert.AreEqual(hollowPointDirectHit, 10381.6289f);
            Assert.AreEqual(hollowPointNonDirectHit, hollowPointDirectHit);
        }


        [Test]
        public void InaccuracyTest1()
        {
            float[] testModuleCounts = [0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0];
            int barrelCount = 1;
            float gauge = 100;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            bool isDif = false;
            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeMultiplier,
                default,
                Module.APHead,
                Module.BaseBleeder,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                isDif
                );
            testModuleCounts.CopyTo(testShell.BodyModuleCounts, 0);

            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);

            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(5.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.810000062f);

            testShell.Gauge = 300;
            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(16.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.810000062f);

            testShell.Gauge = 400;
            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(22, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.944999993f);

            testShell.Gauge = 500;
            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(27.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 1.02600002f);
        }

        [Test]
        public void InaccuracyTest2()
        {
            float[] testModuleCounts = [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            int barrelCount = 1;
            float gauge = 392;
            float gaugeMultiplier = MathF.Pow(gauge / 500f, 1.8f);
            bool isDif = false;
            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeMultiplier,
                default,
                Module.APHead,
                Module.BaseBleeder,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                isDif
                );
            testModuleCounts.CopyTo(testShell.BodyModuleCounts, 0);

            testShell.GaugeMultiplier = MathF.Pow(testShell.Gauge / 500f, 1.8f);

            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(21.56f, 0.2f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 1.35f);
            Assert.AreEqual(testShell.CalculateMaxProjectileLengthForInaccuracy(21.56f, 0.2f), 899.569763f);
        }

        [Test]
        public void GenerateGridTest()
        {
            /*
            float minGP = 0.5f;
            float maxGP = 8f;
            float minRG = 1.5f;
            float maxRG = 7f;
            float minCasings = 2f;
            float maxCasings = 8f;
            float spacing = 1f;

            // Bottom left
            float testGP = minGP + spacing * 0.3f;
            float testRG = minRG + spacing * 0.3f;

            var testNeighbors = ShellCalc.GenerateNeighbors(testGP, testRG, minGP, maxGP, minRG, maxRG, minCasings, maxCasings, spacing);
            Console.WriteLine("testGP: " + testGP);
            Console.WriteLine("testRG: " + testRG);
            Console.WriteLine("spacing: " + spacing);
            Console.WriteLine("neighbors: " + testNeighbors.Count);
            foreach (var (gp, rg) in testNeighbors)
            {
                Console.WriteLine();
                float totalCasings = gp + rg;
                Console.WriteLine("gp: " + gp);
                Console.WriteLine("rg: " + rg);
                Console.WriteLine("total: " + totalCasings);
                Console.WriteLine("minTest: " + (gp + rg >= minCasings));
                Console.WriteLine("maxTest: " + (gp + rg <= maxCasings));
                Assert.IsTrue(gp + rg >= minCasings);
                Assert.IsTrue(gp + rg <= maxCasings);
            }

            // Top left
            testRG = maxRG - spacing * 0.3f;

            testNeighbors = ShellCalc.GenerateNeighbors(testGP, testRG, minGP, maxGP, minRG, maxRG, minCasings, maxCasings, spacing);
            Console.WriteLine("--------");
            Console.WriteLine("testGP: " + testGP);
            Console.WriteLine("testRG: " + testRG);
            Console.WriteLine("spacing: " + spacing);
            Console.WriteLine("neighbors: " + testNeighbors.Count);
            foreach (var (gp, rg) in testNeighbors)
            {
                Console.WriteLine();
                float totalCasings = gp + rg;
                Console.WriteLine("gp: " + gp);
                Console.WriteLine("rg: " + rg);
                Console.WriteLine("total: " + totalCasings);
                Console.WriteLine("minTest: " + (gp + rg >= minCasings));
                Console.WriteLine("maxTest: " + (gp + rg <= maxCasings));
                Assert.IsTrue(gp + rg >= minCasings);
                Assert.IsTrue(gp + rg <= maxCasings);
            }

            // Top right
            testGP = maxGP - spacing * 0.3f;

            testNeighbors = ShellCalc.GenerateNeighbors(testGP, testRG, minGP, maxGP, minRG, maxRG, minCasings, maxCasings, spacing);
            Console.WriteLine("--------");
            Console.WriteLine("testGP: " + testGP);
            Console.WriteLine("testRG: " + testRG);
            Console.WriteLine("spacing: " + spacing);
            Console.WriteLine("neighbors: " + testNeighbors.Count);
            foreach (var (gp, rg) in testNeighbors)
            {
                Console.WriteLine();
                float totalCasings = gp + rg;
                Console.WriteLine("gp: " + gp);
                Console.WriteLine("rg: " + rg);
                Console.WriteLine("total: " + totalCasings);
                Console.WriteLine("minTest: " + (gp + rg >= minCasings));
                Console.WriteLine("maxTest: " + (gp + rg <= maxCasings));
                Assert.IsTrue(gp + rg >= minCasings);
                Assert.IsTrue(gp + rg <= maxCasings);
            }

            // Bottom right
            testGP = minGP + spacing * 0.3f;

            testNeighbors = ShellCalc.GenerateNeighbors(testGP, testRG, minGP, maxGP, minRG, maxRG, minCasings, maxCasings, spacing);
            Console.WriteLine("--------");
            Console.WriteLine("testGP: " + testGP);
            Console.WriteLine("testRG: " + testRG);
            Console.WriteLine("spacing: " + spacing);
            Console.WriteLine("neighbors: " + testNeighbors.Count);
            foreach (var (gp, rg) in testNeighbors)
            {
                Console.WriteLine();
                float totalCasings = gp + rg;
                Console.WriteLine("gp: " + gp);
                Console.WriteLine("rg: " + rg);
                Console.WriteLine("total: " + totalCasings);
                Console.WriteLine("minTest: " + (gp + rg >= minCasings));
                Console.WriteLine("maxTest: " + (gp + rg <= maxCasings));
                Assert.IsTrue(gp + rg >= minCasings);
                Assert.IsTrue(gp + rg <= maxCasings);
            }
            */

            /*
            List<(float gpCount, float rgCount)> testGrid = ShellCalc.GenerateCasingGrid(minGP, maxGP, minRG, maxRG, spacing);
            foreach ((float gpCount, float rgCount) in testGrid)
            {
                Console.WriteLine("gp: " + gpCount + " rg: " + rgCount);
            }

            Assert.Contains((minGP, 0), testGrid);
            Assert.Contains((maxGP, 0), testGrid);
            Assert.Contains((0, minRG), testGrid);
            Assert.Contains((0, maxRG), testGrid);
            */
        }
    }
}