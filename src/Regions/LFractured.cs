using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Looker.Plugin;

namespace Looker.Regions
{
    public static class LFractured
    {
        public static void SkyWhale_ctor(On.SkyWhale.orig_ctor orig, SkyWhale self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (CheckMechanics(abstractCreature?.Room, "fractured", "WVWB"))
            {
                self.creatureParams.idleSpeedFactor *= 3;
                self.creatureParams.fleeSpeedFactor *= 3;
                self.creatureParams.huntSpeedFactor *= 3;
                self.creatureParams.travelSpeedFactor *= 3;
                self.creatureParams.stunOnCollideVelocity = 1f;
                self.creatureParams.stunOnCollideDamage = 2f;
                self.creatureParams.stunOnCollideDuration = 2f;
            }
        }

        public static void SkyWhale_Update(On.SkyWhale.orig_Update orig, SkyWhale self, bool eu)
        {
            orig(self, eu);
            if (CheckMechanics(self?.abstractCreature?.Room, "fractured", "WVWB") && self.IsTileSolid(1, 0, 0))
            {
                self.Die();
                self.Destroy();
            }
        }

        public static void SkyWhale_Collide(On.SkyWhale.orig_Collide orig, SkyWhale self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            // If vanilla traffic incident code doesnt work, make the whale spawn an explosion on high velocity collisionb
        }
    }
}
