using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Looker.Plugin;
using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Music;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using Watcher;
using static SlugBase.Features.FeatureTypes;
using Looker.CWTs;

namespace Looker.Regions
{
    public static class LSignal
    {
        public static void SignalGameOver(VultureGrub self)
        {
            if (!CheckMechanics(self.room, "signal", "WPTA")) return;
            if (OptionsMenu.noSkyWhales.Value) return;
            foreach (AbstractCreature player in self.room.game.AlivePlayers)
            {
                if (player.realizedCreature != null && player.realizedCreature is Player)
                {
                    if (OptionsMenu.weakerBroadcast.Value)
                    {
                        player.realizedCreature.Stun(300);
                        player.realizedCreature.room.AddObject(new ExplosionSpikes(player.realizedCreature.room, player.realizedCreature.firstChunk.pos, 14, 30f, 12f, 7f, 170f, Color.black));
                        if (PlayerCWT.TryGetData(player.realizedCreature as Player, out var data))
                        {
                            data.signalLeniency = Plugin.MaxSignalLeniency() + 300;
                            AbstractCreature abstractCreature = new(self.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.VultureGrub), null, self.abstractCreature.pos, self.room.game.GetNewID())
                            {
                                saveCreature = false
                            };
                            self.room.abstractRoom.AddEntity(abstractCreature);
                            abstractCreature.RealizeInRoom();
                        }
                    }
                    else
                    {
                        player.Die();
                        AbstractPhysicalObject abstractPhysicalObject = new(player.Room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, player.realizedCreature.room.GetWorldCoordinate(player.realizedCreature.mainBodyChunk.pos), player.Room.world.game.GetNewID());
                        player.Room.AddEntity(abstractPhysicalObject);
                        abstractPhysicalObject.RealizeInRoom();
                        (abstractPhysicalObject.realizedObject as SingularityBomb).Explode();
                    }
                }
            }
            self.room.AddObject(new ExplosionSpikes(self.room, self.firstChunk.pos, 14, 30f, 12f, 7f, 170f, Color.black));
            self.Destroy();
        }

        public static void VultureGrub_Violence(On.VultureGrub.orig_Violence orig, VultureGrub self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if (Plugin.CheckMechanics(self.room, "signal", "WPTA"))
            {
                SignalGameOver(self);
            }
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        public static void VultureGrub_AttemptCallVulture(On.VultureGrub.orig_AttemptCallVulture orig, VultureGrub self)
        {
            if (CheckMechanics(self.room, "signal", "WPTA"))
            {
                //SignalGameOver(self);
                return;
            }
            else orig(self);
        }

        public static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            PhysicalObject grabbed = self.grasps[grasp].grabbed;
            if (grabbed is VultureGrub && self?.room?.game?.StoryCharacter == LookerEnums.looker && CWTs.PlayerCWT.TryGetData(self, out var data))
            {
                data.signalLeniency = (int)(400 * OptionsMenu.broadcastingLeniencyTimer.Value);
            }
            orig(self, grasp, eu);
        }

        public static void VultureGrub_Act(On.VultureGrub.orig_Act orig, VultureGrub self)
        {
            if (self.singalCounter > 0 && self.singalCounter < 10 && Plugin.CheckMechanics(self.room, "signal", "WPTA"))
            {
                SignalGameOver(self);
            }
            orig(self);
        }
    }
}
