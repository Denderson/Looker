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

namespace Looker.Regions
{
    internal class LMisc
    {

        public static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            orig(self, add);
            if (Plugin.CheckMechanics(self.room, "wrecks", "WRRA"))
            {
                self.InjectPoison(OptionsMenu.halvedPoison.Value ? 0.10f : 0.20f, new Color(0.8f, 0f, 0f));
            }
        }

        public static void Frog_Attach(On.Watcher.Frog.orig_Attach orig, Frog self, BodyChunk chunk, bool suckFood)
        {
            if (Plugin.CheckMechanics(self.room, "wrecks", "WRRA") && !OptionsMenu.noFrogStacking.Value && chunk.owner is Creature && (chunk.owner as Creature).grabbedBy.Count > 0)
            {
                foreach (Creature.Grasp grasp in (chunk.owner as Creature).grabbedBy)
                {
                    if (grasp.grabber is Frog)
                    {
                        var room = self.room;
                        var pos = self.mainBodyChunk.pos;
                        var color = self.ShortCutColor();
                        room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                        room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
                        room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                        room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                        room.ScreenMovement(pos, default, 1.3f);
                        orig(self, chunk, suckFood);
                        return;
                    }
                }
            }
            orig(self, chunk, suckFood);
        }

        public static void StrikeAOE_ctor(On.Watcher.LightningMaker.StrikeAOE.orig_ctor orig, LightningMaker.StrikeAOE self, Vector2 pos, float effectRadius, float killRadius, PhysicalObject[] targets, Room room, Color col)
        {
            if (room.game.StoryCharacter == LookerEnums.looker && !OptionsMenu.smallerLightnings.Value)
            {
                orig(self, pos, effectRadius * 2, killRadius * 2, targets, room, col);
                return;
            }
            orig(self, pos, effectRadius, killRadius, targets, room, col);
        }

        public static void Angler_Update(On.Watcher.Angler.orig_Update orig, Angler self, bool eu)
        {
            orig(self, eu);
            if (CheckMechanics(self?.room, "salination", "WARB"))
            {
                if (self?.lightSource?.pos != null && self.Submersion > 0.9f)
                {
                    Bubble bubble = new(self.lightSource.pos + Custom.RNV() * UnityEngine.Random.value * 6f, Custom.RNV() * 1.5f * Mathf.Lerp(6f, 16f, UnityEngine.Random.value) * Mathf.InverseLerp(0f, 0.45f, 0.5f), bottomBubble: false, fakeWaterBubble: false);
                    self.room.AddObject(bubble);
                    bubble.age = 600 - UnityEngine.Random.Range(20, UnityEngine.Random.Range(30, 80));
                    for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                    {
                        if (self.room.abstractRoom.creatures[i].rippleLayer != 0)
                        {
                            continue;
                        }
                        if (self.room.abstractRoom.creatures[i].realizedCreature is Player && Custom.DistLess(self.lightSource.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, 130f * OptionsMenu.breathZoneSize.Value))
                        {
                            (self.room.abstractRoom.creatures[i].realizedCreature as Player).airInLungs = 1f;
                        }
                    }
                }
            }
        }

        public static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            if (!CWTs.PlayerCWT.TryGetData(self, out var data))
            {
                orig(self); 
                return;
            }
            if (self?.SlugCatClass == LookerEnums.looker && data.fakingDeath > 0)
            {
                self.stun = 0;
                orig(self);
                self.stun = 15;
                self.input[0].x = 0;
                self.input[0].y = 0;
                self.input[0].analogueDir *= 0f;
                self.input[0].jmp = false;
                self.input[0].thrw = false;
                self.input[0].pckp = false;
            }
            else orig(self);

