using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using PenCalc;

namespace ApsCalcUI
{
    public class Shell
    {
        public Shell(
            int barrelCount,
            float gauge,
            float gaugeCoefficient,
            bool isBelt,
            Module headModule,
            Module baseModule,
            int regularClipsPerLoader,
            int regularInputsPerLoader,
            int beltfedClipsPerLoader,
            int beltfedInputsPerLoader,
            bool usesAmmoEjector,
            float gpCasingCount,
            float rgCasingCount,
            float rateOfFireRpm,
            bool gunUsesRecoilAbsorbers,
            bool isDif
            )
        {
            BarrelCount = barrelCount;
            Gauge = gauge;
            GaugeCoefficient = gaugeCoefficient;
            IsBelt = isBelt;
            HeadModule = headModule;
            BaseModule = baseModule;
            RegularClipsPerLoader = regularClipsPerLoader;
            RegularInputsPerLoader = regularInputsPerLoader;
            BeltfedClipsPerLoader = beltfedClipsPerLoader;
            BeltfedInputsPerLoader = beltfedInputsPerLoader;
            UsesAmmoEjector = usesAmmoEjector;
            GPCasingCount = gpCasingCount;
            RGCasingCount = rgCasingCount;
            RateOfFireRpm = rateOfFireRpm;
            GunUsesRecoilAbsorbers = gunUsesRecoilAbsorbers;
            IsDif = isDif;
        }
        private const float ApsModifier = 23; // Used as global multiplier in damage calculations
        public float Gauge { get; set; }
        public float GaugeCoefficient { get; set; }

        public bool IsBelt;

