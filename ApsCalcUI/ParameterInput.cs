using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace ApsCalcUI
{
    public partial class ParameterInput : Form
    {
        readonly List<TestParameters> parameterList = [];
        int testsInQueue = 0;
        // For automatically setting min gauge to compensate for smoke minimum 200 mm
        decimal nonSmokeMinGauge = 18;

        readonly Dictionary<int, int> gaugeHardCaps = new()
        {
            { 1, 500 },
            { 2, 250 },
            { 3, 225 },
            { 4, 200 },
            { 5, 175 },
            { 6, 150 }
        };
        int gaugeHardCap = 500;

        public ParameterInput()
        {
            InitializeComponent();
        }

        private void ParameterInput_Load(object sender, EventArgs e)
        {
            // Barrel count dropdown
            BarrelCountItem[] barrelCounts =
            [
                new BarrelCountItem(1, "1, 500 mm max"),
                new BarrelCountItem(2,"2, 250 mm max"),
                new BarrelCountItem(3,"3, 225 mm max"),
                new BarrelCountItem(4,"4, 200 mm max"),
                new BarrelCountItem(5,"5, 175 mm max"),
                new BarrelCountItem(6,"6, 150 mm max")
            ];
            BarrelCountDD.DataSource = barrelCounts;
            BarrelCountDD.DisplayMember = "Text";
            BarrelCountDD.SelectedIndex = 0;

            // Barrel length limit type dropdown
            BarrelLengthLimitTypeItem[] barrelLengthLimitTypeItems =
            [
                new BarrelLengthLimitTypeItem(BarrelLengthLimit.Calibers, "calibers"),
                new BarrelLengthLimitTypeItem(BarrelLengthLimit.FixedLength, "m")
            ];
            BarrelLengthLimitDD.DataSource = barrelLengthLimitTypeItems;
            BarrelLengthLimitDD.DisplayMember = "Text";
            BarrelLengthLimitDD.SelectedIndex = 0;

            // Head module checked list
            foreach (HeadModuleItem head in HeadModuleItem.GenerateHeadList())
            {
                HeadModulesCL.Items.Add(head, false);
            }
            HeadModulesCL.DisplayMember = "Name";

            // Variable module checked list
            foreach (VariableModuleItem varMod in VariableModuleItem.GenerateBodyList())
            {
                VariableModulesCL.Items.Add(varMod, false);
            }
            VariableModulesCL.DisplayMember = "Name";

            // Damage type dropdown
            DamageTypeItem[] damageTypes =
            [
                new DamageTypeItem(DamageType.Kinetic, "Kinetic"),
                new DamageTypeItem(DamageType.EMP, "EMP"),
                new DamageTypeItem(DamageType.MD, "Munition defense"),
                new DamageTypeItem(DamageType.Frag, "Frag"),
                new DamageTypeItem(DamageType.HE, "HE" ),
                new DamageTypeItem(DamageType.HEAT, "HEAT"),
                new DamageTypeItem(DamageType.Incendiary, "Incendiary"),
                new DamageTypeItem(DamageType.Disruptor, "Disruptor"),
                new DamageTypeItem(DamageType.Smoke, "Smoke")
            ];
            DamageTypeDD.DataSource = damageTypes;
            DamageTypeDD.DisplayMember = "Text";
            DamageTypeDD.SelectedIndex = 0;

            // Target AC checked list
            TargetACCL.Items.Add(new TargetACItem(8, "8, wood"));
            TargetACCL.Items.Add(new TargetACItem(9.6f, "9.6, stacked wood"));
            TargetACCL.Items.Add(new TargetACItem(20, "20, munitions"));
            TargetACCL.Items.Add(new TargetACItem(35, "35, alloy"));
            TargetACCL.Items.Add(new TargetACItem(40, "40, metal"));
            TargetACCL.Items.Add(new TargetACItem(42, "42, stacked alloy"));
            TargetACCL.Items.Add(new TargetACItem(48, "48, stacked metal"));
            TargetACCL.Items.Add(new TargetACItem(60, "60, heavy armour"));
            TargetACCL.Items.Add(new TargetACItem(72, "72, stacked heavy armour"));
            TargetACCL.DisplayMember = "Text";

            // Armor layer dropdown
            foreach (Layer armorLayer in Layer.AllLayers)
            {
                ArmorLayerDD.Items.Add(new ArmorLayerItem(armorLayer.Name, armorLayer));
            }
            ArmorLayerDD.DisplayMember = "Name";
            ArmorLayerDD.SelectedIndex = 0;

            // Armor scheme list
            ArmorLayerLB.DisplayMember = "Name";
        }

        /// <summary>
        /// Updates max module counts in each category according to module count limits
        /// Alse updates length restrictions
        /// </summary>
        private void UpdateModuleCounts()
        {
            decimal maxFixedCount = Math.Min(20, Math.Floor(MaxLengthUD.Value / MinGaugeUD.Value)) - 1; // always have a head
            if (!NoBaseRB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedGravCompCB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedPendepthFuzeCB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedAltitudeFuzeCB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedInertialFuzeCB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedTimedFuzeCB.Checked)
            {
                maxFixedCount -= 1;
            }
            if (FixedDefuzeCB.Checked)
            {
                maxFixedCount -= 1;
            }

            decimal maxCasingCount;

            SolidBodyFixedUD.Maximum = maxFixedCount;
            SabotBodyFixedUD.Maximum = SolidBodyFixedUD.Maximum - SolidBodyFixedUD.Value;
            EmpBodyFixedUD.Maximum = SabotBodyFixedUD.Maximum - SabotBodyFixedUD.Value;
            FlakBodyFixedUD.Maximum = EmpBodyFixedUD.Maximum - EmpBodyFixedUD.Value;
            FragBodyFixedUD.Maximum = FlakBodyFixedUD.Maximum - FlakBodyFixedUD.Value;
            HEBodyFixedUD.Maximum = FragBodyFixedUD.Maximum - FragBodyFixedUD.Value;
            FinFixedUD.Maximum = HEBodyFixedUD.Maximum - HEBodyFixedUD.Value;
            SmokeBodyFixedUD.Maximum = FinFixedUD.Maximum - FinFixedUD.Value;
            IncendiaryBodyFixedUD.Maximum = SmokeBodyFixedUD.Maximum - SmokeBodyFixedUD.Value;

            maxCasingCount = HEBodyFixedUD.Maximum - HEBodyFixedUD.Value;
            // Check whether max GP casing count is current set to maximum possible value
            bool maxGPAtMaxPossibleValue = MaxGPUD.Value == MaxGPUD.Maximum;
            MaxGPUD.Maximum = maxCasingCount;
            if (MaxGPUD.Value > MaxGPUD.Maximum || maxGPAtMaxPossibleValue)
            {
                MaxGPUD.Value = MaxGPUD.Maximum;
            }

            bool maxRGAtMaxPossibleValue = MaxRGUD.Value == MaxRGUD.Maximum;
            MaxRGUD.Maximum = maxCasingCount;
            if (MaxRGUD.Value > MaxRGUD.Maximum || maxRGAtMaxPossibleValue)
            {
                MaxRGUD.Value = MaxRGUD.Maximum;
            }

            // Set length restrictions
            // Check whether min length is currently set to minimum possible value
            bool minLengthAtMinPossibleValue = MinLengthUD.Value == MinLengthUD.Minimum;
            float minLength = (float)MinGaugeUD.Value;
            if (!NoBaseRB.Checked)
            {
                minLength *= 2;
            }
            minLength += (float)SolidBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.SolidBody.MaxLength);
            minLength += (float)SabotBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.SabotBody.MaxLength);
            minLength += (float)EmpBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.EmpBody.MaxLength);
            minLength += (float)FlakBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.MDBody.MaxLength);
            minLength += (float)FragBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.FragBody.MaxLength);
            minLength += (float)HEBodyFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.HEBody.MaxLength);
            minLength += (float)FinFixedUD.Value * Math.Min((float)MinGaugeUD.Value, Module.FinBody.MaxLength);

            if (FixedGravCompCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.GravCompensator.MaxLength);
            }

            if (FixedPendepthFuzeCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.PenDepthFuse.MaxLength);
            }

            if (FixedTimedFuzeCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.TimedFuse.MaxLength);
            }

            if (FixedInertialFuzeCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.InertialFuse.MaxLength);
            }

            if (FixedAltitudeFuzeCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.AltitudeFuse.MaxLength);
            }

            if (FixedDefuzeCB.Checked)
            {
                minLength += Math.Min((float)MinGaugeUD.Value, Module.Defuse.MaxLength);
            }

            MinLengthUD.Minimum = (decimal)minLength;
            // Set min length to min value if it was there before updating
            if (minLengthAtMinPossibleValue)
            {
                MinLengthUD.Value = (decimal)minLength;
            }

            MaxLengthUD.Minimum = Math.Max(MinLengthUD.Minimum + 1, MinLengthUD.Value + 1);
        }

        /// <summary>
        /// Set max gauge according to barrel count
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BarrelCountDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            gaugeHardCap = gaugeHardCaps[((BarrelCountItem)BarrelCountDD.SelectedValue).ID];
            MinGaugeUD.Maximum = gaugeHardCap;
            MaxGaugeUD.Maximum = gaugeHardCap;
            UpdateModuleCounts();
        }

        /// <summary>
        /// Ensures max gauge is never smaller than min gauge
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinGaugeUD_ValueChanged(object sender, EventArgs e)
        {
            // Update non-smoke minimum
            if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID != DamageType.Smoke)
            {
                nonSmokeMinGauge = MinGaugeUD.Value;
            }
            MaxGaugeUD.Minimum = MinGaugeUD.Value;
            UpdateModuleCounts();
        }

        private void MaxGaugeUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void SolidBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void SabotBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void EmpBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FlakBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FragBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void HEBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FuseFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FinFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void SmokeBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void IncendiaryBodyFixedUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void NoBaseRB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }


        /// <summary>
        /// Enables bore evacuator checkbox if GP is allowed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaxGPUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void MaxRGUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void MinLengthUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void MaxLengthUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }


        /// <summary>
        /// Activates AC or armor selection panels depending on damage type
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DamageTypeDD_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set and reset minimum gauge to compensate for smoke minimum 200 mm
            if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID != DamageType.Smoke)
            {
                MinGaugeUD.Minimum = 18;
                MinGaugeUD.Value = nonSmokeMinGauge;
            }
            else if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Smoke)
            {
                MinGaugeUD.Minimum = 200;
                MinGaugeUD.Value = Math.Max(200, nonSmokeMinGauge);
            }

            if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Kinetic)
            {
                TargetACPanel.Enabled = true;
                TargetACPanel.Visible = true;
                TargetACCL.Visible = true;
                TargetACLabel.Visible = true;

            }
            else
            {
                TargetACPanel.Enabled = false;
                TargetACPanel.Visible = false;
                TargetACCL.Visible = false;
                TargetACLabel.Visible = false;
            }

            UpdateDisruptorPanel();

            if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Frag)
            {
                FragAnglePanel.Enabled = true;
                FragAnglePanel.Visible = true;
                FragAngleLabel.Visible = true;
                FragAngleUD.Visible = true;
            }
            else
            {
                FragAnglePanel.Enabled = false;
                FragAnglePanel.Visible = false;
                FragAngleLabel.Visible = false;
                FragAngleUD.Visible = false;
            }
        }

        private void HeadModulesCL_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisruptorPanel();
        }

        private void UpdateDisruptorPanel()
        {
            bool disruptorSelected = false;
            foreach (HeadModuleItem item in HeadModulesCL.CheckedItems)
            {
                if (item.Name == "Disruptor conduit")
                {
                    disruptorSelected = true;
                    break;
                }
            }
            if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Disruptor)
            {
                disruptorSelected = true;
            }

            DisruptorPanel.Enabled = disruptorSelected;
            DisruptorPanel.Visible = disruptorSelected;
            DisruptorLabel.Visible = disruptorSelected;
            DisruptorUD.Visible = disruptorSelected;
            // Set value to 0 when not in use; default to 100% when in use
            DisruptorUD.Value = disruptorSelected ? 100m : 0m;
        }


        private void AddLayerButton_Click(object sender, EventArgs e)
        {
            if (ArmorLayerDD.SelectedItem != null)
            {
                ArmorLayerLB.BeginUpdate();
                ArmorLayerLB.Items.Add((ArmorLayerItem)ArmorLayerDD.SelectedItem);
                ArmorLayerLB.EndUpdate();
            }
        }

        private void RemoveLayerButton_Click(object sender, EventArgs e)
        {
            if (ArmorLayerLB.Items.Count > 0)
            {
                ArmorLayerLB.BeginUpdate();
                ArmorLayerLB.Items.Remove(ArmorLayerLB.Items[^1]);
                ArmorLayerLB.EndUpdate();
            }
        }

        private void MaxDrawUD_ValueChanged(object sender, EventArgs e)
        {
            if (MaxDrawUD.Value > 0)
            {
                EnginePanel.Enabled = true;
            }
            else
            {
                EnginePanel.Enabled = false;
            }
        }

        private void PendepthCB_CheckedChanged(object sender, EventArgs e)
        {
            TargetSchemePanel.Enabled = PendepthCB.Checked;
        }

        private void FixedGravCompCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FixedPendepthFuzeCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FixedTimedFuzeCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FixedInertialFuzeCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FixedAltitudeFuzeCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
        }

        private void FixedDefuzeCB_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModuleCounts();
            AmmoEjectorCB.Enabled = !FixedDefuzeCB.Checked;
            AmmoEjectorCB.Checked = FixedDefuzeCB.Checked;
        }


        // Sets max length according to modded max gauge and DIF checkboxes
        private void CheckMaxLength()
        {
            if (DifCB.Checked && ModdedMaxGaugeCB.Checked)
            {
                MaxLengthUD.Maximum = 20000;
                MaxLengthUD.Value = 20000;
            }
            else if (DifCB.Checked)
            {
                MaxLengthUD.Maximum = 10000;
                MaxLengthUD.Value = 10000;
            }
            else
            {
                MaxLengthUD.Maximum = 8000;
                MaxLengthUD.Value = 8000;
            }
        }
        private void ModdedMaxGaugeCB_CheckedChanged(object sender, EventArgs e)
        {
            if (ModdedMaxGaugeCB.Checked)
            {
                MaxGaugeUD.Maximum = 1000;
                if (MaxGaugeUD.Value == 500)
                {
                    MaxGaugeUD.Value = 1000;
                }
            }
            else
            {
                MaxGaugeUD.Maximum = 500;
            }

            CheckMaxLength();
        }

        private void DifCB_CheckedChanged(object sender, EventArgs e)
        {
            CheckMaxLength();
        }

        private void BarrelLengthLimitCB_CheckedChanged(object sender, EventArgs e)
        {
            BarrelLengthLimitDD.Enabled = BarrelLengthLimitCB.Checked;
            BarrelLengthLimitUD.Enabled = BarrelLengthLimitCB.Checked;
            MaxInaccUD.Enabled = BarrelLengthLimitCB.Checked;
            BarrelLengthHardLimitCB.Enabled = BarrelLengthLimitCB.Checked;
            if (!BarrelLengthLimitCB.Checked)
            {
                BarrelLengthHardLimitCB.Checked = true;
            }
        }

        private void TracerRB_CheckedChanged(object sender, EventArgs e)
        {
            RofRpmPanel.Enabled = TracerRB.Checked;
            RofRpmPanel.Visible = TracerRB.Checked;
            RofRpmLabel.Visible = TracerRB.Checked;
            RofRpmUD.Visible = TracerRB.Checked;
        }

        private void BeltfedClipsPerLoaderUD_ValueChanged(object sender, EventArgs e)
        {
            BeltfedInputsPerLoaderUD.Maximum = 4 + 3 + 2 * (BeltfedClipsPerLoaderUD.Value - 1);
        }

        private void RegularClipsPerLoaderUD_ValueChanged(object sender, EventArgs e)
        {
            RegularInputsPerLoaderUD.Maximum = RegularClipsPerLoaderUD.Value + 1;
        }

        /// <summary>
        /// Validates input and creates test parameters from current selections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddParametersButton_Click(object sender, EventArgs e)
        {
            bool error = false;
            QueueErrorProvider.Clear();

            // For checking chemical types for pendepth
            List<Module> varModList = [];
            foreach (VariableModuleItem varModItem in VariableModulesCL.CheckedItems)
            {
                varModList.Add(Module.AllModules[varModItem.Index]);
            }

            if (HeadModulesCL.CheckedItems.Count == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Select at least one head");
            }
            else if (VariableModulesCL.CheckedItems.Count == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Select at least one variable module");
            }
            else if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Kinetic && TargetACCL.SelectedItems.Count == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Select at least one target AC");
            }
            else if (PendepthCB.Checked && ArmorLayerLB.Items == null)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Add at least one layer to the target armor scheme or deselect \"Pendepth\"");
            }
            else if (MinVelocityUD.Value > 0 && MaxDrawUD.Value == 0 && MaxGPUD.Value == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Increase max gunpowder or rail draw allowance, or else set min velocity to 0");
            }
            else if (((DamageTypeItem)DamageTypeDD.SelectedItem).ID == DamageType.Kinetic && MaxDrawUD.Value == 0 && MaxGPUD.Value == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Increase max gunpowder or rail draw allowance, or else choose a different damage type");
            }
            else if (PendepthCB.Checked && ArmorLayerLB.Items.Count == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, "Add at least one armor layer with \"Add\" or uncheck \"Pendepth\"");
            }
            else if (BarrelLengthLimitCB.Checked && !BarrelLengthHardLimitCB.Checked && MinRangeUD.Value == 0)
            {
                error = true;
                QueueErrorProvider.SetError(AddParametersButton, 
                    "Soft barrel length limit selected but no \"Min Eff. Range\" entered.\n"
                    + "Check \"Hard inaccuracy limit\", uncheck \"Limit barrel length\", or\n"
                    + "enter nonzero \"Min Eff. Range\" for inaccuracy calculations.");
            }

            if (!error)
            {
                // Run button is off by default
                RunButton.Enabled = true;

                // Update "Tests in Queue" text
                testsInQueue += 1;
                TestsInQueueLabel.Text = "Tests in Queue: " + testsInQueue.ToString();

                int barrelCount = ((BarrelCountItem)BarrelCountDD.SelectedItem).ID;
                TestParameters testParameters = new()
                {
                    BarrelCount = barrelCount,
                    MinGauge = (int)MinGaugeUD.Value,
                    MaxGauge = (int)MaxGaugeUD.Value,

                    ImpactAngle = (float)ImpactAngleUD.Value
                };

                List<int> headIndices = [];
                foreach (HeadModuleItem head in HeadModulesCL.CheckedItems)
                {
                    headIndices.Add(head.Index);
                }
                testParameters.HeadIndices = headIndices;

                if (BaseBleederRB.Checked)
                {
                    testParameters.BaseModule = Module.BaseBleeder;
                }
                else if (SupercavRB.Checked)
                {
                    testParameters.BaseModule = Module.Supercav;
                }
                else if (TracerRB.Checked)
                {
                    testParameters.BaseModule = Module.Tracer;
                }
                else if (GravRB.Checked)
                {
                    testParameters.BaseModule = Module.GravRam;
                }
                else
                {
                    testParameters.BaseModule = null;
                }

                // Get fuze counts
                float pendepthFuzeCount;
                if (FixedPendepthFuzeCB.Checked)
                {
                    pendepthFuzeCount = 1f;
                }
                else
                {
                    pendepthFuzeCount = 0f;
                }

                float timedFuzeCount;
                if (FixedTimedFuzeCB.Checked)
                {
                    timedFuzeCount = 1f;
                }
                else
                {
                    timedFuzeCount = 0f;
                }

                float inertialFuzeCount;
                if (FixedInertialFuzeCB.Checked)
                {
                    inertialFuzeCount = 1f;
                }
                else
                {
                    inertialFuzeCount = 0f;
                }

                float altitudeFuzeCount;
                if (FixedAltitudeFuzeCB.Checked)
                {
                    altitudeFuzeCount = 1f;
                }
                else
                {
                    altitudeFuzeCount = 0f;
                }

                float defuzeCount;
                if (FixedDefuzeCB.Checked)
                {
                    defuzeCount = 1f;
                }
                else
                {
                    defuzeCount = 0f;
                }

                float gravCompCount;
                if (FixedGravCompCB.Checked)
                {
                    gravCompCount = 1f;
                }
                else
                {
                    gravCompCount = 0f;
                }

                float[] fixedModuleCounts =
                [
                    (float)SolidBodyFixedUD.Value,
                    (float)SabotBodyFixedUD.Value,
                    (float)EmpBodyFixedUD.Value,
                    (float)FlakBodyFixedUD.Value,
                    (float)FragBodyFixedUD.Value,
                    (float)HEBodyFixedUD.Value,
                    (float)FinFixedUD.Value,
                    (float)SmokeBodyFixedUD.Value,
                    (float)IncendiaryBodyFixedUD.Value,
                    pendepthFuzeCount,
                    timedFuzeCount,
                    inertialFuzeCount,
                    altitudeFuzeCount,
                    defuzeCount,
                    gravCompCount
                ];
                testParameters.FixedModulecounts = fixedModuleCounts;

                float minModuleCount;
                if (NoBaseRB.Checked)
                {
                    minModuleCount = 1;
                }
                else
                {
                    minModuleCount = 2;
                }
                minModuleCount += (float)fixedModuleCounts.Sum();
                testParameters.MinModuleCount = minModuleCount;

                // Add all selected variable module indicies to list
                List<int> varModIndices = [];
                foreach (VariableModuleItem varMod in VariableModulesCL.CheckedItems)
                {
                    varModIndices.Add(varMod.Index);
                }
                // Pad list out to number of possible variable modules by duplicating first index
                // Copies of first index will be ignored but are necessary to avoid errors in
                // ShellCalc.GenerateModConfigs
                while (varModIndices.Count < Module.GetVariableModuleCount())
                {
                    varModIndices.Add(varModIndices[0]);
                }
                int[] variableModuleIndices = [.. varModIndices];
                testParameters.VariableModuleIndices = variableModuleIndices;

                testParameters.RegularClipsPerLoader = (int)RegularClipsPerLoaderUD.Value;
                testParameters.RegularInputsPerLoader = (int)RegularInputsPerLoaderUD.Value;
                testParameters.BeltfedClipsPerLoader = (int)BeltfedClipsPerLoaderUD.Value;
                testParameters.BeltfedInputsPerLoader = (int)BeltfedInputsPerLoaderUD.Value;
                testParameters.UsesAmmoEjector = AmmoEjectorCB.Checked;

                testParameters.MaxGPCasingCount = (float)MaxGPUD.Value;
                testParameters.CasingIncrement = (float)CasingIncrementUD.Value;
                testParameters.MaxRGCasingCount = (float)MaxRGUD.Value;
                testParameters.MinLength = (float)MinLengthUD.Value;
                testParameters.MaxLength = (float)MaxLengthUD.Value;
                testParameters.MaxDraw = (float)MaxDrawUD.Value;
                testParameters.MaxRecoil = (float)MaxRecoilUD.Value;
                testParameters.MinVelocity = (float)MinVelocityUD.Value;
                testParameters.MinEffectiveRange = (float)MinRangeUD.Value;
                testParameters.DamageType = ((DamageTypeItem)DamageTypeDD.SelectedItem).ID;

                testParameters.FragConeAngle = (float)FragAngleUD.Value;
                testParameters.FragAngleMultiplier = (2 + MathF.Sqrt(testParameters.FragConeAngle)) / 16f;

                if (testParameters.DamageType == DamageType.HEAT)
                {
                    // Overwrite head list with shaped charge head
                    testParameters.HeadIndices.Clear();
                    int modIndex = 0;
                    foreach (Module head in Module.AllModules)
                    {
                        if (head == Module.ShapedChargeHead)
                        {
                            testParameters.HeadIndices.Add(modIndex);
                            break;
                        }
                        modIndex++;
                    }
                }
                else if (testParameters.DamageType == DamageType.Disruptor)
                {
                    // Overwrite head list with disruptor conduit
                    testParameters.HeadIndices.Clear();
                    int modIndex = 0;
                    foreach (Module head in Module.AllModules)
                    {
                        if (head == Module.Disruptor)
                        {
                            testParameters.HeadIndices.Add(modIndex);
                            break;
                        }
                        modIndex++;
                    }
                }

                testParameters.MinDisruptor = (float)(DisruptorUD.Value / 100m);

                Scheme targetArmorScheme = new();
                if (PendepthCB.Checked)
                {
                    foreach (ArmorLayerItem layerItem in ArmorLayerLB.Items)
                    {
                        targetArmorScheme.LayerList.Add(layerItem.Layer);
                    }
                }
                else
                {
                    targetArmorScheme.LayerList.Add(Layer.Air);
                }
                targetArmorScheme.CalculateLayerAC();
                testParameters.ArmorScheme = targetArmorScheme;

                List<float> targetACList = [];
                if (testParameters.DamageType == DamageType.Kinetic)
                {
                    foreach (TargetACItem ac in TargetACCL.CheckedItems)
                    {
                        targetACList.Add(ac.ID);
                    }
                }
                else
                {
                    float averageAC = targetArmorScheme.GetAverageAC();
                    targetACList.Add(averageAC); // at least one AC value is needed for testing (see RunButton_Click)
                }
                testParameters.TargetACList = targetACList;

                testParameters.SabotAngleMultiplier =
                    MathF.Abs(MathF.Cos((testParameters.ImpactAngle + targetArmorScheme.LayerList[0].BaseAngle) * MathF.PI / 240f));
                testParameters.NonSabotAngleMultiplier =
                    MathF.Abs(MathF.Cos((testParameters.ImpactAngle + targetArmorScheme.LayerList[0].BaseAngle) * MathF.PI / 180f));

                testParameters.TestType = PerVolumeRB.Checked ? TestType.DpsPerVolume : TestType.DpsPerCost;

                testParameters.TestInterval = (int)TestIntervalUD.Value;

                if (GenericStorageButton.Checked)
                {
                    testParameters.StoragePerVolume = 500f;
                    testParameters.StoragePerCost = 250f;
                }
                else if (CargoContainerStorageButton.Checked)
                {
                    testParameters.StoragePerVolume = 1000f;
                    testParameters.StoragePerCost = 469.5652f;
                }
                else if (CoalStorageButton.Checked)
                {
                    testParameters.StoragePerVolume = 583.3333f;
                    testParameters.StoragePerCost = 218.75f;
                }

                testParameters.EnginePpm = (float)EnginePpmUD.Value;
                testParameters.EnginePpv = (float)EnginePpvUD.Value;
                testParameters.EnginePpc = (float)EnginePpcUD.Value;

                testParameters.EngineUsesFuel = EngineFuelCB.Checked;

                testParameters.FiringPieceIsDif = DifCB.Checked;
                testParameters.GunUsesRecoilAbsorbers = GunUsesRecoilAbsorbersCB.Checked;

                testParameters.MaxInaccuracy = (float)MaxInaccUD.Value;
                testParameters.RateOfFireRpm = (float)RofRpmUD.Value;
                testParameters.LimitBarrelLength = BarrelLengthLimitCB.Checked;
                testParameters.MaxBarrelLength = (float)BarrelLengthLimitUD.Value;
                testParameters.BarrelLengthLimitType = ((BarrelLengthLimitTypeItem)BarrelLengthLimitDD.SelectedItem).ID;
                testParameters.BarrelLengthHardLimit = BarrelLengthHardLimitCB.Checked;

                testParameters.VerboseOutputIsChecked = VerboseOutputCB.Checked;
                testParameters.RawNumberOutputIsChecked = RawNumberOutputCB.Checked;
                // Must use semicolons to separate columns if using commas for decimals
                testParameters.ColumnDelimiter = CommaDecimalCB.Checked ? ';' : ',';
                testParameters.UseBruteForce = UseBruteForceCB.Checked;

                parameterList.Add(testParameters);
            }
        }

        /// <summary>
        /// Runs tests for all parameters in queue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunButton_Click(object sender, EventArgs e)
        {
            bool error = false;

            if (parameterList.Count == 0)
            {
                error = true;
                RunErrorProvider.SetError(RunButton, "Add parameters to queue");
            }

            if (!error)
            {
                // Lock buttons
                TestsInQueueLabel.Text = "Tests Remaining: " + testsInQueue.ToString();
                AddParametersButton.Enabled = false;
                RunButton.Enabled = false;
                RunButton.Text = "Running...";

                try
                {
                    ParallelOptions options = new() { MaxDegreeOfParallelism = (int)MathF.Ceiling(Environment.ProcessorCount * 0.75f) };
                    foreach (TestParameters testParameters in parameterList)
                    {
                        Stopwatch stopwatch = new();
                        stopwatch.Start();
                        testParameters.ArmorScheme.CalculateLayerAC();
                        foreach (float ac in testParameters.TargetACList)
                        {
                            ShellCalc MakeShellCalc(int gauge, float gaugeMultiplier) => new(
                                testParameters.BarrelCount,
                                gauge,
                                gaugeMultiplier,
                                testParameters.HeadIndices,
                                testParameters.BaseModule,
                                testParameters.FixedModulecounts,
                                testParameters.MinModuleCount,
                                testParameters.VariableModuleIndices,
                                testParameters.RegularClipsPerLoader,
                                testParameters.RegularInputsPerLoader,
                                testParameters.BeltfedClipsPerLoader,
                                testParameters.BeltfedInputsPerLoader,
                                testParameters.UsesAmmoEjector,
                                testParameters.MaxGPCasingCount,
                                testParameters.CasingIncrement,
                                testParameters.MaxRGCasingCount,
                                testParameters.MinLength,
                                testParameters.MaxLength,
                                testParameters.MaxDraw,
                                testParameters.MaxRecoil,
                                testParameters.MinVelocity,
                                testParameters.MinEffectiveRange,
                                testParameters.ImpactAngle,
                                testParameters.SabotAngleMultiplier,
                                testParameters.NonSabotAngleMultiplier,
                                ac,
                                testParameters.DamageType,
                                testParameters.FragConeAngle,
                                testParameters.FragAngleMultiplier,
                                testParameters.MinDisruptor,
                                testParameters.ArmorScheme,
                                testParameters.TestType,
                                testParameters.TestInterval,
                                testParameters.StoragePerVolume,
                                testParameters.StoragePerCost,
                                testParameters.EnginePpm,
                                testParameters.EnginePpv,
                                testParameters.EnginePpc,
                                testParameters.EngineUsesFuel,
                                testParameters.FiringPieceIsDif,
                                testParameters.GunUsesRecoilAbsorbers,
                                testParameters.MaxInaccuracy,
                                testParameters.RateOfFireRpm,
                                testParameters.LimitBarrelLength,
                                testParameters.MaxBarrelLength,
                                testParameters.BarrelLengthLimitType,
                                testParameters.BarrelLengthHardLimit,
                                testParameters.VerboseOutputIsChecked,
                                testParameters.RawNumberOutputIsChecked,
                                testParameters.ColumnDelimiter
                                );
                            // Shared merged-results dictionary, one entry per LoaderBracket
                            Dictionary<LoaderBracket, Shell> finalShells = [];
                            object mergeLock = new();

                            // Capture scoring criteria for use inside parallel section
                            DamageType damageType = testParameters.DamageType;
                            TestType testType = testParameters.TestType;


                            float ScoreOf(Shell s)
                            {
                                float score;
                                if (testParameters.LimitBarrelLength && !testParameters.BarrelLengthHardLimit)
                                {
                                    score = testType == TestType.DpsPerCost
                                        ? s.DpsPerCostPerAreaDict[damageType]
                                        : s.DpsPerVolumePerAreaDict[damageType];
                                }
                                else
                                {
                                    score = testType == TestType.DpsPerCost
                                        ? s.DpsPerCostDict[damageType]
                                        : s.DpsPerVolumeDict[damageType];
                                }
                                return score;
                            }


                            Parallel.For(
                                testParameters.MinGauge,
                                testParameters.MaxGauge + 1,
                                options,
                                localInit: () => new Dictionary<LoaderBracket, Shell>(),
                                body: (gauge, _, threadLocal) =>
                                {
                                    ShellCalc calcLocal = MakeShellCalc(gauge, Shell.CalculateGaugeMultiplier(gauge));
                                    if (testParameters.UseBruteForce)
                                    {
                                        calcLocal.ShellTest();
                                    }
                                    else
                                    {
                                        calcLocal.BigTest();
                                    }

                                    foreach (var (bracket, shell) in calcLocal.TopShells)
                                    {
                                        // Skip brackets where BigTest never found a feasible config
                                        if (shell.DpsDict[damageType] <= 0) continue;

                                        if (!threadLocal.TryGetValue(bracket, out Shell current)
                                            || ScoreOf(shell) > ScoreOf(current))
                                        {
                                            threadLocal[bracket] = shell;
                                        }
                                    }
                                    return threadLocal;
                                },
                                localFinally: threadLocal =>
                                {
                                    lock (mergeLock)
                                    {
                                        foreach (var (bracket, shell) in threadLocal)
                                        {
                                            if (!finalShells.TryGetValue(bracket, out Shell current)
                                                || ScoreOf(shell) > ScoreOf(current))
                                            {
                                                finalShells[bracket] = shell;
                                            }
                                        }
                                    }
                                });

                            // Build output host. Gauge values don't matter; WriteTopShells
                            // reads only TopShells, populated below from merged finalShells.
                            ShellCalc calcFinal = MakeShellCalc(0, 0);
                            foreach (var (bracket, winner) in finalShells)
                            {
                                calcFinal.TopShells[bracket] = winner;
                            }

                            stopwatch.Stop();
                            TimeSpan ts = stopwatch.Elapsed;
                            calcFinal.WriteTopShells(testParameters.MinGauge, testParameters.MaxGauge, ts, testParameters.UseBruteForce);
                        }
                        testsInQueue -= 1;
                        TestsInQueueLabel.Text = "Tests Remaining: " + testsInQueue.ToString();
                    }
                }
                finally
                {
                    // Unlock buttons and update queue count
                    parameterList.Clear();
                    testsInQueue = 0;
                    TestsInQueueLabel.Text = "Tests in Queue: " + testsInQueue.ToString();

                    AddParametersButton.Enabled = true;
                    RunButton.Enabled = true;
                    RunButton.Text = "Run Queued Tests";
                }
            }
        }
    }
}
