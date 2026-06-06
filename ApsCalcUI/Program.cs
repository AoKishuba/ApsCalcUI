using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ApsCalcUI
{
    public struct TestParameters
    {
        public int BarrelCount;
        public int MinGauge;
        public int MaxGauge;
        public List<int> HeadIndices;
        public Module BaseModule;
        public float[] FixedModulecounts;
        public float MinModuleCount;
        public int[] VariableModuleIndices;
        public int RegularClipsPerLoader;
        public int RegularInputsPerLoader;
        public int BeltfedClipsPerLoader;
        public int BeltfedInputsPerLoader;
        public bool UsesAmmoEjector;
        public float MaxGPCasingCount;
        public float CasingIncrement;
        public float MaxRGCasingCount;
        public float MinLength;
        public float MaxLength;
        public float MaxDraw;
        public float MaxRecoil;
        public float MinVelocity;
        public float MinEffectiveRange;
        public float ImpactAngle;
        public float SabotAngleMultiplier;
        public float NonSabotAngleMultiplier;
        public List<float> TargetACList;
        public DamageType DamageType;
        public float FragConeAngle;
        public float FragAngleMultiplier;
        public float MinDisruptor;
        public Scheme ArmorScheme;
        public bool Penpdepth;
        public TestType TestType;
        public bool Labels;
        public int TestInterval;
        public float StoragePerVolume;
        public float StoragePerCost;
        public float EnginePpm;
        public float EnginePpv;
        public float EnginePpc;
        public bool EngineUsesFuel;
        public bool FiringPieceIsDif;
        public bool GunUsesRecoilAbsorbers;
        public float MaxInaccuracy;
        public float RateOfFireRpm;
        public bool LimitBarrelLength;
        public float MaxBarrelLength;
        public BarrelLengthLimit BarrelLengthLimitType;
        public bool VerboseOutputIsChecked;
        public bool RawNumberOutputIsChecked;
        public char ColumnDelimiter;
        public bool UseBruteForce;
    }

    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ParameterInput());
        }
    }
}
