using BepInEx;
using BepInEx.Logging;
using Looker.CWTs;
using Looker.Regions;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Music;
using Newtonsoft.Json.Linq;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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

namespace Looker
{
    public static class LPlayer_Flower
    {
        public static float Visibility_Bonus(Func<Player, float> orig, Player self)
        {
            if (PlayerCWT.TryGetData(self, out PlayerCWT.DataClass data) && data.fakingDeath > 0)
            {
                return -1f;
            }
            else
            {
                return orig(self);
            }
        }

        public static bool CanSee_VoidSpawn(Func<SaveState, bool> orig, SaveState self)
        {
            return orig(self) || (self.saveStateNumber == LookerEnums.looker);
        }

        public static float PlayerRippleLevel(Func<Player, float> orig, Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker)
            {
                return 5f;
            }
            return orig(self);
        }

        public static float PlayerMaxRippleLevel(Func<Player, float> orig, Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker)
            {
                return 5f;
            }
            return orig(self);
        }
        public static float OverrideGravity(Func<Player, float> orig, Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker && PlayerCWT.TryGetData(self, out var data) && data.shouldOverrideGravity && self.room != null)
            {
                return data.overrideAirfriction;
            }
            return orig(self);
        }

        public static float OverrideAirFriction(Func<Player, float> orig, Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker && PlayerCWT.TryGetData(self, out var data) && data.shouldOverrideGravity && self.room != null)
            {
                return data.overrideGravity * self.room.gravity;
            }
            return orig(self);
        }

        public static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self?.SlugCatClass != null && self.SlugCatClass == LookerEnums.looker)
            {
                self.glowing = OptionsMenu.enableGlow.Value;
            }
        }

        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (self?.room == null || self.inShortcut) return;
            if (!CWTs.PlayerCWT.TryGetData(self, out var data)) return;

            if (self.SlugCatClass == LookerEnums.looker && self.input != null && self.input.Length > 2)
            {
                // faking death ability
                if (!OptionsMenu.differentAbility.Value)
                {
                    if (data.fakingDeath > 0)
                    {
                        data.fakingDeath--;
                    }
                    if (self.input[0].spec && self.stun < 20)
                    {
                        data.fakingDeath = 15;
                    }
                }

                // float ability
                else
                {
                    // check if player currently wants to float
                    if (self.Consious && self.input[0].spec && data.floatRemaining > 0)
                    {
                        data.floatRemaining--;
                        // check if player just started floating
                        if (!data.currentlyFloating)
                        {
                            self.room.AddObject(new RippleRing(self.mainBodyChunk.pos, 120, 1f, 0.7f));
                            for (int i = 0; i < self.bodyChunks.Length; i++)
                            {
                                if (self.bodyChunks[i].vel.y < 0)
                                {
                                    self.bodyChunks[i].vel.y *= 0.1f;
                                }
                            }
                        }
                        data.currentlyFloating = true;
                    }
                    else
                    {
                        data.currentlyFloating = false;
                        if (self.canJump > 0)
                        {
                            data.floatRemaining = Math.Min(data.floatRemaining + 2, 60);
                        }
                    }
                    if (data.currentlyFloating)
                    {
                        data.timeInFloat++;
                        self.gravity = 0.3f;
                        for (int i = 0; i < self.bodyChunks.Length; i++)
                        {
                            if (self.bodyChunks[i].vel.y < 8)
                            {
                                self.bodyChunks[i].vel.y += Mathf.Min(0.25f * Mathf.Pow(data.timeInFloat, 0.7f), 1.1f);
                            }
                        }
                    }
                    else
                    {
                        self.airFriction = 0.999f;
                    }
                    if ((!data.currentlyFloating && data.timeInFloat > 0) || (data.currentlyFloating && data.floatRemaining == 0))
                    {
                        self.Stun(data.timeInFloat * 2);
                        data.timeInFloat = 0;
                    }
                }
            }

            if (self.room.game?.StoryCharacter == LookerEnums.looker)
            {
                if (data.timeUntilChaser > 0)
                {
                    data.timeUntilChaser--;
                    if (data.timeUntilChaser == 0)
                    {
                        data.timeUntilChaser = -1;
                        LMigration.SpawnChaser(self);
                        data.chaserpos = new IntVector2();
                    }
                }

                data.previousKarmaMode = data.karmaMode;
                if (self.grasps.Length != 0)
                {
                    bool flag = false;
                    for (int i = 0; i < self.grasps.Length; i++)
                    {
                        if (self.grasps[i]?.grabbed is VultureMask && (self.grasps[i].grabbed as VultureMask).abstractPhysicalObject.ID == SpecialId)
                        {
                            data.karmaMode = true;
                            flag = true;
                        }
                        if (!flag) data.karmaMode = false;
                    }
                }
                if (data.darknessImmunity > 0)
                {
                    retractDarkness = true;
                    data.darknessImmunity--;
                }
                else
                {
                    retractDarkness = false;
                }
                if (darknessProgress > 0.8)
                {
                    self.eyesClosedTime = 10;
                    self.slowMovementStun = 40;
                    if (darknessProgress >= 1 && data.darknessImmunity <= 0)
                    {
                        if (!OptionsMenu.weakerDarkness.Value && !self.dead)
                        {
                            self.Die();
                        }
                    }
                }
            }
        }

        public static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (self?.player != null && self.player.SlugCatClass == LookerEnums.looker)
            {
                self.blink = 0;
            }
        }
        public static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;
            orig(self);
            if (!wasDead && self.dead && self.SlugCatClass == LookerEnums.looker)
            {
                if (OptionsMenu.deathExplosion.Value)
                {
                    var room = self.room;
                    var pos = self.mainBodyChunk.pos;
                    if (self?.room == null || pos == null) return;
                    room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                    room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                    room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, new Color(1f, 1f, 1f)));

                    room.ScreenMovement(pos, default, 1.3f);
                    room.PlaySound(LookerEnums.vineboom, self.firstChunk.pos, 0.6f + UnityEngine.Random.value * 0.2f, 0.8f + UnityEngine.Random.value * 0.4f);
                }
            }

        }
        public static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig(self, pos, newRoom, spitOutAllSticks);

            if (newRoom?.world?.game?.StoryCharacter != LookerEnums.looker)
            {
                return;
            }
            if (!CWTs.PlayerCWT.TryGetData(self, out var data))
            {
                Log.LogMessage("Couldnt get playerCWT in SpitOutOfShortcut!");
                return;
            }
            string roomName = newRoom?.abstractRoom?.name;
            bool enteredNewRoom = (data.oldRoom != roomName);
            data.oldRoom = roomName;

            string regionName = newRoom?.world?.region?.name;
            bool enteredNewRegion = (data.oldRegion != regionName);
            data.oldRegion = regionName;

            if (enteredNewRegion)
            {
                data.inShelter = true;
            }

            if (CheckMechanics(newRoom, "migration", "WMPA"))
            {
                if (newRoom.abstractRoom.name == data.oldChaserRoom)
                {
                    data.timeUntilChaser = 40;
                }
                else
                {
                    data.timeUntilChaser = 80;
                }
                data.chaserpos = pos;
                data.oldChaserRoom = newRoom.abstractRoom.name;
                Vector2 vector = Custom.IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    self.bodyChunks[i].vel = vector * 35f;
                }
            }
            if (roomName.StartsWith("WARA") && CheckMechanics(self.room, "signal", "WPTA"))
            {
                Plugin.delayedTutorial = "WPTA";
            }

            if (roomName.StartsWith("WORA_THRONE"))
            {
                SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "ReachedThrone", true);
            }

            if (roomName == "WORA_AI")
            {
                bool flag = true;
                for (int i = 0; i < newRoom.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < newRoom.physicalObjects[i].Count; j++)
                    {
                        if (newRoom.physicalObjects[i][j] is VultureMask)
                        {
                            flag = false;
                        }
                    }
                }
                if (flag)
                {
                    Log.LogMessage("Spawning karma mask!");
                    VultureMask.AbstractVultureMask abstractVultureMask = new(newRoom.world, null, newRoom.GetWorldCoordinate(new Vector2(1050f, 400f)), SpecialId, self.abstractCreature.ID.RandomSeed, false);
                    self.room.abstractRoom.AddEntity(abstractVultureMask);
                    abstractVultureMask.RealizeInRoom();
                }
            }

            if (Plugin.delayedTutorial != null)
            {
                LProgression.ShowTutorial(self.room);
                Plugin.delayedTutorial = null;
            }

            if (data.inShelter && SaveFileCode.GetBool(self.room.game.GetStorySession.saveState, "ReachedThrone") && !SaveFileCode.GetBool(self.room.game.GetStorySession.saveState, "ShownMaskTutorial") && enteredNewRoom)
            {
                SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "ShownMaskTutorial", true);
                newRoom.game.cameras[0].hud.textPrompt.AddMessage("Karma mask can be remade if lost", 120, 160, darken: true, hideHud: true);
                newRoom.game.cameras[0].hud.textPrompt.AddMessage("To do so, hibernate while holding a flower and any mask", 120, 160, darken: true, hideHud: true);
            }

            if (CheckMechanics(self.room, "turbulent", "WRFB") && data.inShelter && enteredNewRoom)
            {
                data.inShelter = OptionsMenu.moreJetfish.Value;
                AbstractCreature abstractCreature = new(newRoom.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.JetFish), null, self.abstractCreature.pos, newRoom.game.GetNewID())
                {
                    saveCreature = false
                };
                newRoom.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.RealizeInRoom();
                newRoom.AddObject(new ShockWave(new Vector2(self.mainBodyChunk.pos.x, self.mainBodyChunk.pos.y), 300f, 0.2f, 15, false));
            }
            if (CheckMechanics(self.room, "signal", "WPTA") && enteredNewRoom && data.inShelter)
            {
                data.inShelter = false;
                data.signalLeniency = (int)(400 * OptionsMenu.broadcastingLeniencyTimer.Value);
                AbstractCreature abstractCreature = new(newRoom.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.VultureGrub), null, self.abstractCreature.pos, newRoom.game.GetNewID())
                {
                    saveCreature = false
                };
                newRoom.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.RealizeInRoom();
                newRoom.AddObject(new ShockWave(new Vector2(self.mainBodyChunk.pos.x, self.mainBodyChunk.pos.y), 300f, 0.2f, 15, false));
            }

            if (CheckMechanics(self.room, "sunbaked", "WSKB") && OptionsMenu.resetDarkness.Value && enteredNewRoom)
            {
                darknessProgress = 0f;
            }

            if (CheckMechanics(self.room, "fetid", "WARC") && enteredNewRoom)
            {
                if (!OptionsMenu.stableMovement.Value)
                {
                    data.reverseHorizontal = UnityEngine.Random.value > 0.5;
                    data.reverseVertical = UnityEngine.Random.value > 0.5;
                }
                data.controlOffset = (int)(UnityEngine.Random.value * 100) % 3;
                if (OptionsMenu.controlAnnouncement.Value)
                {
                    string announcement = string.Empty;
                    if (data.reverseHorizontal)
                    {
                        announcement += "Reversed horizontal, ";
                    }
                    if (data.reverseVertical)
                    {
                        announcement += "Reversed vertical, ";
                    }
                    switch (data.controlOffset)
                    {
                        case 0: announcement += "Throw and grab swapped"; break;
                        case 1: announcement += "Jump and grab swapped"; break;
                        case 2: announcement += "Jump and throw swapped"; break;
                    }
                    newRoom.game.cameras[0].hud.textPrompt.AddMessage(announcement, 40, 200, darken: false, hideHud: false);
                }
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    Vector2 vector = Custom.IntVector2ToVector2(newRoom.ShorcutEntranceHoleDirection(pos));
                    self.bodyChunks[i].vel = vector * 10f;
                }
            }
            

            data.inShelter = newRoom.abstractRoom.shelter;

            if (data.inShelter)
            {
                KarmaMask.CheckMaskMechanics(newRoom);
            }
        }

        public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (sLeaser?.sprites is { Length: > 9 } && self?.player?.SlugCatClass == LookerEnums.looker && PlayerCWT.TryGetData(self?.player, out PlayerCWT.DataClass data) && data.fakingDeath > 0)
            {
                sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("FaceDead");
            }
        }

        public static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.player?.SlugCatClass == LookerEnums.looker)
            {
                sLeaser.sprites[0].shader = Custom.rainWorld.Shaders["PlayerCamoMaskBeforePlayer"];
                for (int i = 1; i < 10; i++)
                {
                    sLeaser.sprites[i].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
                }
                sLeaser.sprites[11].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
            }
        }

        public static void Room_MaterializeRippleSpawn(On.Room.orig_MaterializeRippleSpawn orig, Room self, Vector2 spawnPos, Room.RippleSpawnSource source)
        {
            if (!self.game.IsStorySession || self.game.StoryCharacter != LookerEnums.looker)
            {
                orig(self, spawnPos, source);
                return;
            }
            VoidSpawn.SpawnType spawnType = VoidSpawn.SpawnType.RippleSpawn;
            VoidSpawn voidSpawn = new(new AbstractPhysicalObject(self.world, WatcherEnums.AbstractObjectType.RippleSpawn, null, self.GetWorldCoordinate(spawnPos), self.game.GetNewID()), self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidMelt), VoidSpawnKeeper.DayLightMode(self), spawnType);

            if (source == Room.RippleSpawnSource.Dimension)
            {
                float value = UnityEngine.Random.value;
                if (value < 0.25f)
                {
                    voidSpawn.behavior = new VoidSpawn.BezierSwarm(voidSpawn, self);
                }
                else if (value < 0.55f)
                {
                    int num4 = -1;
                    if (self.abstractRoom.connections.Length != 0)
                    {
                        num4 = self.abstractRoom.connections[UnityEngine.Random.Range(0, self.abstractRoom.connections.Length)];
                    }
                    if (self.world.GetAbstractRoom(num4) == null)
                    {
                        voidSpawn.behavior = new VoidSpawn.EggAndAway(voidSpawn, self);
                    }
                    else
                    {
                        voidSpawn.behavior = new VoidSpawn.PassThrough(voidSpawn, num4, self);
                    }
                }
                else
                {
                    voidSpawn.behavior = new VoidSpawn.BezierSwarm(voidSpawn, self);
                }
            }
            else
            {
                voidSpawn.behavior = new VoidSpawn.EggAndAway(voidSpawn, self);
            }
            voidSpawn.PlaceInRoom(self);
            voidSpawn.timeUntilFadeout = UnityEngine.Random.Range(200, 400);
            if (source == Room.RippleSpawnSource.Dimension)
            {
                voidSpawn.ChangeRippleLayer(1, showEffect: false);
            }
            else
            {
                voidSpawn.ChangeRippleLayer(0, showEffect: true);
            }
        }

        public static void AbstractConsumable_Consume(On.AbstractConsumable.orig_Consume orig, AbstractConsumable self)
        {
            if (self.world?.game?.StoryCharacter == LookerEnums.looker && !self.isConsumed)
            {
                self.isConsumed = true;
                if (self.world.game.session is StoryGameSession)
                {
                    (self.world.game.session as StoryGameSession).saveState.ReportConsumedItem(self.world, false, self.originRoom, self.placedObjectIndex, self.minCycles);
                }
            }
            orig(self);
        }

        public static void KarmaFlowerPatch_DrawSprites(On.Watcher.KarmaFlowerPatch.orig_DrawSprites orig, KarmaFlowerPatch self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                for (int i = 0; i < self.flowers.Length; i++)
                {
                    sLeaser.sprites[self.EffectSprite(i, 0)].color = Color.Lerp(RainWorld.RippleColor, Color.white, 0.3f);
                }
            }
        }

        public static bool KarmaFlower_CanSpawnKarmaFlower(On.KarmaFlower.orig_CanSpawnKarmaFlower orig, Room room)
        {
            return orig(room) || room?.game?.StoryCharacter == LookerEnums.looker;
        }

        public static void KarmaFlowerPatch_ApplyPalette(On.Watcher.KarmaFlowerPatch.orig_ApplyPalette orig, KarmaFlowerPatch self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                self.petalColor = RainWorld.RippleColor;
                self.stalkColor = Color.Lerp(palette.blackColor, palette.fogColor, 0.3f);
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }
    }
}
