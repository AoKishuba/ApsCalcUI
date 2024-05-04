using System;
using System.Collections.Generic;
using System.Text;

namespace PenCalc
{
    public class Layer
    {
        /// <summary>
        /// An armor layer.  Stats given are for 4 m beams.
        /// </summary>
        /// <param name="name">Material name</param>
        /// <param name="hp">Hit points</param>
        /// <param name="ac">Armor class</param>
        /// <param name="givesACBonus">True if the block is structural</param>
        /// <param name="baseAngle">Impact angle from perpindicular, in °</param>
        public Layer (string name, float hp, float ac, bool givesACBonus, float baseAngle)
        {
            Name = name;
            HP = hp;
            RawAC = ac;
            ACBonus = 0.2f * RawAC;
            AC = ac; // Actual AC defaults to unbonused
            GivesACBonus = givesACBonus;
            BaseAngle = baseAngle;
        }
        public string Name { get; }
        public float HP { get; }
        public float RawAC { get; }
        public float ACBonus { get; }
        public float AC { get; set; }
        public bool GivesACBonus { get; }
        public float BaseAngle { get; }

        // Initialize all supported layers
        public static Layer Air { get; } = new Layer("Air", 0, 0, false, 0f);
        public static Layer AlloyBeam { get; } = new Layer("Alloy 4m Beam", 1440f, 35f, true, 0f);
        public static Layer HeavyBeam { get; } = new Layer("HA 4m Beam", 6000f, 60f, true, 0f);
        public static Layer MetalBeam { get; } = new Layer("Metal 4m Beam", 1680f, 40f, true, 0f);
        public static Layer StoneBeam { get; } = new Layer("Stone 4m Beam", 1200f, 16f, true, 0f);
        public static Layer WoodBeam { get; } = new Layer("Wood 4m Beam", 864f, 8f, true, 0f);

        public static Layer AlloyBeamSlope { get; } = new Layer("Alloy Beam Slope", 720f, 35f, false, 45f);
        public static Layer HeavyBeamSlope { get; } = new Layer("HA Beam Slope", 3000f, 60f, false, 45f);
        public static Layer MetalBeamSlope { get; } = new Layer("Metal Beam Slope", 840f, 40f, false, 45f);
        public static Layer StoneBeamSlope { get; } = new Layer("Stone Beam Slope", 600f, 16f, false, 45f);
        public static Layer WoodBeamSlope { get; } = new Layer("Wood Beam Slope", 432f, 8f, false, 45f);

        public static Layer Alloy2mSlopeSteep { get; } = new Layer("Alloy 2m Slope (steep)", 330f, 35f, false, 26.56505f);
        public static Layer Heavy2mSlopeSteep { get; } = new Layer("HA 2m Slope (steep)", 1375f, 60f, false, 26.56505f);
        public static Layer Metal2mSlopeSteep { get; } = new Layer("Metal 2m Slope (steep)", 385f, 40f, false, 26.56505f);
        public static Layer Stone2mSlopeSteep { get; } = new Layer("Stone 2m Slope (steep)", 275f, 16f, false, 26.56505f);
        public static Layer Wood2mSlopeSteep { get; } = new Layer("Wood 2m Slope (steep)", 198f, 8f, false, 26.56505f);

        public static Layer Alloy2mSlopeShallow { get; } = new Layer("Alloy 2m Slope (shallow)", 330f, 35f, false, 63.43495f);
        public static Layer Heavy2mSlopeShallow { get; } = new Layer("HA 2m Slope (shallow)", 1375f, 60f, false, 63.43495f);
        public static Layer Metal2mSlopeShallow { get; } = new Layer("Metal 2m Slope (shallow)", 385f, 40f, false, 63.43495f);
        public static Layer Stone2mSlopeShallow { get; } = new Layer("Stone 2m Slope (shallow)", 275f, 16f, false, 63.43495f);
        public static Layer Wood2mSlopeShallow { get; } = new Layer("Wood 2m Slope (shallow)", 198f, 8f, false, 63.43495f);