            if (Plugin.CheckMechanics(self.room, "fetid", "WARC") && self.mushroomEffect <= 0)
            {
                if (data.reverseHorizontal)
                {
                    self.input[0].x *= -1;
                    self.input[0].analogueDir.x *= -1;
                }
                if (data.reverseVertical)
                {
                    self.input[0].y *= -1;
                    self.input[0].analogueDir.y *= -1;
                }
                switch (data.controlOffset % 3)
                {
                    case 0:
                        {
                            (self.input[0].thrw, self.input[0].pckp) = (self.input[0].pckp, self.input[0].thrw);
                            break;
                        }
                    case 1:
                        {
                            (self.input[0].jmp, self.input[0].pckp) = (self.input[0].pckp, self.input[0].jmp);
                            break;
                        }
                    case 2:
                        {
                            (self.input[0].jmp, self.input[0].thrw) = (self.input[0].thrw, self.input[0].jmp);
                            break;
                        }
                }
            }
        }
        public static Color BoxWormGraphics_BaseColor_Room(On.Watcher.BoxWormGraphics.orig_BaseColor_Room orig, Room room)
        {
            if (room.game.IsStorySession && room.world.game.GetStorySession.characterStats.name == LookerEnums.looker && room.world.name == "WTDA")
            {
                return BoxWormColor;
            }
            return orig(room);
        }

        public static Color BoxWormGraphics_BaseColor_AbstractRoom(On.Watcher.BoxWormGraphics.orig_BaseColor_AbstractRoom orig, AbstractRoom room)
        {
            if (room.world.game.IsStorySession && room.world.game.GetStorySession.characterStats.name == LookerEnums.looker && room.world.name == "WTDA")
            {
                return BoxWormColor;
            }
            return orig(room);
        }

        public static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {
            orig(self);
            if (self?.world?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (self.world.region.name == "WRRA")
                {
                    if (self.TimeUntilRain <= 200)
                    {
                        self.timer--;
                    }
                }
                if (self.world.region.name == "WARA")
                {
                    if (self.TimeUntilRain <= 600)
                    {
                        self.timer--;
                    }
                }
            }
        }

        public static bool DaddyCorruption_SentientRotMode(On.DaddyCorruption.orig_SentientRotMode orig, Room rm)
        {
            return orig(rm) || (rm.world?.game?.StoryCharacter == LookerEnums.looker);
        }

        public static string WarpPoint_ChooseDynamicWarpTarget(On.Watcher.WarpPoint.orig_ChooseDynamicWarpTarget orig, World world, string oldRoom, string targetRegion, bool badWarp, bool spreadingRot, bool playerCreated)
        {
            if (world.game?.StoryCharacter == LookerEnums.looker && playerCreated)
            {
                return "wrsa_l01";
            }
            return orig(world, oldRoom, targetRegion, badWarp, spreadingRot, playerCreated);
        }

        public static string SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            if (self.saveStateNumber == LookerEnums.looker)
            {
                self.respawnCreatures = new List<int> { };
                self.waitRespawnCreatures = new List<int> { };
            }
            return orig(self);
        }

        public static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            if (self?.game?.StoryCharacter == LookerEnums.looker)
            {
                foreach (AbstractRoom abstractRoom in self.abstractRooms)
                {
                    if (!abstractRoom.shelter)
                    {
                        abstractRoom.creatures.Clear();
                        abstractRoom.entitiesInDens.Clear();
                        continue;
                    }
                    for (int num3 = abstractRoom.creatures.Count - 1; num3 >= 0; num3--)
                    {
                        if (!AbstractPhysicalObject.IsObjectImportant(abstractRoom.creatures[num3], self.world))
                        {
                            abstractRoom.creatures.RemoveAt(num3);
                        }
                    }
                    abstractRoom.entitiesInDens.Clear();
                }
                orig(self, true);
                return;
            }
            orig(self, fresh);
        }

        public static void SingularityBomb_ctor(On.MoreSlugcats.SingularityBomb.orig_ctor orig, SingularityBomb self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            if (world?.game?.StoryCharacter == LookerEnums.looker)
            {
                self.zeroMode = true;
            }
            orig(self, abstractPhysicalObject, world);
        }

        public static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self?.game?.StoryCharacter != LookerEnums.looker)
            {
                return;
            }
            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                if (self.roomSettings.placedObjects[i].type == WatcherEnums.PlacedObjectType.SpinningTopSpot)
                {
                    SpinningTop.SpawnBackupWarpPoint(self, self.roomSettings.placedObjects[i]);
                }
            }

        }

        public static void Room_InitializeSentientRotPresenceInRoom(On.Room.orig_InitializeSentientRotPresenceInRoom orig, Room self, float amount)
        {
            bool flag = Custom.rainWorld.progression.miscProgressionData.beaten_Watcher_SentientRot;
            if (self.game.IsStorySession && self.game.StoryCharacter == LookerEnums.looker)
            {
                Custom.rainWorld.progression.miscProgressionData.beaten_Watcher_SentientRot = false;
            }
            orig(self, amount);
            Custom.rainWorld.progression.miscProgressionData.beaten_Watcher_SentientRot = flag;
        }

        public static bool PrinceFilterData_Active(On.PlacedObject.PrinceFilterData.orig_Active orig, PlacedObject.PrinceFilterData self, RoomSettings roomSettings, SlugcatStats.Timeline timelinePoint)
        {
            if (roomSettings.game == null || !roomSettings.game.IsStorySession || roomSettings.game.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self, roomSettings, timelinePoint);
        }
    }
}