        // Keep counts of body modules.
        public float[] BodyModuleCounts { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public float ModuleCountTotal { get; set; }


        public Module BaseModule { get; }
        public Module HeadModule { get; } // Shell must always contain a head, even if shell is only 1 module

        // Clip and input counts
        public int RegularClipsPerLoader { get; set; }
        public int RegularInputsPerLoader { get; set; }
        public int BeltfedClipsPerLoader { get; set; }
        public int BeltfedInputsPerLoader { get; set; }
        public bool UsesAmmoEjector { get; set; }

        // Gunpowder and Railgun casing counts
        public float GPCasingCount { get; set; }
        public float RGCasingCount { get; set; }


        // Lengths
        public float CasingLength { get; set; }
        public float ProjectileLength { get; set; } // Everything but casings
        public float BodyLength { get; set; } // Everything but casings and Head
        public float TotalLength { get; set; }
        public float ShortLength { get; set; } // Used for penalizing short shells
        public float LengthDifferential { get; set; } // Used for penalizing short shells
        public float EffectiveBodyLength { get; set; } // Used for penalizing short shells
        public float EffectiveBodyModuleCount { get; set; } // Compensate for length-limited modules
        public float EffectiveProjectileModuleCount { get; set; } // Compensate for length-limited modules
        public float BarrelLengthForInaccuracy { get; set; }
        public float BarrelLengthForPropellant { get; set; }

        // Overall modifiers
        public float OverallVelocityModifier { get; set; }
        public float OverallKineticDamageModifier { get; set; }
        public float OverallArmorPierceModifier { get; set; }
        public float OverallChemModifier { get; set; }
        public float OverallInaccuracyModifier { get; set; }
        public float RateOfFireRpm { get; set; }


        // Power
        public float GPRecoil { get; set; }
        public float MaxDraw { get; set; }
        public float RailDraw { get; set; }
        public float TotalRecoil { get; set; }
        public bool GunUsesRecoilAbsorbers { get; set; }
        public float Velocity { get; set; }

        // Reload
        public bool IsDif { get; set; }
        public float ShellReloadTime { get; set; }
        public float ClusterReloadTime { get; set; }
        public float Uptime { get; set; }
        public int BarrelCount { get; set; }
        public float CooldownTime { get; set; }

        // Effective range
        public float EffectiveRange { get; set; }


        // Damage
        public float RawKD { get; set; }
        public float ArmorPierce { get; set; }
        public float SabotAngleMultiplier { get; set; }
        public float NonSabotAngleMultiplier { get; set; }
        public float FragCount { get; set; }
        public float DamagePerFrag { get; set; }
        public float RawHE { get; set; }
        public float HEExplosionRadius { get; set; }
        public float FlakExplosionRadius { get; set; }

        public Dictionary<DamageType, float> DamageDict = new()
        {
            { DamageType.Kinetic, 0 },
            { DamageType.EMP, 0 },
            { DamageType.Flak, 0 },
            { DamageType.Frag, 0 },
            { DamageType.HE, 0 },
            { DamageType.HEAT, 0 },
            { DamageType.Disruptor, 0 },
            { DamageType.Smoke, 0 }
        };

        public Dictionary<DamageType, float> DpsDict = new()
        {
            { DamageType.Kinetic, 0 },
            { DamageType.EMP, 0 },
            { DamageType.Flak, 0 },
            { DamageType.Frag, 0 },
            { DamageType.HE, 0 },
            { DamageType.HEAT, 0 },
            { DamageType.Disruptor, 0 },
            { DamageType.Smoke, 0 }
        };

        public Dictionary<DamageType, float> DpsPerVolumeDict = new()
        {
            { DamageType.Kinetic, 0 },
            { DamageType.EMP, 0 },
            { DamageType.Flak, 0 },
            { DamageType.Frag, 0 },
            { DamageType.HE, 0 },
            { DamageType.HEAT, 0 },
            { DamageType.Disruptor, 0 },
            { DamageType.Smoke, 0 }
        };

        public Dictionary<DamageType, float> DpsPerCostDict = new()
        {
            { DamageType.Kinetic, 0 },
            { DamageType.EMP, 0 },
            { DamageType.Flak, 0 },
            { DamageType.Frag, 0 },
            { DamageType.HE, 0 },
            { DamageType.HEAT, 0 },
            { DamageType.Disruptor, 0 },
            { DamageType.Smoke, 0 }
        };


        // Volume
        public float LoaderVolume { get; set; }
        public float RecoilVolume { get; set; }
        public float ChargerVolume { get; set; }
        public float EngineVolume { get; set; }
        public float FuelAccessVolume { get; set; }
        public float FuelStorageVolume { get; set; }
        public float CoolerVolume { get; set; }
        public float AmmoAccessVolume { get; set; }
        public float AmmoStorageVolume { get; set; }
        public float VolumePerLoader { get; set; }


        // Cost
        public float LoaderCost { get; set; }
        public float RecoilCost { get; set; }
        public float ChargerCost { get; set; }
        public float EngineCost { get; set; }
        public float FuelBurned { get; set; }
        public float FuelAccessCost { get; set; }
        public float FuelStorageCost { get; set; }
        public float CoolerCost { get; set; }
        public float CostPerShell { get; set; } // Material cost for one shell
        public float AmmoUsed { get; set; } // Material cost for all shells
        public float AmmoAccessCost { get; set; }
        public float AmmoStorageCost { get; set; }
        public float CostPerLoader { get; set; }

        // Cost per Volume
        public float CostPerVolume { get; set; }


        /// <summary>
        /// Calculates body, projectile, casing, and total lengths, as well as length differential, which is used to penalize short shells
        /// </summary>
        public void CalculateLengths()
        {
            BodyLength = 0;
            if (BaseModule != null)
            {
                BodyLength += MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float ModuleLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                BodyLength += ModuleLength * modCount;
                modIndex++;
            }

            CasingLength = (GPCasingCount + RGCasingCount) * Gauge;

            float HeadLength = MathF.Min(Gauge, HeadModule.MaxLength);
            ProjectileLength = BodyLength + HeadLength;

            TotalLength = CasingLength + ProjectileLength;

            ShortLength = 2 * Gauge;
            LengthDifferential = MathF.Max(ShortLength - BodyLength, 0);
            EffectiveBodyLength = MathF.Max(2 * Gauge, BodyLength);

            EffectiveBodyModuleCount = BodyLength / Gauge;
            EffectiveProjectileModuleCount = ProjectileLength / Gauge;
        }

        /// <summary>
        /// Calculates velocity modifier
        /// </summary>
        public void CalculateVelocityModifier()
        {
            // Calculate weighted velocity modifier of body
            float weightedVelocityMod = 0f;
            if (BaseModule != null)
            {
                weightedVelocityMod += BaseModule.VelocityMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            // Add body module weighted modifiers
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedVelocityMod += modLength * Module.AllModules[modIndex].VelocityMod * modCount;
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedVelocityMod += 0.7f * LengthDifferential;
            }

            weightedVelocityMod /= EffectiveBodyLength;

            OverallVelocityModifier = weightedVelocityMod * HeadModule.VelocityMod;
            if (BaseModule?.Name == "Base bleeder")
            {
                OverallVelocityModifier += 0.15f;
            }
        }


        /// <summary>
        /// Calculate damage modifier according to current DamageType
        /// </summary>
        public void CalculateDamageModifierByType(DamageType dt)
        {
            CalculateKDModifier();
            CalculateAPModifier();
            if (dt != DamageType.Kinetic)
            {
                CalculateChemModifier();
            }
        }


        /// <summary>
        /// Calculates kinetic damage modifier
        /// </summary>
        void CalculateKDModifier()
        {
            // Calculate weighted KineticDamage modifier of body
            float weightedKineticDamageMod = 0f;
            if (BaseModule != null)
            {
                weightedKineticDamageMod += BaseModule.KineticDamageMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedKineticDamageMod += modLength * Module.AllModules[modIndex].KineticDamageMod * modCount;
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedKineticDamageMod += LengthDifferential;
            }

            weightedKineticDamageMod /= EffectiveBodyLength;

            OverallKineticDamageModifier = weightedKineticDamageMod * HeadModule.KineticDamageMod;

            if (BaseModule?.Name == Module.GravRam.Name)
            {
                OverallKineticDamageModifier *= 0.7f;
            }
        }


        /// <summary>
        /// Calculates AP modifier
        /// </summary>
        void CalculateAPModifier()
        {
            // Calculate weighted AP modifier of body
            float weightedArmorPierceMod = 0f;
            if (BaseModule != null)
            {
                weightedArmorPierceMod += BaseModule.ArmorPierceMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedArmorPierceMod += modLength * Module.AllModules[modIndex].ArmorPierceMod * modCount;
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedArmorPierceMod += LengthDifferential;
            }

            weightedArmorPierceMod /= EffectiveBodyLength;

            OverallArmorPierceModifier = weightedArmorPierceMod * HeadModule.ArmorPierceMod;
        }


        /// <summary>
        /// Calculates chemical payload modifier
        /// </summary>
        public void CalculateChemModifier()
        {
            OverallChemModifier = 1f;
            if (BaseModule != null)
            {
                OverallChemModifier = MathF.Min(OverallChemModifier, BaseModule.ChemMod);
            }


            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (modCount > 0)
                {
                    OverallChemModifier = MathF.Min(OverallChemModifier, Module.AllModules[modIndex].ChemMod);
                }
                modIndex++;
            }

            if (HeadModule == Module.Disruptor) // Disruptor 50% penalty stacks
            {
                OverallChemModifier *= 0.5f;
            }
            else
            {
                OverallChemModifier = MathF.Min(OverallChemModifier, HeadModule.ChemMod);
            }
        }


