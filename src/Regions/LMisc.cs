using BepInEx;
using BepInEx.Logging;
using Looker.CWTs;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Music;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using Watcher;
using static Looker.Plugin;
using static SlugBase.Features.FeatureTypes;

namespace Looker.Regions
{
    public static class LMisc
    {
        // How much the player is pushed back toward the camera area
        private const float oobPushStrength = 10f;

        // How far outside the camera bounds before push kicks in
        private const float oobMargin = 0f;

        // How far into the camera bounds is the Player pushed
        private const float oobInnerMargin = 20f;

        // How long does player need to be out of bounds before the push (if set to zero, weird stuff when going through screens)
        public const int oobRequirement = 10;


        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (!PlayerCWT.TryGetData(self, out var data))
            {
                return;
            }

            if (!CheckMechanics(self?.room, "storage", "WARD"))
            {
                return;
            }

            RoomCamera camera = FindCameraForRoom(self.room);
            if (camera == null)
            {
                return;
            }

            Vector2 cameraPos = camera.pos;

            float camWidth = camera.sSize.x;
            float camHeight = camera.sSize.y;

            float left = cameraPos.x - oobMargin + 20;
            float right = cameraPos.x + camWidth + oobMargin - 20;
            float bottom = cameraPos.y - oobMargin;
            float top = cameraPos.y + camHeight + oobMargin;

            Vector2 playerPos = self.mainBodyChunk.pos;
            Vector2 pushDir = Vector2.zero;

            // Check horizontal OOB
            if (playerPos.x < left)
            {
                pushDir.x = cameraPos.x + oobInnerMargin - playerPos.x;
            }
            else if (playerPos.x > right)
            {
                pushDir.x = (cameraPos.x + camWidth - oobInnerMargin) - playerPos.x;
            }

            // Check vertical OOB
            if (playerPos.y < bottom && self.gravity == 0f)
            {
                pushDir.y = cameraPos.y + oobInnerMargin - playerPos.y;
            }
            else if (playerPos.y > top)
            {
                pushDir.y = (cameraPos.y + camHeight - oobInnerMargin) - playerPos.y;
            }

            // Check if any collision happened, dont run code afterwards if not
            if (pushDir == Vector2.zero)
            {
                data.oobTimer = 0;
                return;
            }

            if (data.oobTimer < oobRequirement)
            {
                data.oobTimer++;
                return;
            }
            data.oobTimer = 0;

            if (!OptionsMenu.nonLethalBorders.Value && !CheckEasyMode(self.room) && self.gravity == 0)
            {
                self.Die();
                self.room.AddObject(new ZapCoil.ZapFlash(self.firstChunk.pos, 2f));
                self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1f);
            }

