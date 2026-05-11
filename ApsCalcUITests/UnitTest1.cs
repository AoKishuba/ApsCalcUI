using NUnit.Framework;
using ApsCalcUI;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

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
            float minGP = 0.5f;
            float maxGP = 8f;
            float minRG = 1.5f;
            float maxRG = 7f;
            float spacing = 1f;

            List<(float gpCount, float rgCount)> testGrid = ShellCalc.GenerateCasingGrid(minGP, maxGP, minRG, maxRG, spacing);
            foreach ((float gpCount, float rgCount) casingCounts in testGrid)
            {
                Console.WriteLine("gp: " + casingCounts.gpCount + ". rg: " + casingCounts.rgCount);
            }
        }
    }
}