        /// <summary>
        /// Calculates inaccuracy modifier
        /// </summary>
        void CalculateInaccuracyModifier()
        {
            // Calculate weighted inaccuracy modifier of body
            float weightedInaccuracyMod = 0f;
            if (BaseModule != null)
            {
                weightedInaccuracyMod += BaseModule.InaccuracyMod * MathF.Min(Gauge, BaseModule.MaxLength);
            }

            // Add body module weighted modifiers
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                float modLength = MathF.Min(Gauge, Module.AllModules[modIndex].MaxLength);
                weightedInaccuracyMod += modLength * Module.AllModules[modIndex].InaccuracyMod * modCount;
                modIndex++;
            }

            if (LengthDifferential > 0f) // Add 'ghost' module for penalizing short shells; has no effect if body length >= 2 * gauge
            {
                weightedInaccuracyMod += LengthDifferential;
            }

            weightedInaccuracyMod /= EffectiveBodyLength;

            OverallInaccuracyModifier = weightedInaccuracyMod * HeadModule.InaccuracyMod;
            if (BaseModule?.Name == Module.BaseBleeder.Name)
            {
                OverallInaccuracyModifier *= 1.35f;
            }
            else if (BaseModule?.Name == Module.Tracer.Name)
            {
                OverallInaccuracyModifier /= 1f + (0.5f * MathF.Max(0f, 15f - (60f / RateOfFireRpm)) / 15f);
            }

            if (BarrelCount > 1)
            {
                OverallInaccuracyModifier *= (BarrelCount - 1f) * 0.05f + 1.2f;
            }

            if (!GunUsesRecoilAbsorbers)
            {
                OverallInaccuracyModifier *= 1f + 0.6f * TotalRecoil / 12500f / GaugeCoefficient;
            }
        }


        /// <summary>
        /// Calculates max allowed rail draw for given inaccuracy (only affects guns without recoil absorbers)
        /// </summary>
        public float CalculateMaxDrawForInaccuracy(float maxBarrelLengthInM, float desiredInaccuracy)
        {
            float maxDraw = 
                (MathF.Pow(
                    MathF.Pow(ProjectileLength / 1000f, 3f / 4f) / maxBarrelLengthInM * 4f, 1f / 2.5f)
                / 0.3f * desiredInaccuracy / OverallInaccuracyModifier - 1f)
                / 0.6f * 12500f * GaugeCoefficient - GPRecoil;

            return maxDraw;
        }


        /// <summary>
        /// Calculate max body length for 0.3° inaccuracy
        /// </summary>
        public float CalculateMaxProjectileLengthForInaccuracy(float maxBarrelLengthInM, float desiredInaccuracy)
        {
            CalculateInaccuracyModifier();
            float maxProjectileLength = MathF.Pow(maxBarrelLengthInM / 4f / MathF.Pow(0.3f / desiredInaccuracy * OverallInaccuracyModifier, 2.5f), 4f / 3f);

            return maxProjectileLength * 1000f;
        }


        /// <summary>
        /// Calculate min barrel length for inaccuracy and full propellant burn
        /// </summary>
        public void CalculateRequiredBarrelLengths(float desiredInaccuracy)
        {
            CalculateInaccuracyModifier();
            BarrelLengthForInaccuracy = 4 * MathF.Pow(ProjectileLength / 1000f, 0.75f) * MathF.Pow(0.3f / desiredInaccuracy * OverallInaccuracyModifier, 2.5f);

            BarrelLengthForPropellant = 2.2f * GPCasingCount * MathF.Pow(Gauge / 1000f, 0.55f);
        }


        /// <summary>
        /// Calculates max rail draw of shell
        /// </summary>
        public void CalculateMaxDraw()
        {
            MaxDraw = 12500f * GaugeCoefficient * (EffectiveProjectileModuleCount + (0.5f * RGCasingCount));
        }


        /// <summary>
        /// Calculates recoil from gunpowder casings
        /// </summary>
        public void CalculateRecoil()
        {
            GPRecoil = GaugeCoefficient * GPCasingCount * 2500f;
            TotalRecoil = GPRecoil + RailDraw;
        }


        /// <summary>
        /// Calculates shell velocity
        /// </summary>
        public void CalculateVelocity()
        {
            Velocity = MathF.Sqrt(TotalRecoil * 85f * Gauge / (GaugeCoefficient * ProjectileLength)) * OverallVelocityModifier;
        }


