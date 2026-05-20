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

        public static int AcidShieldTimer = 80;
        /*public static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            if (self.coord.x != self.lastCoord.x || self.coord.y != self.lastCoord.y || self.coord.room != self.lastCoord.room)
            {
                self.NewTile();
            }
            if (ModManager.MSC)
            {
                self.SafariControlInputUpdate(0);
            }
            self.lastCoord = self.abstractCreature.pos;
            if (self.abstractCreature.realizedCreature != self)
            {
                Custom.LogWarning(new string[]
                {
                "ABSTRACT CREATURE REALIZED CREATURE MISMATCH!"
                });
                if (self.abstractCreature.realizedCreature != null)
                {
                    self.abstractCreature.realizedCreature.Destroy();
                }
                self.abstractCreature.realizedCreature = self;
            }
            if (self.newToRoomInvinsibility > 0)
            {
                self.newToRoomInvinsibility--;
            }
            if (self.stun > 0)
            {
                int stun = self.stun;
                self.stun = stun - 1;
            }
            if (self.stun < 10)
            {
                for (int i = 0; i < self.grabbedBy.Count; i++)
                {
                    if (self.grabbedBy[i].pacifying)
                    {
                        self.stun = 10;
                        break;
                    }
                }
            }
            if (self.shortcutDelay > 0)
            {
                self.shortcutDelay--;
            }
            if (self.blind > 0)
            {
                self.blind--;
            }
            if (self.deaf > 0)
            {
                self.deaf--;
            }
            if (self.muddy > 0)
            {
                self.muddy -= (int)Mathf.Lerp(1f, 10f, self.Submersion);
                self.repelLocusts = Mathf.Max(self.repelLocusts, Mathf.Min(self.muddy, 80));
            }
            if (self.repelLocusts > 0)
            {
                self.repelLocusts--;
            }
            if (ModManager.HypothermiaModule)
            {
                self.HypothermiaUpdate();
            }
            if (self.abstractCreature.rippleCreature)
            {
                if (self.rippleTransferCooldown > 0)
                {
                    self.rippleTransferCooldown--;
                }
                float num = (self.abstractCreature.rippleLayer != self.abstractCreature.world.game.ActiveRippleLayer) ? 1f : 5f;
                for (int j = 0; j < self.room.cosmeticRipples.Count; j++)
                {
                    if (self.rippleTransferCooldown <= 0 && self.room.cosmeticRipples[j].Data != null && self.room.cosmeticRipples[j].Data.cycleExpiry > 0 && self.room.cosmeticRipples[j].PointInRipple(self.mainBodyChunk.pos) > 0f && UnityEngine.Random.value <= 1f / (40f * num))
                    {
                        self.ChangeRippleLayer((self.abstractCreature.rippleLayer == 0) ? 1 : 0);
                        if (self.abstractCreature.realizedCreature != null)
                        {
                            self.room.PlaySound(WatcherEnums.WatcherSoundID.Ripple_Creature_Swap_Dimensions, self.abstractCreature.realizedCreature.firstChunk.pos);
                        }
                        self.room.AddObject(new ShockWave(self.mainBodyChunk.pos, 250f, 0.1f, 40, false));
                        self.rippleTransferCooldown = 800;
                        if (self.abstractCreature.abstractAI.RealAI != null)
                        {
                            self.abstractCreature.abstractAI.RealAI.ripplePathingTarget = null;
                            self.abstractCreature.abstractAI.RealAI.ripplePathingTime = 0;
                        }
                    }
                }
            }
            self.BrineWaterInteraction();
            if (self.Submersion < 0.2f && CheckMechanics(self.room, "waterways", "WVWA") && self?.room?.game?.StoryCharacter == LookerEnums.looker && (CheckEasyMode(self.room) || OptionsMenu.acidProtection.Value)) { AcidShieldTimer = 80; }
            if (self.Submersion > 0.1f && self.room.waterObject != null && self.room.waterObject.WaterIsLethal && !self.abstractCreature.lavaImmune)
            {
                if (self.Submersion > 0.2f)
                {
                    if (self is Player && !self.dead)
                    {
                        if (ModManager.MSC && (self as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                        {
                            (self as Player).pyroJumpCounter++;
                            if ((self as Player).pyroJumpCounter >= MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity.Value)
                            {
                                (self as Player).PyroDeath();
                            }
                        }
                        else if (CheckMechanics(self.room, "waterways", "WVWA") && self?.room?.game?.StoryCharacter == LookerEnums.looker && (CheckEasyMode(self.room) || OptionsMenu.acidProtection.Value))
                        {
                            if (AcidShieldTimer > 0) { AcidShieldTimer--; }
                            else { (self as Player).Die(); }
                            
                        }
                        else { (self as Player).Die(); }
                    }
                    else if (self.State is HealthState || (self.State is HealthState && (self.State as HealthState).health > 1f))
                    {
                        if (!self.dead)
                        {
                            self.Violence(null, new Vector2?(new Vector2(0f, 5f)), self.firstChunk, null, Creature.DamageType.Explosion, 0.2f, 0.1f);
                        }
                    }
                    else if (!self.dead)
                    {
                        self.Die();
                    }
                    if (self.lavaContactCount == 0)
                    {
                        self.mainBodyChunk.vel.y = 35f;
                        self.room.AddObject(new Smoke.Smolder(self.room, self.firstChunk.pos, self.firstChunk, null));
                    }
                    else if (self.lavaContactCount == 1)
                    {
                        self.mainBodyChunk.vel.y = 20f;
                    }
                    else if (self.lavaContactCount == 2)
                    {
                        self.mainBodyChunk.vel.y = 15f;
                    }
                    else if (self.lavaContactCount == 3)
                    {
                        self.mainBodyChunk.vel.y = 5f;
                        self.room.AddObject(new Smoke.Smolder(self.room, self.firstChunk.pos, self.firstChunk, null));
                    }
                    if (self.lavaContactCount < ((self is Player) ? 400 : 30))
                    {
                        self.lavaContactCount++;
                        self.room.AddObject(new Explosion.ExplosionSmoke(self.firstChunk.pos, Custom.RNV() * (5f * UnityEngine.Random.value), 1f));
                    }
                    if (!self.lavaContact)
                    {
                        if (self.lavaContactCount <= 3)
                        {
                            for (int k = 0; k < 14 + (3 - self.lavaContactCount) * 5; k++)
                            {
                                Vector2 a = Custom.RNV();
                                self.room.AddObject(new Spark(self.firstChunk.pos + a * (UnityEngine.Random.value * 40f), a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 8, 24));
                            }
                        }
                        self.room.PlaySound(SoundID.Firecracker_Burn, self.firstChunk, false, 0.5f, 0.5f + UnityEngine.Random.value * 1.5f);
                        self.lavaContact = true;
                        self.lavaContactCount++;
                    }
                }
                else if (self.lavaContactCount == 0)
                {
                    self.lavaContactCount++;
                    self.room.AddObject(new Smoke.Smolder(self.room, self.firstChunk.pos, self.firstChunk, null));
                    self.room.PlaySound(SoundID.Firecracker_Burn, self.firstChunk, false, 0.5f, 0.3f + UnityEngine.Random.value * 0.6f);
                    self.lavaContact = true;
                }
            }
            else
            {
                self.lavaContact = false;
            }
            if (self.killTagCounter > 0 && !self.State.dead && (!(self.State is HealthState) || (self.State as HealthState).health > 0f))
            {
                self.killTagCounter--;
                if (self.killTagCounter < 1)
                {
                    self.killTag = null;
                }
            }
            self.rainDeath = Mathf.Clamp(self.rainDeath - 0.0125f, 0f, 1f);
            if (ModManager.ChallengeModule && self.State is HealthState && self.room.world.game.IsArenaSession && self.room.world.game.GetArenaGameSession.chMeta != null && self.room.world.game.GetArenaGameSession.chMeta.invincibleCreatures)
            {
                (self.State as HealthState).health = 1f;
            }
            if (self.injectedPoison > 0f)
            {
                HealthState healthState = self.State as HealthState;
                if (healthState != null)
                {
                    float num2 = self.injectedPoison;
                    float num3 = 0.0027777778f;
                    self.injectedPoison = Mathf.Max(self.injectedPoison - num3, 0f);
                    healthState.health -= (num2 - self.injectedPoison) / self.Template.baseDamageResistance;
                }
                else
                {
                    float num4 = self.injectedPoison / self.Template.instantDeathDamageLimit;
                    float num5 = 1f;
                    Player player = self as Player;
                    if (player != null)
                    {
                        player.slowMovementStun = Math.Max(player.slowMovementStun, Mathf.RoundToInt(num4 * 8f));
                        player.drown = Mathf.Max(player.drown, num4 * 0.5f);
                        num5 = player.aerobicLevel * 0.8f + 0.2f * num4;
                        if ((double)UnityEngine.Random.value < (double)num4 * 0.1 || (double)num4 > 0.8)
                        {
                            player.Blink(10);
                        }
                        if (player.graphicsModule != null)
                        {
                            PlayerGraphics playerGraphics = player.graphicsModule as PlayerGraphics;
                            playerGraphics.malnourished = Mathf.Max(playerGraphics.malnourished, num4 * 1.5f);
                        }
                    }
                    if (self.Consious && (double)UnityEngine.Random.value < 0.03 * (double)num4 * (double)(0.5f + num4 * 0.5f) * (double)num5)
                    {
                        Frog frog = self as Frog;
                        if (frog == null || frog.grasps[0] == null)
                        {
                            self.Stun(UnityEngine.Random.Range(20, Mathf.RoundToInt(80f * UnityEngine.Random.value)));
                            Player player2 = self as Player;
                            if (player2 != null)
                            {
                                player2.Blink(100);
                                player2.exhausted = true;
                            }
                        }
                    }
                    if (num4 >= 1f && !self.dead)
                    {
                        self.Die();
                    }
                }
            }
            if (!self.dead && self.State is HealthState && (self.State as HealthState).health < 0f && UnityEngine.Random.value < -(self.State as HealthState).health && UnityEngine.Random.value < 0.025f)
            {
                self.Die();
            }
            float num6 = -self.bodyChunks[0].restrictInRoomRange + 1f;
            if (self is Player && self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)
            {
                if ((self as Player).bodyMode == Player.BodyModeIndex.WallClimb)
                {
                    num6 = Mathf.Max(num6, -250f);
                }
                else
                {
                    num6 = Mathf.Max(num6, -500f);
                }
            }
            if (self.bodyChunks[0].pos.y < num6 + 10f && !self.Template.canFly && self.grabbedBy.Count > 0)
            {
                Custom.Log(new string[]
                {
                "FORCE CREATURE RELEASE UNDER ROOM"
                });
                while (self.grabbedBy.Count > 0)
                {
                    self.grabbedBy[0].Release();
                }
            }
            if (self.bodyChunks[0].pos.y < num6 && (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10) && (!self.Template.canFly || self.Stunned || self.dead) && (self is Player || !self.room.game.IsArenaSession || self.room.game.GetArenaGameSession.chMeta == null || !self.room.game.GetArenaGameSession.chMeta.oobProtect))
            {
                Custom.Log(new string[]
                {
                string.Format("{0} Fell out of room!", self.abstractCreature)
                });
                if (ModManager.CoopAvailable && self.State is PlayerState)
                {
                    (self as Player).PermaDie();
                }
                self.Die();
                self.Destroy();
                self.abstractCreature.Destroy();
            }
            if (self.room.terrain != null && self.WantsToBurrow)
            {
                List<AbstractPhysicalObject> allConnectedObjects = self.abstractCreature.GetAllConnectedObjects();
                for (int l = 0; l < allConnectedObjects.Count; l++)
                {
                    if (allConnectedObjects[l].realizedObject != null)
                    {
                        allConnectedObjects[l].realizedObject.Buried = true;
                    }
                }
            }
            if (self.enteringShortCut != null)
            {
                self.shortCutRadAdd += 0.2f;
                if (self.graphicsModule != null)
                {
                    self.graphicsModule.Update();
                }
                int num7 = 0;
                Vector2 vector = self.room.MiddleOfTile(self.enteringShortCut.Value) + Custom.IntVector2ToVector2(self.room.ShorcutEntranceHoleDirection(self.enteringShortCut.Value)) * -5f;
                List<AbstractPhysicalObject> allConnectedObjects2 = self.abstractCreature.GetAllConnectedObjects();
                for (int m = 0; m < allConnectedObjects2.Count; m++)
                {
                    if (allConnectedObjects2[m].realizedObject != null)
                    {
                        for (int n = 0; n < allConnectedObjects2[m].realizedObject.bodyChunks.Length; n++)
                        {
                            allConnectedObjects2[m].realizedObject.bodyChunks[n].vel *= 0.06f;
                            if (!Custom.DistLess(allConnectedObjects2[m].realizedObject.bodyChunks[n].pos, vector, Mathf.Max(10f, self.shortCutRadAdd)))
                            {
                                allConnectedObjects2[m].realizedObject.bodyChunks[n].lastPos = allConnectedObjects2[m].realizedObject.bodyChunks[n].pos;
                                allConnectedObjects2[m].realizedObject.bodyChunks[n].pos += Custom.DirVec(allConnectedObjects2[m].realizedObject.bodyChunks[n].pos, vector) * 4.5f;
                                if (allConnectedObjects2[m] == self.abstractCreature)
                                {
                                    num7++;
                                }
                            }
                            else
                            {
                                allConnectedObjects2[m].realizedObject.bodyChunks[n].pos = vector;
                                allConnectedObjects2[m].realizedObject.bodyChunks[n].lastPos = vector;
                            }
                        }
                    }
                }
                if (num7 == 0)
                {
                    self.SuckedIntoShortCut(self.enteringShortCut.Value, false);
                }
                if (self.graphicsModule != null)
                {
                    self.graphicsModule.SuckedIntoShortCut(vector);
                }
                if (self.appendages != null)
                {
                    for (int num8 = 0; num8 < self.appendages.Count; num8++)
                    {
                        self.appendages[num8].Update();
                    }
                }
                if (self.Stunned)
                {
                    Custom.Log(new string[]
                    {
                    string.Format("{0} cancel shortcut enter", self.abstractCreature)
                    });
                    self.enteringShortCut = null;
                }
            }
            else
            {
                self.shortCutRadAdd = 0.9f;
                self.Update(eu);
            }
            if (self.grasps != null)
            {
                for (int num9 = 0; num9 < self.grasps.Length; num9++)
                {
                    if (self.grasps[num9] != null && self.grasps[num9].grabbed.abstractPhysicalObject.rippleLayer != self.abstractCreature.rippleLayer && !self.grasps[num9].grabbed.abstractPhysicalObject.rippleBothSides && !self.abstractCreature.rippleBothSides)
                    {
                        self.ReleaseGrasp(num9);
                    }
                }
            }
        }*/

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
