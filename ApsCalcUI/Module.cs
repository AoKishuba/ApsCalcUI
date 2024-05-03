using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsCalcUI
{
    /// <summary>
    /// Stores module information
    /// </summary>
    public class Module
    {

        // Module positions.  Enum is faster than strings.
        public enum Position : int
        {
            Base,
            Middle,
            Head
        }

        public string Name { get; }
        public float VelocityMod { get; }
        public float KineticDamageMod { get; }
        public float ArmorPierceMod { get; }
        public float ChemMod { get; }
        public float InaccuracyMod { get; }
        public float MaxLength { get; }
        public Position ModulePosition { get; }
        public bool CanBeVariable { get; }

        // Initialize every unique module type
        public static Module SolidBody { get; } = new Module("Solid body", 1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module SabotBody { get; } = new Module("Sabot body", 1.1f, 0.8f, 1.4f, 0.25f, 1.0f, 1000f, Position.Middle, true);
        public static Module EmpBody { get; } = new Module("EMP body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module FlakBody { get; } = new Module("Flak body", 1.0f, 1.0f, 0.1f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module FragBody { get; } = new Module("Frag body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module HEBody { get; } = new Module("HE body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module FinBody { get; } = new Module("Stabilizer fin body", 0.95f, 1.0f, 1.0f, 1.0f, 0.2f, 300f, Position.Middle, true);
        public static Module SmokeBody { get; } = new("Smoke body", 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 1000f, Position.Middle, true);
        public static Module PenDepthFuse { get; } = new Module("Pendepth fuse", 1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module TimedFuse { get; } = new Module("Timed fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module InertialFuse { get; } = new Module("Inertial fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module AltitudeFuse { get; } = new Module("Altitude fuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module Defuse { get; } = new Module("Emergency defuse", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module GravCompensator { get; } = new Module("Grav. compensator", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Middle, false);
        public static Module EmpHead { get; } = new Module("EMP head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module FlakHead { get; } = new Module("Flak head", 1.45f, 1.0f, 0.1f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module FragHead { get; } = new Module("Frag head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module HEHead { get; } = new Module("HE head", 1.45f, 1.2f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module ShapedChargeHead { get; } = new Module("Shaped charge head", 1.6f, 0.1f, 0.1f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module APHead { get; } = new Module("AP head", 1.6f, 1.0f, 1.65f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module SabotHead { get; } = new Module("Sabot head", 1.6f, 0.85f, 2.5f, 0.25f, 1.0f, 1000f, Position.Head, false);
        public static Module HeavyHead { get; } = new Module("Heavy head", 1.45f, 1.75f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module HollowPoint { get; } = new Module("Hollow point head", 1.45f, 1.0f, 1.2f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module SkimmerTip { get; } = new Module("Skimmer tip", 1.6f, 1.0f, 1.4f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module Disruptor { get; } = new Module("Disruptor conduit", 1.6f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Head, false);
        public static Module BaseBleeder { get; } = new Module("Base bleeder", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Base, false);
        public static Module Supercav { get; } = new Module("Supercavitation base", 1.0f, 1.0f, 1.0f, 0.75f, 1.0f, 100f, Position.Base, false);
        public static Module Tracer { get; } = new Module("Visible tracer", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 100f, Position.Base, false);
        public static Module GravRam { get; } = new Module("Graviton ram", 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1000f, Position.Base, false);

        // List modules for reference
        public static Module[] AllModules { get; } =
        {
        SolidBody,
        SabotBody,
        EmpBody,
        FlakBody,
        FragBody,
        HEBody,
        FinBody,
        SmokeBody,
        PenDepthFuse,
        TimedFuse,
        InertialFuse,
        AltitudeFuse,
        Defuse,
        GravCompensator,
        EmpHead,
        FlakHead,
        FragHead,
        HEHead,
        ShapedChargeHead,
        APHead,
        SabotHead,
        HeavyHead,
        HollowPoint,
        SkimmerTip,
        Disruptor,
        BaseBleeder,
        Supercav,
        Tracer,
        GravRam
        };

        /// <param name="name">Module name</param>
        /// <param name="vMod">Velocity modifier</param>
        /// <param name="kdMod">Kinetic damage modifier</param>
        /// <param name="apMod">Armor pierce modifier</param>
        /// <param name="cMod">Chemical payload modifier</param>
        /// <param name="mLength">Max length of module in mm (length equals gauge at or below this value)</param>
        /// <param name="mType">Type of module - base, middle, or head</param>
        /// <param name="canBeVariable">Whether module can be a Variable Module</param>
        public Module(string name, float vMod, float kdMod, float apMod, float cMod, float inaccMod, float mLength, Position mType, bool canBeVariable)
        {
            Name = name;
            VelocityMod = vMod;
            KineticDamageMod = kdMod;
            ArmorPierceMod = apMod;
            ChemMod = cMod;
            InaccuracyMod = inaccMod;
            MaxLength = mLength;
            ModulePosition = mType;
            CanBeVariable = canBeVariable;
        }
    }
}
