using NUnit.Framework;
using ApsCalcUI;
using System;

namespace ApsCalcUITests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void ReloadTimeTest()
        {
            float[] testModuleCounts = { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
            float gauge = 60;
            float gaugeCoefficient = MathF.Pow(gauge / 500f, 1.8f);
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
                gaugeCoefficient,
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
            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateReloadTime(testIntervalSeconds);

            Assert.AreEqual(testShell.ShellReloadTime, 11.6231985f);
            Assert.AreEqual(testShell.ClusterReloadTime, 3.87439942f);
            Assert.AreEqual(testShell.Uptime, 1f);

            isBelt = true;
            Shell testShellBelt = new(
                1,
                gauge,
                gaugeCoefficient,
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
            testShellBelt.GaugeCoefficient = MathF.Pow(testShellBelt.Gauge / 500f, 1.8f);
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

            float[] testModuleCounts = { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
            PenCalc.Scheme testScheme = new();
            float gauge = 490;
            float gaugeCoefficient = MathF.Pow(gauge / 500f, 1.8f);
            float gpCasingCount = 1.5f;
            float rgCasingCount = 1;
            bool isBelt = false;
            bool isDif = false;
            Shell testShellAP = new(
                1,
                gauge,
                gaugeCoefficient,
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
            testShellAP.GaugeCoefficient = MathF.Pow(testShellAP.Gauge / 500f, 1.8f);
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
                gaugeCoefficient,
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
            testShellSabot.GaugeCoefficient = MathF.Pow(testShellSabot.Gauge / 500f, 1.8f);
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
                gaugeCoefficient,
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
            testShellHollowPoint.GaugeCoefficient = MathF.Pow(testShellHollowPoint.Gauge / 500f, 1.8f);
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
            Assert.AreEqual(hollowPointDirectHit, 9489.92285f);
            Assert.AreEqual(hollowPointNonDirectHit, hollowPointDirectHit);
        }

        [Test]
        public void ChemicalDamageTest()
        {
            float fragConeAngle = 60f;
            float fragAngleMultiplier = (2 + MathF.Sqrt(fragConeAngle)) / 16f;

            float[] testModuleCounts = { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0 };
            float gauge = 490;
            float gaugeCoefficient = MathF.Pow(gauge / 500f, 1.8f);
            float gpCasingCount = 1.5f;
            float rgCasingCount = 1;
            bool isBelt = false;
            bool isDif = false;
            Shell testShellAP = new(
                1,
                gauge,
                gaugeCoefficient,
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
            testShellAP.GaugeCoefficient = MathF.Pow(testShellAP.Gauge / 500f, 1.8f);
            testModuleCounts.CopyTo(testShellAP.BodyModuleCounts, 0);

            testShellAP.CalculateLengths();
            testShellAP.CalculateVelocityModifier();
            testShellAP.CalculateRecoil();
            testShellAP.CalculateVelocity();
            testShellAP.CalculateMaxDraw();
            testShellAP.CalculateReloadTime(600f);
            testShellAP.CalculateDamageModifierByType(DamageType.HE);
            testShellAP.CalculateDamageByType(DamageType.Kinetic, fragAngleMultiplier);
            testShellAP.CalculateDamageByType(DamageType.HE, fragAngleMultiplier);
            testShellAP.CalculateDamageByType(DamageType.Flak, fragAngleMultiplier);
            testShellAP.CalculateDamageByType(DamageType.Frag, fragAngleMultiplier);
            testShellAP.CalculateDamageByType(DamageType.EMP, fragAngleMultiplier);
            testShellAP.CalculateDamageByType(DamageType.Smoke, fragAngleMultiplier);

            Assert.AreEqual(testShellAP.TotalLength, 5545);
            Assert.AreEqual(testShellAP.ProjectileLength, 4320);
            Assert.AreEqual(testShellAP.LengthDifferential, 0);
            Assert.AreEqual(testShellAP.BodyLength, 3830);
            Assert.AreEqual(testShellAP.CasingLength, 1225);
            Assert.AreEqual(testShellAP.OverallVelocityModifier, 1.78467369f);
            Assert.AreEqual(testShellAP.TotalRecoil, 4116.08154f);
            Assert.AreEqual(testShellAP.Velocity, 362.045319f);
            Assert.AreEqual(testShellAP.MaxDraw, 112295.32f);
            Assert.AreEqual(testShellAP.ClusterReloadTime, 194.836182f);
            Assert.AreEqual(testShellAP.OverallChemModifier, 0.25f);
            Assert.AreEqual(testShellAP.RawHE, 773.504028f);
            Assert.AreEqual(testShellAP.DamageDict[DamageType.Flak], 703.5271f);
            Assert.AreEqual(testShellAP.DamageDict[DamageType.Frag], 10132.1357f);
            Assert.AreEqual(testShellAP.DamageDict[DamageType.EMP], 415.849396f);
            Assert.AreEqual(testShellAP.DamageDict[DamageType.Smoke], 964.288391f);

            // Note: when testing HEAT damage, ensure HE warhead is directly behind head
            Shell testShellHeat = new(
                1,
                gauge,
                gaugeCoefficient,
                isBelt,
                Module.ShapedChargeHead,
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
            testShellHeat.GaugeCoefficient = MathF.Pow(testShellHeat.Gauge / 500f, 1.8f);
            testModuleCounts.CopyTo(testShellHeat.BodyModuleCounts, 0);

            testShellHeat.CalculateLengths();
            testShellHeat.CalculateVelocityModifier();
            testShellHeat.CalculateRecoil();
            testShellHeat.CalculateVelocity();
            testShellHeat.CalculateMaxDraw();
            testShellHeat.CalculateReloadTime(600f);
            testShellHeat.CalculateDamageModifierByType(DamageType.HE);
            testShellHeat.CalculateDamageByType(DamageType.Kinetic, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.HE, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.Flak, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.Frag, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.EMP, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.Smoke, fragAngleMultiplier);
            testShellHeat.CalculateDamageByType(DamageType.HEAT, fragAngleMultiplier);
            Assert.AreEqual(testShellHeat.DamageDict[DamageType.HEAT], 9557.65039f);
        }

        [Test]
        public void InaccuracyTest1()
        {
            float[] testModuleCounts = { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 };
            int barrelCount = 1;
            float gauge = 100;
            float gaugeCoefficient = MathF.Pow(gauge / 500f, 1.8f);
            bool isDif = false;
            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeCoefficient,
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

            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);

            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(5.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.810000062f);

            testShell.Gauge = 300;
            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(16.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.810000062f);

            testShell.Gauge = 400;
            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(22, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 0.944999993f);

            testShell.Gauge = 500;
            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);
            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(27.5f, 0.3f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 1.02600002f);
        }

        [Test]
        public void InaccuracyTest2()
        {
            float[] testModuleCounts = { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int barrelCount = 1;
            float gauge = 392;
            float gaugeCoefficient = MathF.Pow(gauge / 500f, 1.8f);
            bool isDif = false;
            Shell testShell = new(
                barrelCount,
                gauge,
                gaugeCoefficient,
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

            testShell.GaugeCoefficient = MathF.Pow(testShell.Gauge / 500f, 1.8f);

            testShell.CalculateLengths();
            testShell.CalculateMaxProjectileLengthForInaccuracy(21.56f, 0.2f);
            Assert.AreEqual(testShell.OverallInaccuracyModifier, 1.35f);
            Assert.AreEqual(testShell.CalculateMaxProjectileLengthForInaccuracy(21.56f, 0.2f), 899.569763f);
        }
    }
}