            Vector2 force = pushDir.normalized * oobPushStrength;
            foreach (BodyChunk chunk in self.bodyChunks)
            {
                chunk.vel += force;
            }
        }

        private static RoomCamera FindCameraForRoom(Room room)
        {
            if (room?.game == null) return null;

            foreach (RoomCamera cam in room.game.cameras)
            {
                if (cam.room == room)
                {
                    return cam;
                }
            }
            return null;
        }

        public static void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            Log.LogMessage("LIZARD CTOR!!");
            if (world?.game?.StoryCharacter != LookerEnums.looker)
            {
                orig(self, abstractCreature, world);
                return;
            }
            if (world.game.GetStorySession.saveState.GetBool(SaveFileCode.reachedThrone))
            {
                if (self?.LizardState != null)
                {
                    float randomValue = UnityEngine.Random.value;
                    if (randomValue > 0.95f)
                    {
                        self.LizardState.rotType = LizardState.RotType.Full;
                    }
                    else if (randomValue > 0.85f)
                    {
                        self.LizardState.rotType = LizardState.RotType.Opossum;
                    }
                    else if (randomValue > 0.70f)
                    {
                        self.LizardState.rotType = LizardState.RotType.Slight;
                    }
                }
                else
                {
                    Log.LogMessage("Couldnt grab self.LizardState!");
                }
            }

            orig(self, abstractCreature, world);

            Log.LogMessage("LOOKER LIZARD CTOR!!");
            if (world?.region?.name != null)
            {
                string regionName = world.region.name.ToLowerInvariant();
                if (regionName.Contains("wvwa"))
                {
                    self.effectColor = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.87f, 0.1f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));
                }
                if (regionName.Contains("warg") && (OptionsMenu.strongerLizardChance.Value == 1f || UnityEngine.Random.value < OptionsMenu.strongerLizardChance.Value))
                {
                    Log.LogMessage("Buffing The Surface lizards!!");
                    if ((OptionsMenu.lizardsCanLeap.Value || CheckEasyMode(self.room)) && self.jumpModule == null) self.jumpModule = new LizardJumpModule(self);
                    if (OptionsMenu.lizardsCanShield.Value && self.blizzardModule == null) self.blizzardModule = new LizardBlizzardModule(self);
                }
            }

            if (self.spawnDataEvil == 0)
            {
                self.spawnDataEvil = 0.3f * OptionsMenu.spawnFileDifficulty.Value;
            }
        }

        public static void BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity self)
        {
            orig(self);
            if (CheckMechanics(self.game, "storage", "WARD") && OptionsMenu.normalGravity.Value)
            {
                self.counter = 10;
                self.progress = 0f;
                self.from = 0f;
                self.to = 0f;
            }
        }

        public static void Inspector_InitiateGraphicsModule(On.MoreSlugcats.Inspector.orig_InitiateGraphicsModule orig, Inspector self)
        {
            if (self.ownerIterator == -1)
            {
                if (CheckMechanics(self.room, "storage", "WARD")) { self.ownerIterator = 752; }
            }
            orig(self);
        }

        public static Color On_Inspector_get_OwneriteratorColor(Func<Inspector, Color> orig, Inspector self)
        {
            if (self.ownerIterator == 752) { return new Color(0.59f, 0.38f, 1f); }
            return orig(self);
        }



        public static int RainCycle_GetDesiredCycleLength(On.RainCycle.orig_GetDesiredCycleLength orig, RainCycle self)
        {
            int value = orig(self);

            if (self?.world?.game?.StoryCharacter != null)
            {
                if (self.world.game.StoryCharacter == LookerEnums.looker)
                {
                    if (self?.world?.region?.name != null && self.world.region.name == "WSKA")
                    {
                        value = (int)((float)value * (0.35 * OptionsMenu.rainTimerMult.Value));
                    }
                    else if (self?.world?.region?.name != null && self.world.region.name == "WRRA")
                    {
                        value = (int)((float)value * 0.50);
                    }
                }
            }
            else
            {
                Log.LogMessage("Couldnt find story character for rain cycle!");
            }
            return value;
        }

        public static void Player_LungUpdate(On.Player.orig_LungUpdate orig, Player self)
        {
            orig(self);
            if (!CWTs.PlayerCWT.TryGetData(self, out var data)) return;
            if (self.room != null && CheckMechanics(self.room, "salination", "WARB") && self.airInLungs <= 0.1f && OptionsMenu.emergencyBreath.Value && !data.usedEmergencyBreath)
            {
                data.usedEmergencyBreath = true;
                for (int i = 0; i < 10; i++)
                {
                    Bubble bubble = new(self.firstChunk.pos + Custom.RNV() * UnityEngine.Random.value * 6f, Custom.RNV() * 1.5f * Mathf.Lerp(6f, 16f, UnityEngine.Random.value) * Mathf.InverseLerp(0f, 0.45f, 0.5f), bottomBubble: false, fakeWaterBubble: false);
                    self.room.AddObject(bubble);
                    bubble.age = 600 - UnityEngine.Random.Range(20, UnityEngine.Random.Range(30, 80));
                }
                self.airInLungs = 1f;
                self.lungsExhausted = false;
            }
        }

        public static int LethalThunderStorm_GetLethalDelay(On.Watcher.LethalThunderStorm.orig_GetLethalDelay orig, LethalThunderStorm self, float amount)
        {
            int value = orig(self, amount);
            if (self?.room != null && CheckMechanics(self.room, "stormy", "WSKC"))
            {
                if (CheckEasyMode(self.room)) { return (int)(value / Math.Min(OptionsMenu.lightningSpawnSpeed.Value, 0.7f)); }
                else { return (int)(value / OptionsMenu.lightningSpawnSpeed.Value); }
            }
            return value;
        }

        public static Vector2 StaticBuildup_GetBestTarget(On.Watcher.LightningMaker.StaticBuildup.orig_GetBestTarget orig, LightningMaker.StaticBuildup self)
        {
            Vector2 value = orig(self);
            if (OptionsMenu.lessEvilLightnings.Value || CheckEasyMode(self.room))
            {
                return value;
            }
            if (!CheckMechanics(self?.room, "stormy", "WSKC"))
            {
                return value;
            }
            if (self.room.lightningMaker == null)
            {
                return value;
            }
            foreach (PhysicalObject physicalObject in self.targets)
            {
                if (physicalObject != null && !self.IsTargetForbidden(physicalObject) && !self.room.lightningMaker.IsPosProtected(physicalObject.firstChunk.pos) && physicalObject is Player player && PlayerCWT.TryGetData(player, out var data))
                {
                    if (data.timesUntilTargetedByLightning > 0)
                    {
                        data.timesUntilTargetedByLightning--;
                        if (data.timesUntilTargetedByLightning == 0)
                        {
                            player.eyesClosedTime = 200;
                        }
                        return value;
                    }
                    data.timesUntilTargetedByLightning = 2;
                    return physicalObject.firstChunk.pos;
                }
            }
            return value;
        }
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
            if (Plugin.CheckMechanics(self.room, "wrecks", "WRRA") && (!OptionsMenu.noFrogStacking.Value || !CheckEasyMode(self.room)) && chunk.owner is Creature && (chunk.owner as Creature).grabbedBy.Count > 0)
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
                        if (self.room.abstractRoom.creatures[i].realizedCreature is Player && (!CheckEasyMode(self.room) && Custom.DistLess(self.lightSource.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, 130f * OptionsMenu.breathZoneSize.Value) || (CheckEasyMode(self.room) && Custom.DistLess(self.lightSource.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, 130f * Math.Max(OptionsMenu.breathZoneSize.Value, 1.5f)))))
                        {
                            (self.room.abstractRoom.creatures[i].realizedCreature as Player).airInLungs = 1f;
                        }
                    }
                }
            }
        }

        public static void Creature_Update(ILContext il)
        {
            FieldInfo waterIsLethal = typeof(Water).GetField(nameof(Water.WaterIsLethal));

            if (waterIsLethal == null)
            {
                Log.LogMessage("WaterIsLethal field not found!");
                return;
            }

            ILCursor c = new(il);

            if (!c.TryGotoNext(MoveType.After, x => x.MatchLdfld(waterIsLethal)))
            {
                Log.LogMessage("Creature_Update IL ldfld failed!");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Creature, bool>>(IsWaterNonlethal);
            c.Emit(OpCodes.Not);
            c.Emit(OpCodes.And);
        }

        public static bool IsWaterNonlethal(Creature creature)
        {
            if (CheckMechanics(creature?.room, "waterways", "WVWA") && (CheckEasyMode(creature.room) || OptionsMenu.acidProtection.Value) && creature is Player player && PlayerCWT.TryGetData(player, out var data) && data.acidShieldTimer > 0)
            {
                return true;
            }
            return false;
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