        /// <summary>
        /// Calculates minimum rail draw needed to achieve given velocity and effective range
        /// </summary>
        public float CalculateMinimumDrawForVelocityandRange(float minVelocityInput, float minRangeInput)
        {
            CalculateRecoil();
            // Calculate effective time
            float gravityCompensatorCount = 0;
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (Module.AllModules[modIndex] == Module.GravCompensator)
                {
                    gravityCompensatorCount = BodyModuleCounts[modIndex];
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            float effectiveTime = 10f * OverallVelocityModifier * (ProjectileLength / 1000f) * (1f + gravityCompensatorCount);

            // Determine whether range or velocity is limiting factor
            float minVelocity = MathF.Max(minVelocityInput, minRangeInput / effectiveTime);

            // Calculate draw required for either range or velocity
            float minDrawVelocity = MathF.Pow(minVelocity / OverallVelocityModifier, 2)
                * (GaugeCoefficient * ProjectileLength)
                / (Gauge * 85f)
                - GPRecoil;

            float minDraw = MathF.Max(0, minDrawVelocity);

            return minDraw;
        }


        /// <summary>
        /// Calculates reload time and uptime
        /// </summary>
        public void CalculateReloadTime(float testIntervalSeconds)
        {
            ShellReloadTime = MathF.Pow(Gauge / 500f, 1.35f)
                * (2f + EffectiveProjectileModuleCount + 0.25f * (RGCasingCount + GPCasingCount))
                * 17.5f;
            
            if (IsBelt)
            {
                ShellReloadTime *= 0.75f * MathF.Pow(Gauge / 1000f, 0.45f);
                ClusterReloadTime = ShellReloadTime;

                float capacityModifier = Gauge <= 250f ? 2 : 1;
                float shellCapacity = BeltfedClipsPerLoader * MathF.Min(64f, MathF.Floor(1000f / Gauge) * capacityModifier) + 1f;
                float firingCycleLength = (shellCapacity - 1f) * ClusterReloadTime;
                float loadingCycleLength = (shellCapacity - BeltfedInputsPerLoader) * ClusterReloadTime / BeltfedInputsPerLoader;
                float fullCycleLength = firingCycleLength + loadingCycleLength;
                Uptime = MathF.Min(firingCycleLength / MathF.Min(fullCycleLength, testIntervalSeconds), 1f);
            }
            else if (IsDif)
            {
                ClusterReloadTime = ShellReloadTime * 2f;
                Uptime = 1f;
            }
            else
            {
                ClusterReloadTime = ShellReloadTime / (1f + RegularClipsPerLoader);

                float capacityModifier = Gauge <= 250f ? 2 : 1;
                float shellCapacity = RegularClipsPerLoader * MathF.Min(64f, MathF.Floor(1000f / Gauge) * capacityModifier) + 1f;
                float timeToEmptySeconds =
                    shellCapacity
                    * ClusterReloadTime
                    * (1f + RegularClipsPerLoader)
                    / (1f + RegularClipsPerLoader - RegularInputsPerLoader);

                if (timeToEmptySeconds >= testIntervalSeconds)
                {
                    Uptime = 1f;
                }
                else
                {
                    float reloadTimeWhenEmptySeconds = ClusterReloadTime * (1f + RegularClipsPerLoader) / RegularInputsPerLoader;
                    float reducedRofDurationSeconds = MathF.Max(0f, testIntervalSeconds - timeToEmptySeconds);
                    Uptime = 
                        (timeToEmptySeconds + ClusterReloadTime / reloadTimeWhenEmptySeconds * reducedRofDurationSeconds)
                        / testIntervalSeconds;
                }
            }
        }


        /// <summary>
        /// Calculates barrel cooldown time
        /// </summary>
        public void CalculateCooldownTime()
        {
            CooldownTime =
                3.75f
                * GaugeCoefficient
                / MathF.Pow(Gauge * Gauge * Gauge / 125000000f, 0.15f)
                * 17.5f
                * MathF.Pow(GPCasingCount, 0.35f)
                / 2;
            CooldownTime = MathF.Max(CooldownTime, 0);
        }


        /// <summary>
        /// Calculates effective range of shell
        /// </summary>
        public void CalculateEffectiveRange()
        {
            float gravityCompensatorCount = 0;
            int modIndex = 0;
            foreach (float modCount in BodyModuleCounts)
            {
                if (Module.AllModules[modIndex] == Module.GravCompensator)
                {
                    gravityCompensatorCount = BodyModuleCounts[modIndex];
                    break;
                }
                else
                {
                    modIndex++;
                }
            }
            float effectiveTime = 10f * OverallVelocityModifier * (ProjectileLength / 1000f) * (1f + gravityCompensatorCount);
            EffectiveRange = Velocity * effectiveTime;
        }


        /// <summary>
        /// Calculates damage according to current damageType
        /// </summary>
        /// <param name="dt">Damage type to test</param>
        /// <param name="fragAngleMultiplier">(2 + sqrt(angle °)) / 16</param>
        public void CalculateDamageByType(DamageType dt, float fragAngleMultiplier)
        {
            if (dt == DamageType.Kinetic)
            {
                CalculateKineticDamage();
                CalculateAP();
            }
            else if (dt == DamageType.EMP)
            {
                CalculateEmpDamage();
            }
            else if (dt == DamageType.Flak)
            {
                CalculateFlakDamage();
            }
            else if (dt == DamageType.Frag)
            {
                CalculateFragDamage(fragAngleMultiplier);
            }
            else if (dt == DamageType.HE)
            {
                CalculateHEDamage();
            }
            else if (dt == DamageType.HEAT)
            {
                CalculateHeatDamage();
            }
            else if (dt == DamageType.Disruptor)
            {
                CalculateShieldReduction();
            }
            else if (dt == DamageType.Smoke)
            {
                CalculateSmokeStrength();
            }
        }


        /// <summary>
        /// Calculates raw kinetic damage
        /// </summary>
        void CalculateKineticDamage()
        {
            CalculateVelocity();

            if (HeadModule == Module.HollowPoint)
            {
                RawKD =
                    GaugeCoefficient
                    * EffectiveProjectileModuleCount
                    * Velocity
                    * OverallKineticDamageModifier
                    * 0.16f
                    * ApsModifier;
            }
            else
            {
                RawKD =
                    MathF.Pow(500 / MathF.Max(Gauge, 100f), 0.15f)
                    * GaugeCoefficient
                    * EffectiveProjectileModuleCount
                    * Velocity
                    * OverallKineticDamageModifier
                    * 0.16f
                    * ApsModifier;
            }
        }

        /// <summary>
        /// Calculates armor pierce rating
        /// </summary>
        void CalculateAP()
        {
            ArmorPierce = Velocity * OverallArmorPierceModifier * 0.0175f;
        }

        /// <summary>
        /// Calculates EMP damage. Used by shield reduction
        /// </summary>
        void CalculateEmpDamage()
        {
            // Get index of EMP body
            int empIndex = int.MaxValue;
            for (int i = 0; i < Module.AllModules.Length; i++)
            {
                if (Module.AllModules[i] == Module.EmpBody)
                {
                    empIndex = i;
                    break;
                }
            }

            float empBodies = BodyModuleCounts[empIndex];

            if (HeadModule == Module.EmpHead || HeadModule == Module.EmpBody || HeadModule == Module.Disruptor)
            {
                empBodies++;
            }
            DamageDict[DamageType.EMP] = GaugeCoefficient * empBodies * OverallChemModifier * 75f * ApsModifier;
        }

        /// <summary>
        /// Calculates Flak damage
        /// </summary>
        void CalculateFlakDamage()
        {
            // Get index of Flak body
            int flakIndex = int.MaxValue;
            for (int i = 0; i < Module.AllModules.Length; i++)
            {
                if (Module.AllModules[i] == Module.FlakBody)
                {
                    flakIndex = i;
                    break;
                }
            }

            float flaKBodies = BodyModuleCounts[flakIndex];

            if (HeadModule == Module.FlakHead || HeadModule == Module.FlakBody)
            {
                flaKBodies++;
            }
            DamageDict[DamageType.Flak] = 3000f * MathF.Pow(GaugeCoefficient * flaKBodies * 108f * ApsModifier / 3000f * OverallChemModifier, 0.9f);
            FlakExplosionRadius = MathF.Min(MathF.Pow(DamageDict[DamageType.Flak], 0.3f) * 3f, 30f);
            /* 
             * Multiply by volume to approximate applied damage; divide by 1000 to make result more manageable
            float sphereVolume = MathF.Pow(FlakExplosionRadius, 3) * MathF.PI * 4f / 3f;
            DamageDict[DamageType.Flak] = RawFlak * sphereVolume / 1000f;
            */
        }

        /// <summary>
        /// Calculates damage from Frag
        /// </summary>
        /// <param name="fragAngleMultiplier">(2 + sqrt(cone angle °)) / 16</param>
        void CalculateFragDamage(float fragAngleMultiplier)
        {
            // Get index of frag body
            int fragIndex = int.MaxValue;
            for (int i = 0; i < Module.AllModules.Length; i++)
            {
                if (Module.AllModules[i] == Module.FragBody)
                {
                    fragIndex = i;
                    break;
                }
            }

            float fragBodies = BodyModuleCounts[fragIndex];

            if (HeadModule == Module.FragHead || HeadModule == Module.FragBody)
            {
                fragBodies++;
            }
            DamageDict[DamageType.Frag] = GaugeCoefficient * fragBodies * OverallChemModifier * 3000f * ApsModifier;
            // Frag count is based on raw damage before angle multiplier
            FragCount = MathF.Floor(MathF.Pow(DamageDict[DamageType.Frag], 0.25f));
            DamageDict[DamageType.Frag] *= fragAngleMultiplier;
            DamagePerFrag = DamageDict[DamageType.Frag] / FragCount;
        }

        /// <summary>
        /// Calculates damage from HE 
        /// </summary>
        void CalculateHEDamage()
        {
            // Get index of HE body
            int heIndex = int.MaxValue;
            for (int i = 0; i < Module.AllModules.Length; i++)
            {
                if (Module.AllModules[i] == Module.HEBody)
                {
                    heIndex = i;
                    break;
                }
            }

            float heBodies = BodyModuleCounts[heIndex];

            if (HeadModule == Module.ShapedChargeHead)
            {
                heBodies += 0.2f;
            }
            else if (HeadModule == Module.HEHead || HeadModule == Module.HEBody)
            {
                heBodies++;
            }
            RawHE = 3000f * MathF.Pow(GaugeCoefficient * heBodies * 120f * ApsModifier / 3000f * OverallChemModifier, 0.9f);
            HEExplosionRadius = MathF.Min(MathF.Pow(RawHE, 0.3f), 30f);
            // Multiply by volume to approximate applied damage; divide by 1000 to make result more manageable
            float sphereVolume = MathF.Pow(HEExplosionRadius, 3) * MathF.PI * 4f / 3f;
            DamageDict[DamageType.HE] = RawHE * sphereVolume / 1000f;
        }

        /// <summary>
        /// Calculates damage from HEAT, assuming special factor of 1 for all HE bodies and a penetration metric of 0.5
        /// HESH damage scales same as HEAT, so optimal configurations work for both types
        /// </summary>
        void CalculateHeatDamage()
        {
            if (HeadModule == Module.ShapedChargeHead)
            {
                // Get index of HE body
                int heIndex = int.MaxValue;
                for (int i = 0; i < Module.AllModules.Length; i++)
                {
                    if (Module.AllModules[i] == Module.HEBody)
                    {
                        heIndex = i;
                        break;
                    }
                }

                float heBodies = BodyModuleCounts[heIndex];
                // Calculate HE damage assuming special factor of 1 for HE bodies
                // Special heads count as HE body with special factor of 0.8, leaving 0.2 body equivalents for actual HE damage
                RawHE = 3000f * MathF.Pow(GaugeCoefficient * 0.2f * 120f * ApsModifier / 3000f * OverallChemModifier, 0.9f);
                HEExplosionRadius = MathF.Min(MathF.Pow(RawHE, 0.3f), 30f);
                // Multiply by volume to approximate applied damage
                float sphereVolume = MathF.Pow(HEExplosionRadius, 3) * MathF.PI * 4f / 3f;
                DamageDict[DamageType.HE] = RawHE * sphereVolume / 1000f;

                DamageDict[DamageType.HEAT] =
                    GaugeCoefficient
                    * (heBodies + 0.8f)
                    * OverallChemModifier
                    * ApsModifier
                    * 957.6435f; // 3000 * 1.15 * 0.42 / 16 / sqrt(0.5) * (2 + sqrt(30))
            }
            else
            {
                DamageDict[DamageType.HE] = 0;
                DamageDict[DamageType.HEAT] = 0;
            }
        }

        /// <summary>
        /// Calculates planar shield reduction for shells with disruptor head
        /// </summary>
        void CalculateShieldReduction()
        {
            CalculateEmpDamage();
            if (HeadModule == Module.Disruptor)
            {
                DamageDict[DamageType.Disruptor] = DamageDict[DamageType.EMP] * 0.75f / 1500;
                DamageDict[DamageType.Disruptor] = MathF.Min(DamageDict[DamageType.Disruptor], 1f);
            }
            else
            {
                DamageDict[DamageType.Disruptor] = 0;
            }
        }

        /// <summary>
        /// Calculates strength of smoke warheads
        /// </summary>
        void CalculateSmokeStrength()
        {
            if (Gauge >= 200)
            {
                // Get index of smoke body
                int smokeIndex = int.MaxValue;
                for (int i = 0; i < Module.AllModules.Length; i++)
                {
                    if (Module.AllModules[i] == Module.SmokeBody)
                    {
                        smokeIndex = i;
                        break;
                    }
                }
                float smokeBodies = BodyModuleCounts[smokeIndex];

                // Smoke is not affected by chem multiplier
                DamageDict[DamageType.Smoke] = GaugeCoefficient * smokeBodies * 1000;
            }
            else
            {
                DamageDict[DamageType.Smoke] = 0;
            }
        }


        public void CalculateDpsByType(
            DamageType dt,
            float targetAC,
            int testIntervalSeconds,
            float storagePerVolume,
            float storagePerCost,
            float ppm,
            float ppv,
            float ppc,
            bool fuel,
            Scheme targetScheme,
            float impactAngleFromPerpendicularDegrees)
        {
            CalculateRecoil();
            CalculateRailVolumeAndCost(testIntervalSeconds, storagePerVolume, storagePerCost, ppm, ppv, ppc, fuel);
            CalculateRecoilVolumeAndCost();
            CalculateVariableVolumesAndCosts(testIntervalSeconds, storagePerVolume, storagePerCost);
            CalculateVolumeAndCostPerLoader();

            // Kinetic stats needed for pendepth testing
            CalculateVelocity();
            CalculateKineticDamage();
            CalculateAP();

            if (RawKD >= targetScheme.GetRequiredKD(ArmorPierce, impactAngleFromPerpendicularDegrees, HeadModule == Module.SabotHead)
                || (HeadModule == Module.HollowPoint && RawKD >= targetScheme.GetRequiredThump(ArmorPierce)))
            {
                if (dt == DamageType.Kinetic)
                {
                    CalculateKineticDps(targetAC);
                }
                else if (dt == DamageType.EMP)
                {
                    CalculateEmpDps();
                }
                else if (dt == DamageType.Flak)
                {
                    CalculateFlakDps();
                }
                else if (dt == DamageType.Frag)
                {
                    CalculateFragDps();
                }
                else if (dt == DamageType.HE)
                {
                    CalculateHEDps();
                }
                else if (dt == DamageType.HEAT)
                {
                    CalculateHeatDps();
                }
                else if (dt == DamageType.Disruptor)
                {
                    CalculateShieldRps();
                }
                else if (dt == DamageType.Smoke)
                {
                    CalculateSmokeDPS();
                }
            }
            else
            {
                foreach (DamageType dpstype in DpsDict.Keys)
                {
                    DpsDict[dpstype] = 0;
                }
                foreach (DamageType dpstype in DpsPerCostDict.Keys)
                {
                    DpsPerCostDict[dpstype] = 0;
                }
                foreach (DamageType dpstype in DpsPerVolumeDict.Keys)
                {
                    DpsPerVolumeDict[dpstype] = 0;
                }
            }
        }


        /// <summary>
        /// Calculates applied kinetic damage for a given target armor class and impact angle
        /// </summary>
        void CalculateKineticDps(float targetAC)
        {
            CalculateKineticDamage();
            CalculateAP();

            // Hollow point and CIWS ignore impact angle
            if (HeadModule == Module.HollowPoint || targetAC == 20f)
            {
                DamageDict[DamageType.Kinetic] = RawKD * MathF.Min(1, ArmorPierce / targetAC);
            }
            else if (HeadModule == Module.SabotHead)
            {
                DamageDict[DamageType.Kinetic] = RawKD * MathF.Min(1, ArmorPierce / targetAC) * SabotAngleMultiplier;
            }
            else
            {
                DamageDict[DamageType.Kinetic] = RawKD * MathF.Min(1, ArmorPierce / targetAC) * NonSabotAngleMultiplier;
            }

            DpsDict[DamageType.Kinetic] = DamageDict[DamageType.Kinetic] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.Kinetic] = DpsDict[DamageType.Kinetic] / VolumePerLoader;
            DpsPerCostDict[DamageType.Kinetic] = DpsDict[DamageType.Kinetic] / CostPerLoader;
        }