        public static Layer Alloy3mSlopeSteep { get; } = new Layer("Alloy 3m Slope (steep)", 517.5f, 35f, false, 18.43495f);
        public static Layer Heavy3mSlopeSteep { get; } = new Layer("HA 3m Slope (steep)", 2156.2f, 60f, false, 18.43495f);
        public static Layer Metal3mSlopeSteep { get; } = new Layer("Metal 3m Slope (steep)", 603.8f, 40f, false, 18.43495f);
        public static Layer Stone3mSlopeSteep { get; } = new Layer("Stone 3m Slope (steep)", 431.2f, 16f, false, 18.43495f);
        public static Layer Wood3mSlopeSteep { get; } = new Layer("Wood 3m Slope (steep)", 310.5f, 8f, false, 18.43495f);

        public static Layer Alloy3mSlopeShallow { get; } = new Layer("Alloy 3m Slope (shallow)", 517.5f, 35f, false, 71.56505f);
        public static Layer Heavy3mSlopeShallow { get; } = new Layer("HA 3m Slope (shallow)", 2156.2f, 60f, false, 71.56505f);
        public static Layer Metal3mSlopeShallow { get; } = new Layer("Metal 3m Slope (shallow)", 603.8f, 40f, false, 71.56505f);
        public static Layer Stone3mSlopeShallow { get; } = new Layer("Stone 3m Slope (shallow)", 431.2f, 16f, false, 71.56505f);
        public static Layer Wood3mSlopeShallow { get; } = new Layer("Wood 3m Slope (shallow)", 310.5f, 8f, false, 71.56505f);

        public static Layer Alloy4mSlopeSteep { get; } = new Layer("Alloy 4m Slope (steep)", 720f, 35f, false, 14.03624f);
        public static Layer Heavy4mSlopeSteep { get; } = new Layer("HA 4m Slope (steep)", 3000f, 60f, false, 14.03624f);
        public static Layer Metal4mSlopeSteep { get; } = new Layer("Metal 4m Slope (steep)", 840f, 40f, false, 14.03624f);
        public static Layer Stone4mSlopeSteep { get; } = new Layer("Stone 4m Slope (steep)", 600f, 16f, false, 14.03624f);
        public static Layer Wood4mSlopeSteep { get; } = new Layer("Wood 4m Slope (steep)", 432f, 8f, false, 14.03624f);

        public static Layer Alloy4mSlopeShallow { get; } = new Layer("Alloy 4m Slope (shallow)", 720f, 35f, false, 75.96376f);
        public static Layer Heavy4mSlopeShallow { get; } = new Layer("HA 4m Slope (shallow)", 3000f, 60f, false, 75.96376f);
        public static Layer Metal4mSlopeShallow { get; } = new Layer("Metal 4m Slope (shallow)", 840f, 40f, false, 75.96376f);
        public static Layer Stone4mSlopeShallow { get; } = new Layer("Stone 4m Slope (shallow)", 600f, 16f, false, 75.96376f);
        public static Layer Wood4mSlopeShallow { get; } = new Layer("Wood 4m Slope (shallow)", 432f, 8f, false, 75.96376f);

        public static Layer AlloyWedgeSteep { get; } = new Layer("Alloy Wedge (steep)", 150f, 35f, false, 26.56505f);
        public static Layer HeavyWedgeSteep { get; } = new Layer("HA Wedge (steep)", 625f, 60f, false, 26.56505f);
        public static Layer MetalWedgeSteep { get; } = new Layer("Metal Wedge (steep)", 175f, 40f, false, 26.56505f);
        public static Layer StoneWedgeSteep { get; } = new Layer("Stone Wedge (steep)", 125f, 16f, false, 26.56505f);
        public static Layer WoodWedgeSteep { get; } = new Layer("Wood Wedge (steep)", 90f, 8f, false, 26.56505f);

