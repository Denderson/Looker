using Looker.CWTs;
using Mono.Cecil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;
using static Looker.Plugin;

namespace Looker.Regions
{
    public static class LMigration
    {
        public static void Player_RippleSpawnInteractions(On.Player.orig_RippleSpawnInteractions orig, Player self)
        {
            orig(self);
            if (self != null && self.warpPointCooldown <= 0 && self.standingInWarpPointProtectionTime <= 0 && self.room != null)
            {
                if (self?.room?.game?.StoryCharacter == LookerEnums.looker && PlayerCWT.TryGetData(self, out var data) && data.chaser != null && data.chaser.room == self.room)
                {
                    if (Vector2.Distance(self.mainBodyChunk.pos, data.chaser.firstChunk.pos) <= 50f && data.chaser.abstractPhysicalObject.rippleLayer == self.abstractCreature.rippleLayer)
                    {
                        self.rippleDeathIntensity += 0.015f;
                    }
                }
            }
        }

        public static void SpawnChaser(this Player self)
        {
            if (self?.room?.world == null)
            {
                Log.LogMessage("Couldnt find player / room / world!");
                return;
            }
            if (!CWTs.PlayerCWT.TryGetData(self, out var data))
            {
                Log.LogMessage("Cannot find PlayerCWT!");
                return;
            }
            if (data.chaserpos == null || data.chaserpos == new IntVector2())
            {
                Log.LogMessage("Cannot find chaserpos!");
                return;
            }

            if (data.chaser != null)
            {
                if (data.chaser.room != null && data.chaser.room == self.room)
                {
                    return;
                }
                else
                {
                    data.chaser.timeUntilFadeout = 200;
                    data.chaser = null;
                }
            }

            IntVector2 spawnPos = data.chaserpos;
            VoidSpawn voidSpawn = new(new AbstractPhysicalObject(self.room.world, WatcherEnums.AbstractObjectType.RippleSpawn, null, self.room.GetWorldCoordinate(spawnPos), SpecialId), self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidMelt), VoidSpawnKeeper.DayLightMode(self.room), VoidSpawn.SpawnType.RippleAmoeba)
            {
                sizeFac = 2
            };
            voidSpawn.behavior = new VoidSpawn.ChasePlayer(voidSpawn, self.room);
            voidSpawn.swimSpeed = 1.7f;
            voidSpawn.PlaceInRoom(self.room);

            voidSpawn.timeUntilFadeout = int.MaxValue;
            voidSpawn.ChangeRippleLayer(0, showEffect: true);

            data.chaser = voidSpawn;
        }


    }
}
