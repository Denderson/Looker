using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Looker.CWTs
{
    public static class PlayerCWT
    {

        public static readonly ConditionalWeakTable<Player, DataClass> playerCWT = new();
        public static bool TryGetData(Player key, out DataClass data)
        {
            if (key != null)
            {
                data = playerCWT.GetOrCreateValue(key);
            }
            else data = null;

            return data != null;
        }
        public class DataClass
        {
            public int fakingDeath = -1;

            public int floatRemaining = 0;
            public int timeInFloat = 0;
            public bool currentlyFloating = false;

            public bool inShelter = true;

            public bool reverseHorizontal = false;
            public bool reverseVertical = false;
            public int controlOffset = -1;

            public bool startingRipple = false;
            public int rippleTimer = -1;

            public int signalLeniency = -1;

            public int darknessImmunity = -1;

            public bool karmaMode = false;
            public bool previousKarmaMode = false;

            public bool usedEmergencyBreath = false;

            public int timeUntilChaser = -1;
            public RWCustom.IntVector2 chaserpos = new();
            public VoidSpawn chaser = null;
            public string oldChaserRoom = string.Empty;

            public bool shouldOverrideGravity = false;

            public float overrideGravity = 0;
            public float overrideAirfriction = 0;

            public int timesUntilTargetedByLightning = 3;

            public string oldRoom = string.Empty;
            public string oldRegion = string.Empty;
        }
    }
}