        public static Layer AlloyWedgeShallow { get; } = new Layer("Alloy Wedge (shallow)", 150f, 35f, false, 63.43495f);
        public static Layer HeavyWedgeShallow { get; } = new Layer("HA Wedge (shallow)", 625f, 60f, false, 63.43495f);
        public static Layer MetalWedgeShallow { get; } = new Layer("Metal Wedge (shallow)", 175f, 40f, false, 63.43495f);
        public static Layer StoneWedgeShallow { get; } = new Layer("Stone Wedge (shallow)", 125f, 16f, false, 63.43495f);
        public static Layer WoodWedgeShallow { get; } = new Layer("Wood Wedge (shallow)", 90f, 8f, false, 63.43495f);

        public static Layer Alloy2mWedgeSteep { get; } = new Layer("Alloy 2m Wedge (steep)", 330f, 35f, false, 14.03624f);
        public static Layer Heavy2mWedgeSteep { get; } = new Layer("HA 2m Wedge (steep)", 1375f, 60f, false, 14.03624f);
        public static Layer Metal2mWedgeSteep { get; } = new Layer("Metal 2m Wedge (steep)", 385f, 40f, false, 14.03624f);
        public static Layer Stone2mWedgeSteep { get; } = new Layer("Stone 2m Wedge (steep)", 275f, 16f, false, 14.03624f);
        public static Layer Wood2mWedgeSteep { get; } = new Layer("Wood 2m Wedge (steep)", 198f, 8f, false, 14.03624f);

        public static Layer Alloy2mWedgeShallow { get; } = new Layer("Alloy 2m Wedge (shallow)", 330f, 35f, false, 75.96376f);
        public static Layer Heavy2mWedgeShallow { get; } = new Layer("HA 2m Wedge (shallow)", 1375f, 60f, false, 75.96376f);
        public static Layer Metal2mWedgeShallow { get; } = new Layer("Metal 2m Wedge (shallow)", 385f, 40f, false, 75.96376f);
        public static Layer Stone2mWedgeShallow { get; } = new Layer("Stone 2m Wedge (shallow)", 275f, 16f, false, 75.96376f);
        public static Layer Wood2mWedgeShallow { get; } = new Layer("Wood 2m Wedge (shallow)", 198f, 8f, false, 75.96376f);

        public static Layer Alloy3mWedgeSteep { get; } = new Layer("Alloy 3m Wedge (steep)", 517.5f, 35f, false, 9.46232f);
        public static Layer Heavy3mWedgeSteep { get; } = new Layer("HA 3m Wedge (steep)", 2156.2f, 60f, false, 9.46232f);
        public static Layer Metal3mWedgeSteep { get; } = new Layer("Metal 3m Wedge (steep)", 603.8f, 40f, false, 9.46232f);
        public static Layer Stone3mWedgeSteep { get; } = new Layer("Stone 3m Wedge (steep)", 431.2f, 16f, false, 9.46232f);
        public static Layer Wood3mWedgeSteep { get; } = new Layer("Wood 3m Wedge (steep)", 310.5f, 8f, false, 9.46232f);

        public static Layer Alloy3mWedgeShallow { get; } = new Layer("Alloy 3m Wedge (shallow)", 517.5f, 35f, false, 80.53768f);
        public static Layer Heavy3mWedgeShallow { get; } = new Layer("HA 3m Wedge (shallow)", 2156.2f, 60f, false, 80.53768f);
        public static Layer Metal3mWedgeShallow { get; } = new Layer("Metal 3m Wedge (shallow)", 603.8f, 40f, false, 80.53768f);
        public static Layer Stone3mWedgeShallow { get; } = new Layer("Stone 3m Wedge (shallow)", 431.2f, 16f, false, 80.53768f);
        public static Layer Wood3mWedgeShallow { get; } = new Layer("Wood 3m Wedge (shallow)", 310.5f, 8f, false, 80.53768f);

        public static Layer Alloy4mWedgeSteep { get; } = new Layer("Alloy 4m Wedge (steep)", 720f, 35f, false, 7.12502f);
        public static Layer Heavy4mWedgeSteep { get; } = new Layer("HA 4m Wedge (steep)", 3000f, 60f, false, 7.12502f);
        public static Layer Metal4mWedgeSteep { get; } = new Layer("Metal 4m Wedge (steep)", 840f, 40f, false, 7.12502f);
        public static Layer Stone4mWedgeSteep { get; } = new Layer("Stone 4m Wedge (steep)", 600f, 16f, false, 7.12502f);
        public static Layer Wood4mWedgeSteep { get; } = new Layer("Wood 4m Wedge (steep)", 432f, 8f, false, 7.12502f);