        /// <summary>
        /// Calculates EMP damage per second
        /// </summary>
        void CalculateEmpDps()
        {
            DpsDict[DamageType.EMP] = DamageDict[DamageType.EMP] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.EMP] = DpsDict[DamageType.EMP] / VolumePerLoader;
            DpsPerCostDict[DamageType.EMP] = DpsDict[DamageType.EMP] / CostPerLoader;
        }

        /// <summary>
        /// Calculates Flak damage per second
        /// </summary>
        void CalculateFlakDps()
        {
            DpsDict[DamageType.Flak] = DamageDict[DamageType.Flak] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.Flak] = DpsDict[DamageType.Flak] / VolumePerLoader;
            DpsPerCostDict[DamageType.Flak] = DpsDict[DamageType.Flak] / CostPerLoader;
        }

        /// <summary>
        /// Calculates Frag damage per second
        /// </summary>
        void CalculateFragDps()
        {
            DpsDict[DamageType.Frag] = DamageDict[DamageType.Frag] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.Frag] = DpsDict[DamageType.Frag] / VolumePerLoader;
            DpsPerCostDict[DamageType.Frag] = DpsDict[DamageType.Frag] / CostPerLoader;
        }

        /// <summary>
        /// Calculates HE damage per second
        /// </summary>
        void CalculateHEDps()
        {
            DpsDict[DamageType.HE] = DamageDict[DamageType.HE] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.HE] = DpsDict[DamageType.HE] / VolumePerLoader;
            DpsPerCostDict[DamageType.HE] = DpsDict[DamageType.HE] / CostPerLoader;
        }

        /// <summary>
        /// Calculates HEAT damage per second (HESH scales similarly)
        /// </summary>
        void CalculateHeatDps()
        {
            if (HeadModule == Module.ShapedChargeHead)
            {
                DpsDict[DamageType.HE] = DamageDict[DamageType.HE] / ClusterReloadTime * Uptime;
                DpsPerVolumeDict[DamageType.HE] = DpsDict[DamageType.HE] / VolumePerLoader;
                DpsPerCostDict[DamageType.HE] = DpsDict[DamageType.HE] / CostPerLoader;

                DpsDict[DamageType.HEAT] = DamageDict[DamageType.HEAT] / ClusterReloadTime * Uptime;
                DpsPerVolumeDict[DamageType.HEAT] = DpsDict[DamageType.HEAT] / VolumePerLoader;
                DpsPerCostDict[DamageType.HEAT] = DpsDict[DamageType.HEAT] / CostPerLoader;
            }
            else
            {
                DpsDict[DamageType.HE] = 0;
                DpsPerVolumeDict[DamageType.HE] = 0;
                DpsPerCostDict[DamageType.HE] = 0;

                DpsDict[DamageType.HEAT] = 0;
                DpsPerVolumeDict[DamageType.HEAT] = 0;
                DpsPerCostDict[DamageType.HEAT] = 0;
            }
        }

        /// <summary>
        /// Calculates shield disruption, in % reduction per second per volume
        /// </summary>
        void CalculateShieldRps()
        {
            CalculateEmpDps();

            if (HeadModule == Module.Disruptor)
            {
                DpsDict[DamageType.Disruptor] = DamageDict[DamageType.Disruptor] / ClusterReloadTime * Uptime;
                DpsPerVolumeDict[DamageType.Disruptor] = DpsDict[DamageType.Disruptor] / VolumePerLoader;
                DpsPerCostDict[DamageType.Disruptor] = DpsDict[DamageType.Disruptor] / CostPerLoader;
            }
            else
            {
                DpsDict[DamageType.Disruptor] = 0;
                DpsPerVolumeDict[DamageType.Disruptor] = 0;
                DpsPerCostDict[DamageType.Disruptor] = 0;
            }
        }

        /// <summary>
        /// Calculates smoke strength per second
        /// </summary>
        void CalculateSmokeDPS()
        {
            DpsDict[DamageType.Smoke] = DamageDict[DamageType.Smoke] / ClusterReloadTime * Uptime;
            DpsPerVolumeDict[DamageType.Smoke] = DpsDict[DamageType.Smoke] / VolumePerLoader;
            DpsPerCostDict[DamageType.Smoke] = DpsDict[DamageType.Smoke] / CostPerLoader;
        }

        /// <summary>
        /// Calculate volume of input and loader
        /// </summary>
        public void CalculateLoaderVolumeAndCost()
        {
            LoaderVolume = 0;
            LoaderCost = 0;

            // DIF can't use loaders, only inputs
            if (IsDif)
            {
                LoaderVolume = 1f;
                LoaderCost = 50f;
            }
            else
            {
                if (IsBelt)
                {
                    LoaderVolume = 1f + BeltfedClipsPerLoader + BeltfedInputsPerLoader;
                    LoaderCost = 600f + 160f * BeltfedClipsPerLoader + 50f * BeltfedInputsPerLoader;
                }
                else
                {
                    if (TotalLength <= 1000f)
                    {
                        LoaderVolume = 1f + RegularClipsPerLoader + RegularInputsPerLoader;
                        LoaderCost = 240f + 160f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 2000f)
                    {
                        LoaderVolume = 2f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 300f + 200f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 3000f)
                    {
                        LoaderVolume = 3f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 330f + 220f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 4000f)
                    {
                        LoaderVolume = 4f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 360f + 240f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 5000f)
                    {
                        LoaderVolume = 5f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 390f + 260f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 6000f)
                    {
                        LoaderVolume = 6f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 420f + 280f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 7000f)
                    {
                        LoaderVolume = 7f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 450f + 300f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }
                    else if (TotalLength <= 8000f)
                    {
                        LoaderVolume = 8f * (1f + RegularClipsPerLoader) + RegularInputsPerLoader;
                        LoaderCost = 480f + 320f * RegularClipsPerLoader + 50 * RegularInputsPerLoader;
                    }

                    if (UsesAmmoEjector && !IsBelt)
                    {
                        LoaderVolume += 2f;
                        LoaderCost += 10f;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates volume per loader of recoil absorbers
        /// </summary>
        public void CalculateRecoilVolumeAndCost()
        {
            if (GunUsesRecoilAbsorbers)
            {
                RecoilVolume = TotalRecoil / (ClusterReloadTime * 120f); // Absorbers absorb 120 per second per metre
                RecoilCost = RecoilVolume * 80f; // Absorbers cost 80 per metre
            }
        }


        /// <summary>
        /// Calculates marginal volume of coolers to sustain fire from one additional loader.  Ignores cooling from firing piece
        /// </summary>
        public void CalculateCoolerVolumeAndCost()
        {
            float coolerVolume;
            float coolerCost;
            if (GPCasingCount > 0)
            {
                coolerVolume = CooldownTime * MathF.Sqrt(Gauge / 1000) / ClusterReloadTime / (1 + BarrelCount * 0.05f) / 0.176775f;
                coolerCost = coolerVolume * 50f;
            }
            else
            {
                coolerVolume = 0;
                coolerCost = 0;
            }

            CoolerVolume = coolerVolume;
            CoolerCost = coolerCost;
        }

        /// <summary>
        /// Calculates marginal volume per loader of rail chargers and engines
        /// </summary>
        public void CalculateRailVolumeAndCost(
            int testIntervalSeconds,
            float storagePerVolume,
            float storagePerCost,
            float ppm,
            float ppv,
            float ppc,
            bool fuel)
        {
            if (RailDraw > 0)
            {
                float drawPerSecond = RailDraw / ClusterReloadTime;
                ChargerVolume = drawPerSecond / 200f; // Chargers provide 200 Energy per second
                ChargerCost = ChargerVolume * 400f; // Chargers cost 400 per metre

                // Volume and cost of engine
                EngineVolume = drawPerSecond / ppv;
                EngineCost = drawPerSecond / ppc;

                // Materials burned by engine
                FuelBurned = drawPerSecond * testIntervalSeconds / ppm * Uptime;

                float fuelStorageNeeded;
                if (fuel)
                {
                    // Volume and cost of special fuel access blocks
                    // 1 fuel per MINUTE = 1/50 m^3 and 0.2 material cost
                    // 1 fuel per SECOND = 60/50 (1.2) m^3 and 12 material cost
                    // 1 m^3 fuel access = 10 material cost
                    float fuelAccessNeeded = drawPerSecond / ppm;
                    FuelAccessVolume = fuelAccessNeeded * 1.2f;
                    FuelAccessCost = FuelAccessVolume * 10;

                    // Fuel access blocks store enough materials to run for 10 minutes
                    fuelStorageNeeded = drawPerSecond * MathF.Max(testIntervalSeconds - 600f, 0) / ppm;
                }
                else
                {
                    fuelStorageNeeded = drawPerSecond * testIntervalSeconds / ppm;
                }

                // Storage for materials burned
                FuelStorageVolume = fuelStorageNeeded / storagePerVolume * Uptime;
                FuelStorageCost = fuelStorageNeeded / storagePerCost * Uptime;
            }
            else
            {
                ChargerVolume = 0;
                ChargerCost = 0;
                FuelBurned = 0;
                EngineVolume = 0;
                EngineCost = 0;
                FuelAccessVolume = 0;
                FuelAccessCost = 0;
                FuelStorageVolume = 0;
                FuelStorageCost = 0;
            }
        }


        /// <summary>
        /// Calculates all volumes and costs dependent on testing interval
        /// </summary>
        public void CalculateVariableVolumesAndCosts(int testIntervalSeconds, float storagePerVolume, float storagePerCost)
        {
            // Calculate cost of shell itself
            CostPerShell = (ProjectileLength + (GPCasingCount + RGCasingCount) / 4)
                * 5f
                * GaugeCoefficient
                / Gauge;

            AmmoUsed = CostPerShell * testIntervalSeconds / ClusterReloadTime * Uptime;

            // Calculate volume and cost of ammo crates
            // 1/50 m^3 and 1/5 material cost per material per minute
            float shellCostPerMinute = CostPerShell / ClusterReloadTime * 60f;
            AmmoAccessVolume = shellCostPerMinute / 50f;
            AmmoAccessCost = shellCostPerMinute / 5f;

            // Calculate volume and cost of material storage
            // Ammo crates hold enough materials for 10 minutes
            float ammoStorageNeeded = CostPerShell * MathF.Max(testIntervalSeconds - 600f, 0) / ClusterReloadTime * Uptime;
            AmmoStorageVolume = ammoStorageNeeded / storagePerVolume;
            AmmoStorageCost = ammoStorageNeeded / storagePerCost;
        }

        /// <summary>
        /// Calculates volume used by shell, including input, loader, cooling, recoil absorbers, and rail chargers
        /// </summary>
        public void CalculateVolumeAndCostPerLoader()
        {
            VolumePerLoader =
                LoaderVolume
                + RecoilVolume
                + CoolerVolume
                + ChargerVolume
                + AmmoAccessVolume
                + AmmoStorageVolume
                + EngineVolume
                + FuelAccessVolume
                + FuelStorageVolume;

            CostPerLoader =
                LoaderCost
                + RecoilCost
                + CoolerCost
                + ChargerCost
                + AmmoUsed
                + AmmoAccessCost
                + AmmoStorageCost
                + FuelBurned
                + EngineCost
                + FuelAccessCost
                + FuelStorageCost;

            CostPerVolume = CostPerLoader / VolumePerLoader;
        }


        /// <summary>
        /// Calculates total number of modules in shell
        /// </summary>
        public void GetModuleCounts()
        {
            // ModuleCountTotal starts at 1 for head
            ModuleCountTotal = 1;
            if (BaseModule != null)
            {
                ModuleCountTotal += 1;
            }

            foreach (float modCount in BodyModuleCounts)
            {
                ModuleCountTotal += modCount;
            }

            ModuleCountTotal += MathF.Ceiling(GPCasingCount) + RGCasingCount;
        }        
    }
}