        public static Layer Alloy4mWedgeShallow { get; } = new Layer("Alloy 4m Wedge (shallow)", 720f, 35f, false, 82.87498f);
        public static Layer Heavy4mWedgeShallow { get; } = new Layer("HA 4m Wedge (shallow)", 3000f, 60f, false, 82.87498f);
        public static Layer Metal4mWedgeShallow { get; } = new Layer("Metal 4m Wedge (shallow)", 840f, 40f, false, 82.87498f);
        public static Layer Stone4mWedgeShallow { get; } = new Layer("Stone 4m Wedge (shallow)", 600f, 16f, false, 82.87498f);
        public static Layer Wood4mWedgeShallow { get; } = new Layer("Wood 4m Wedge (shallow)", 432f, 8f, false, 82.87498f);


        // List all supported layers
        public static Layer[] AllLayers { get; } =
        {
            Air,
            AlloyBeam,
            AlloyBeamSlope,
            Alloy2mSlopeSteep,
            Alloy2mSlopeShallow,
            Alloy3mSlopeSteep,
            Alloy3mSlopeShallow,
            Alloy4mSlopeSteep,
            Alloy4mSlopeShallow,
            AlloyWedgeSteep,
            AlloyWedgeShallow,
            Alloy2mWedgeSteep,
            Alloy2mWedgeShallow,
            Alloy3mWedgeSteep,
            Alloy3mWedgeShallow,
            Alloy4mWedgeSteep,
            Alloy4mWedgeShallow,
            HeavyBeam,
            HeavyBeamSlope,
            Heavy2mSlopeSteep,
            Heavy2mSlopeShallow,
            Heavy3mSlopeSteep,
            Heavy3mSlopeShallow,
            Heavy4mSlopeSteep,
            Heavy4mSlopeShallow,
            HeavyWedgeSteep,
            HeavyWedgeShallow,
            Heavy2mWedgeSteep,
            Heavy2mWedgeShallow,
            Heavy3mSlopeSteep,
            Heavy3mSlopeShallow,
            Heavy4mWedgeSteep,
            Heavy4mWedgeShallow,
            MetalBeam,
            MetalBeamSlope,
            Metal2mSlopeSteep,
            Metal2mSlopeShallow,
            Metal3mSlopeSteep,
            Metal3mSlopeShallow,
            Metal4mSlopeSteep,
            Metal4mSlopeShallow,
            MetalWedgeSteep,
            MetalWedgeShallow,
            Metal2mWedgeSteep,
            Metal2mWedgeShallow,
            Metal3mWedgeSteep,
            Metal3mWedgeShallow,
            Metal4mWedgeSteep,
            Metal4mWedgeShallow,
            StoneBeam,
            StoneBeamSlope,
            Stone2mSlopeSteep,
            Stone2mSlopeShallow,
            Stone3mSlopeSteep,
            Stone3mSlopeShallow,
            Stone4mSlopeSteep,
            Stone4mSlopeShallow,
            StoneWedgeSteep,
            StoneWedgeShallow,
            Stone2mWedgeSteep,
            Stone2mWedgeShallow,
            Stone3mWedgeSteep,
            Stone3mWedgeShallow,
            Stone4mWedgeSteep,
            Stone4mWedgeShallow,
            WoodBeam,
            WoodBeamSlope,
            Wood2mSlopeSteep,
            Wood2mSlopeShallow,
            Wood3mSlopeSteep,
            Wood3mSlopeShallow,
            Wood4mSlopeSteep,
            Wood4mSlopeShallow,
            WoodWedgeSteep,
            WoodWedgeShallow,
            Wood2mWedgeSteep,
            Wood2mWedgeShallow,
            Wood3mWedgeSteep,
            Wood3mWedgeShallow,
            Wood4mWedgeSteep,
            Wood4mWedgeShallow,
        };
    }
}
