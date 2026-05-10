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
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
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
using Unity;
using static Looker.Plugin;
using static SlugBase.Features.FeatureTypes;

namespace Looker
{
    public static class LProgression
    {
        public static bool RegionState_InfectRegionRoomWithSentientRot(On.RegionState.orig_InfectRegionRoomWithSentientRot orig, RegionState self, float amount, string roomName)
        {
            if (self?.world?.game?.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self, amount, roomName);
        }

        public static bool Room_InfectRoomWithSentientRot(On.Room.orig_InfectRoomWithSentientRot orig, Room self, float amount)
        {
            if (self.game?.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self, amount);
        }

        public static void WarpPoint_NewWorldLoaded_Room(On.Watcher.WarpPoint.orig_NewWorldLoaded_Room orig, WarpPoint self, Room newRoom)
        {
            orig(self, newRoom);

            if (!OptionsMenu.checkpointWarps.Value || self.room?.game?.StoryCharacter != LookerEnums.looker) return;

            WarpPoint.WarpPointData warpPointData = (self.overrideData ?? self.Data);
            newRoom.game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave = warpPointData;
            newRoom.game.GetStorySession.saveState.transferCreaturesAfterWarpPointSave.Clear();
            newRoom.game.GetStorySession.saveState.transferObjectsAfterWarpPointSave.Clear();
            newRoom.game.GetStorySession.saveState.importantTransferEntitiesAfterWarpPointSave.Clear();
            foreach (AbstractPhysicalObject t in newRoom.game.GetStorySession.pendingWarpPointTransferObjects)
            {
                if (t is AbstractCreature creature)
                {
                    newRoom.game.GetStorySession.saveState.transferCreaturesAfterWarpPointSave.Add(SaveState.AbstractCreatureToStringStoryWorld(creature));
                }
                else
                {
                    newRoom.game.GetStorySession.saveState.transferObjectsAfterWarpPointSave.Add(t);
                }
            }
            foreach (EntityID t in newRoom.game.GetStorySession.importantWarpPointTransferedEntities)
            {
                newRoom.game.GetStorySession.saveState.importantTransferEntitiesAfterWarpPointSave.Add(t);
            }
            if (newRoom.game.GetStorySession.saveState.miscWorldSaveData.hasSkippedFirstWarpFatigueTransfer == 0)
            {
                newRoom.game.GetStorySession.saveState.preserveWarpFatigueAfterWarpPointSave = 0;
                newRoom.game.GetStorySession.saveState.miscWorldSaveData.hasSkippedFirstWarpFatigueTransfer = 1;
            }
            else
            {
                newRoom.game.GetStorySession.saveState.preserveWarpFatigueAfterWarpPointSave = newRoom.game.GetStorySession.warpsTraversedThisCycle;
            }
            newRoom.game.Win(malnourished: false, fromWarpPoint: true);
        }

        public static bool UsesWarpMap(Func<Menu.KarmaLadderScreen, bool> orig, Menu.KarmaLadderScreen self)
        {
            if (self.saveState != null && self.saveState.saveStateNumber == LookerEnums.looker)
            {
                return !self.RippleLadderMode;
            }
            return orig(self);
        }
        public static void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, WarpPoint self)
        {
            if (self?.room?.game?.StoryCharacter == null)
            {
                Log.LogMessage("Cannot get story character!");
                orig(self);
                return;
            }
            if (self.room.game.StoryCharacter != LookerEnums.looker)
            {
                Log.LogMessage("Story character is not Looker!");
                orig(self);
                return;
            }
            self.Data.destTimeline = LookerEnums.lookerTimeline;
            self.Data.sourceTimeline = LookerEnums.lookerTimeline;
            orig(self);
            Log.LogMessage("Set to Looker timeline!!! from warp");
            self.room.game.GetStorySession.saveState.currentTimelinePosition = LookerEnums.lookerTimeline;
        }

        public static void HI_W05_ctor(On.Watcher.WatcherRoomSpecificScript.HI_W05.orig_ctor orig, WatcherRoomSpecificScript.HI_W05 self, Room room)
        {
            orig(self, room);
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                self.showedCamoTutorial = true;
                self.waitBeforeDrop -= 80;
            }
        }

        public static void HI_W05_Update(On.Watcher.WatcherRoomSpecificScript.HI_W05.orig_Update orig, WatcherRoomSpecificScript.HI_W05 self, bool eu)
        {
            orig(self, eu);
            if (self?.room?.game?.StoryCharacter != LookerEnums.looker)
            {
                return;
            }
            if (self.waitBeforeDrop == 50 || self.waitBeforeDrop == 120 || self.waitBeforeDrop == 160)
            {
                self.room.PlaySound(WatcherEnums.WatcherSoundID.Spinning_Top_Laugh_S);
            }
            if (self.waitBeforeDrop == 1)
            {
                self.room.PlaySound(WatcherEnums.WatcherSoundID.Spinning_Top_Laugh_L);
            }
        }

        public static void ARKillRect_Update(On.ARKillRect.orig_Update orig, ARKillRect self, bool eu)
        {
            if (self.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (CWTs.ARKillRectCWT.TryGetData(self, out var data) && data.active)
                {
                    foreach (Player player in self.room.PlayersInRoom)
                    {
                        if (player == null || player.bodyChunks == null || player.bodyChunks.Length == 0) continue;
                        for (int k = 0; k < player.bodyChunks.Length; k++)
                        {
                            Vector2 vector = player.bodyChunks[k].ContactPoint.ToVector2();
                            Vector2 pos = player.bodyChunks[k].pos + vector * (player.bodyChunks[k].rad + 30f);
                            if (Custom.InsideRect(self.room.GetTilePosition(pos), self.rect))
                            {
                                data.active = false;
                                switch (self.room.abstractRoom.name)
                                {
                                    case "WAUA_BATH":
                                        {
                                            SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "BathEnding", true);
                                            self.room.game.GoToRedsGameOver();
                                            RainWorldGame.BeatGameMode(self.room.game, true);
                                            RainWorldGame.ForceSaveNewDenLocation(self.room.game, "WAUA_TOYS", saveWorldStates: false);
                                            return;
                                        }
                                    case "HI_W14":
                                        {
                                            SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "MaskEnding", true);
                                            self.room.game.GoToRedsGameOver();
                                            RainWorldGame.BeatGameMode(self.room.game, false);
                                            RainWorldGame.ForceSaveNewDenLocation(self.room.game, "HI_W05", saveWorldStates: false);
                                            return;
                                        }
                                    case "WRSA_WEAVER":
                                        {
                                            SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "LinkEnding", true);
                                            self.room.game.GoToRedsGameOver();
                                            RainWorldGame.BeatGameMode(self.room.game, false);
                                            RainWorldGame.ForceSaveNewDenLocation(self.room.game, "WRSA_WEAVER02", saveWorldStates: false);
                                            return;
                                        }
                                    case "WRSA_TREE06":
                                        {
                                            SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "PuzzleEnding", true);
                                            self.room.game.GoToRedsGameOver();
                                            RainWorldGame.BeatGameMode(self.room.game, false);
                                            RainWorldGame.ForceSaveNewDenLocation(self.room.game, "WRSA_WEAVER02", saveWorldStates: false);
                                            return;
                                        }
                                    default:
                                        {
                                            self.room.game.cameras[0].hud.textPrompt.AddMessage("Well, there is a man here.", 20, 160, darken: true, hideHud: true);
                                            AbstractPhysicalObject abstractPhysicalObject = new(player.room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, player.room.GetWorldCoordinate(player.mainBodyChunk.pos), player.abstractCreature.Room.world.game.GetNewID());
                                            player.abstractCreature.Room.AddEntity(abstractPhysicalObject);
                                            abstractPhysicalObject.RealizeInRoom();
                                            if (player.FreeHand() != -1)
                                            {
                                                player.room.game.cameras[0].hud.textPrompt.AddMessage("He offers you an egg.", 20, 160, darken: true, hideHud: true);
                                                player.SlugcatGrab(abstractPhysicalObject.realizedObject, player.FreeHand());
                                            }
                                            else player.room.game.cameras[0].hud.textPrompt.AddMessage("He offers you an egg, but your hands are full.", 20, 160, darken: true, hideHud: true);
                                            return;
                                        }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                orig(self, eu); return;
            }
        }

        public static void SetToEasy()
        {
            OptionsMenu.spawnFileDifficulty.Value = 1;

            OptionsMenu.checkpointWarps.Value = true;
            OptionsMenu.enableGlow.Value = true;

            OptionsMenu.constantShelters.Value = true;

            
            OptionsMenu.weakerDarkness.Value = true;
            OptionsMenu.weakerBarnacles.Value = true;
            OptionsMenu.weakerBroadcast.Value = true;
            
            OptionsMenu.lizardsCanLeap.Value = false;

            OptionsMenu.melonCooldown.Value = 1.5f;
            OptionsMenu.barnacleRate.Value = 0.7f;
            OptionsMenu.darknessSpeed.Value = 0.7f;
            OptionsMenu.ripplespaceDuration.Value = 1.5f;

            OptionsMenu.difficultyChosen.Value = true;
            optionsMenuInstance.config.Save();
        }

        public static void SetToMedium()
        {
            OptionsMenu.spawnFileDifficulty.Value = 2;

            OptionsMenu.checkpointWarps.Value = true;
            OptionsMenu.enableGlow.Value = false;

            OptionsMenu.constantShelters.Value = false;


            OptionsMenu.weakerDarkness.Value = false;
            OptionsMenu.weakerBarnacles.Value = false;
            OptionsMenu.weakerBroadcast.Value = false;

            OptionsMenu.lizardsCanLeap.Value = true;

            OptionsMenu.melonCooldown.Value = 1f;
            OptionsMenu.barnacleRate.Value = 1f;
            OptionsMenu.darknessSpeed.Value = 1f;
            OptionsMenu.ripplespaceDuration.Value = 1f;

            OptionsMenu.difficultyChosen.Value = true;
            optionsMenuInstance.config.Save();
        }

        public static void SetToHard()
        {
            OptionsMenu.spawnFileDifficulty.Value = 3;

            OptionsMenu.checkpointWarps.Value = false;
            OptionsMenu.enableGlow.Value = false;

            OptionsMenu.constantShelters.Value = false;

            OptionsMenu.weakerDarkness.Value = false;
            OptionsMenu.weakerBarnacles.Value = false;
            OptionsMenu.weakerBroadcast.Value = false;

            OptionsMenu.lizardsCanLeap.Value = true;

            OptionsMenu.melonCooldown.Value = 0.7f;
            OptionsMenu.barnacleRate.Value = 2f;
            OptionsMenu.darknessSpeed.Value = 1.5f;
            OptionsMenu.ripplespaceDuration.Value = 1f;

            OptionsMenu.difficultyChosen.Value = true;
            optionsMenuInstance.config.Save();
           
        }

        public static void SlugcatSelectMenu_Singal(On.Menu.SlugcatSelectMenu.orig_Singal orig, Menu.SlugcatSelectMenu self, Menu.MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (self.requestingControllerConnections)
            {
                return;
            }
            if (message.Contains("LOOKER"))
            {
                switch (message)
                {
                    case "LOOKER_DIALOG_0": SetToEasy(); break;
                    case "LOOKER_DIALOG_1": SetToMedium(); break;
                    case "LOOKER_DIALOG_2": SetToHard(); break;
                    default: break;
                }
                foreach (Menu.MenuObject button in self.pages[0].subObjects)
                {
                    if (button is Menu.DialogBoxMultiButtonNotify)
                    {
                        button.RemoveSprites();
                        self.pages[0].subObjects.Remove(button);
                        return;
                    }
                }
            }
        }

        public static void SlugcatPageNewGame_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);
            if (slugcatNumber == LookerEnums.looker)
            {
                if (!shownPopupMenu && !OptionsMenu.difficultyChosen.Value)
                {
                    shownPopupMenu = true;
                    Log.LogMessage("Starting difficulty selection");

                    string text = "Choose Difficulty:" + Environment.NewLine + Environment.NewLine + "Easier mode is still harder than Watcher, but the dangerous spawns and mechanics are toned down." + Environment.NewLine + Environment.NewLine + "Normal mode is the intended experience. Challenging, but mostly fair." + Environment.NewLine + Environment.NewLine + "Unfair mode is around as hard as Inv, if not harder. In case you just want to suffer for some reason." + Environment.NewLine + Environment.NewLine + "Difficulty can be changed at any time in the Remix menu.";
                    string[] buttonTexts = new string[3] { "Easier", "Normal", "Unfair" };
                    string[] array = Enumerable.Repeat("LOOKER_DIALOG_", buttonTexts.Length).ToArray();
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] += i.ToString("0");
                    }
                    Vector2 size = Menu.DialogBoxMultiButtonNotify.CalculateDialogBoxSize(text, buttonTexts.Length);
                    Menu.DialogBoxMultiButtonNotify dialogBoxMultiButtonNotify = new Menu.DialogBoxMultiButtonNotify(self.menu, self.menu.pages[0], text, array, buttonTexts, new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f - size.x / 2f + (1366f - Custom.rainWorld.options.ScreenSize.x) / 2f, 384f - size.y / 2f), size, forceWrapping: true); 
                    self.menu.pages[0].subObjects.Add(dialogBoxMultiButtonNotify);
                }
            }
        }




        public static void MainMenu_Update(On.Menu.MainMenu.orig_Update orig, Menu.MainMenu self)
        {
            orig(self);
            
            if (Input.anyKeyDown)
            {
                switch (puzzleInput)
                {
                    case 0:
                        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) puzzleInput++;
                        else puzzleInput = 0;
                        break;
                    case 1:
                        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) puzzleInput++;
                        else puzzleInput = 0;
                        break;
                    case 2:
                        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) puzzleInput++;
                        else puzzleInput = 0;
                        break;
                    case 3:
                        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) puzzleInput++;
                        else puzzleInput = 0;
                        break;
                    case 4:
                        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) puzzleInput++;
                        else puzzleInput = 0;
                        break;
                    case 5:
                        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                        {
                            self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = LookerEnums.looker;
                            warptodaemon = true;
                            puzzleInput = 0;
                            self.PlaySound(LookerEnums.vineboom);
                            Log.LogMessage("Puzzle complete");
                        }    
                        else puzzleInput = 0;
                        break;
                    default: break;
                }
            }
        }

        public static bool WarpPoint_CheckCanWarpToVoidWeaverEnding(On.Watcher.WarpPoint.orig_CheckCanWarpToVoidWeaverEnding orig, WarpPoint self)
        {
            if (self.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return self.Data.rippleWarp && SaveFileCode.LinkCount(self.room.game.GetStorySession.saveState) > 20 && !SaveFileCode.GetBool(self.room.game.GetStorySession.saveState, "LinkEnding");
            }
            else
            {
                return orig(self);
            }
        }

        public static void WRSA_J01_UpdateObjects(On.Watcher.WatcherRoomSpecificScript.WRSA_J01.orig_UpdateObjects orig, WatcherRoomSpecificScript.WRSA_J01 self)
        {
            if (!self.room.game.IsStorySession || self.room.game.StoryCharacter != LookerEnums.looker)
            {
                orig(self);
                return;
            }
            for (int i = 0; i < self.room.game.cameras.Length; i++)
            {
                if (self.room.game.cameras[i].followAbstractCreature.realizedCreature is Player camFollowingPlayer)
                {
                    self.UpdateTutorials(camFollowingPlayer, i);
                }
            }
        }

        public static void WRSA_J01_ctor(On.Watcher.WatcherRoomSpecificScript.WRSA_J01.orig_ctor orig, WatcherRoomSpecificScript.WRSA_J01 self, Room room)
        {
            orig(self, room);
            if (self.room.game.IsStorySession && self.room.game.StoryCharacter == LookerEnums.looker)
            {
                self.sequenceStartSpawned = true;
                self.altarAwakeEffectsSpawned = true;
            }
        }

        public static void WRSA_J01_UpdateTutorials(On.Watcher.WatcherRoomSpecificScript.WRSA_J01.orig_UpdateTutorials orig, WatcherRoomSpecificScript.WRSA_J01 self, Player camFollowingPlayer, int cam)
        {
            if (!self.room.game.IsStorySession || self.room.game.StoryCharacter != LookerEnums.looker)
            {
                orig(self, camFollowingPlayer, cam);
                return;
            }
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            if (camFollowingPlayer?.room != null && self?.room != null && camFollowingPlayer.room == self.room && camFollowingPlayer.mainBodyChunk?.pos != null && camFollowingPlayer.mainBodyChunk?.pos.y > 800f)
            {
                if (Mathf.Abs(camFollowingPlayer.mainBodyChunk.pos.x - self.leftEye.position.x) < 80f)
                {
                    flag = true;
                }
                else if (Mathf.Abs(camFollowingPlayer.mainBodyChunk.pos.x - self.centerEye1.position.x) < 80f)
                {
                    flag3 = true;
                }
                else if (Mathf.Abs(camFollowingPlayer.mainBodyChunk.pos.x - self.rightEye.position.x) < 80f)
                {
                    flag2 = true;
                }
            }
            HUD.TextPrompt textPrompt = self.room.game.cameras[cam].hud.textPrompt;
            if (textPrompt.messages.Count > 0)
            {
                if (flag && textPrompt.messages[cam].text == "A relic from the roots, read in a memory")
                {
                    textPrompt.messages[0].time = 20;
                }
                if (flag2 && textPrompt.messages[cam].text == "The other plane, communed with a shape")
                {
                    textPrompt.messages[0].time = 20;
                }
                if (flag3 && textPrompt.messages[cam].text == "Along the path of a ring, as it was done before")
                {
                    textPrompt.messages[0].time = 20;
                }
            }
            else
            {
                if (flag)
                {
                    textPrompt.AddMessage("A relic from the roots, read in a memory", 20, 20, darken: false, hideHud: true);
                }
                if (flag2)
                {
                    textPrompt.AddMessage("The other plane, communed with a shape", 20, 20, darken: false, hideHud: true);
                }
                if (flag3)
                {
                    textPrompt.AddMessage("Along the path of a ring, as it was done before", 20, 20, darken: false, hideHud: true);
                }
            }
        }

        public static void WRSA_WEAVER_ctor(On.Watcher.WatcherRoomSpecificScript.WRSA_WEAVER.orig_ctor orig, WatcherRoomSpecificScript.WRSA_WEAVER self, Room room)
        {
            if (room?.abstractRoom != null && room.game != null && room.game.IsStorySession && room.game.StoryCharacter == LookerEnums.looker)
            {
                self.room = room;
                VoidWeaverEndingScene obj = new VoidWeaverEndingScene(room);
                room.AddObject(obj);
                room.abstractRoom.mapPos = Vector2.zero;
                {
                    OmniDirectionalSound item = new OmniDirectionalSound("RWTW_PlateTree_A1.ogg", inherited: false)
                    {
                        volume = 0.2f
                    };
                    OmniDirectionalSound item2 = new OmniDirectionalSound("RWTW_PlateTree_B1.ogg", inherited: false)
                    {
                        volume = 0.35f
                    };
                    if (room.roomSettings?.ambientSounds != null)
                    {
                        room.roomSettings.ambientSounds.Add(item);
                        room.roomSettings.ambientSounds.Add(item2);
                    }
                    if (room.game.cameras != null && room.game.cameras.Length > 0 && room.game.cameras[0].virtualMicrophone != null)
                    {
                        room.game.cameras[0].virtualMicrophone.NewRoom(room);
                    }
                }
                room.AddObject(new FadeOut(room, UnityEngine.Color.black, 10f, fadeIn: true));
                MusicPlayer musicPlayer = room.game.manager.musicPlayer;
                musicPlayer?.FadeOutAllSongs(120f);
                return;
            }
            orig(self, room);
        }

        public static void WatcherRoomSpecificScript_AddRoomSpecificScript(On.Watcher.WatcherRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);
            if (!room.game.IsStorySession || room.game.StoryCharacter != LookerEnums.looker)
            {
                return;
            }
            string name = room.abstractRoom.name;
            if (name == "WORA_DIAL")
            {
                room.AddObject(new WatcherRoomSpecificScript.WORA_RippleDial(room));
            }
            if (name == "WORA_EGG" && !SaveFileCode.GetBool(room.game.GetStorySession.saveState, "DialComplete"))
            {
                room.AddObject(new WatcherRoomSpecificScript.WORA_ElderSpawn(room));
            }
            if (name == "WRSA_J01")
            {
                room.AddObject(new WatcherRoomSpecificScript.WRSA_J01(room));
            }
            if (name == "WRSA_WEAVER")
            {
                room.AddObject(new WatcherRoomSpecificScript.WRSA_WEAVER(room));
            }
            if (name == "WRSA_L01")
            {
                room.AddObject(new WatcherRoomSpecificScript.WRSA_L01(room));
            }
            if (name == "WORA_DIAL" && room.abstractRoom.firstTimeRealized && room.game.GetStorySession.saveState.denPosition == "WORA_DIAL")
            {
                room.AddObject(new WatcherRoomSpecificScript. HI_W05(room));
            }
            if (name == "WAUA_TOYS" && room.game.GetStorySession.saveState.denPosition == "WAUA_TOYS")
            {
                room.AddObject(new WatcherRoomSpecificScript.WAUA_TOYS(room));
            }
        }

        public static void ShowTutorial(Room room)
        {
            string tutorial = null;
            string tutorial2 = null;
            switch (Plugin.delayedTutorial)
            {
                case "WARC":
                    {
                        tutorial = "Controls are randomised each time you go through a pipe"; tutorial2 = "Effects of mushrooms and Basilisks can temporarily disable this effect"; break;
                    }
                case "WSKB":
                    {
                        tutorial = "Darkness steadily grows over time"; tutorial2 = "Avoid being in complete darkness at all cost"; break;
                    }
                case "WSKA":
                    {
                        tutorial = "Rain seems to approach faster here"; break;
                    }
                case "WPTA":
                    {
                        tutorial = "You are now officially on life support by vulture grub signal"; tutorial2 = "Getting the signal to end will result in death"; break;
                    }
                case "WARA":
                    {
                        tutorial = "This is the final stretch."; tutorial2 = "Good luck!"; break;
                    }
                case "LINK1":
                    {
                        tutorial = "Opening portals also causes the world to change"; tutorial2 = "15 left"; break;
                    }
                case "LINK2":
                    {
                        tutorial = "Increased portal activity will attract new creatures"; tutorial2 = "10 left"; break;
                    }
                case "LINK3":
                    {
                        tutorial = "Only five more remaining"; break;
                    }
            }
            if (tutorial != null) room.game.cameras[0].hud.textPrompt.AddMessage(tutorial, 80, 160, darken: true, hideHud: true);
            if (tutorial2 != null) room.game.cameras[0].hud.textPrompt.AddMessage(tutorial2, 80, 160, darken: true, hideHud: true);
        }



        public static void WarpPoint_ChangeState(On.Watcher.WarpPoint.orig_ChangeState orig, WarpPoint self, WarpPoint.State nextState)
        {
            orig(self, nextState);
            if (self?.room?.game?.StoryCharacter != LookerEnums.looker || self.room.world?.region == null)
            {
                return;
            }
            if (self.currentState == WarpPoint.State.EnterWarp)
            {
                if (self.Data.rippleWarp)
                {
                    Plugin.Log.LogMessage("Linking region! " + self.room.world.region.name);
                    SaveFileCode.LinkRegion(self.room.game.GetStorySession.saveState, self.room.world.region.name);
                }
            }
            if (self.currentState == WarpPoint.State.SpawnItems)
            {
                if (Plugin.delayedTutorial != null && SaveFileCode.LinkCount(self.room.game.GetStorySession.saveState) % 5 == 0)
                {
                    self.room.game.GetStorySession.saveState.SetBool(SaveFileCode.daemonTutorialDone, true);
                    Plugin.delayedTutorial = "LINK" + SaveFileCode.LinkCount(self.room.game.GetStorySession.saveState) / 5;
                }
                if (SaveFileCode.NewRegion(self.room.game.GetStorySession.saveState, self.room.world.region.name))
                {
                    Plugin.Log.LogMessage("New region! " + self.room.world.region.name);
                    Plugin.delayedTutorial = self.room.world.region.name;
                }
            }
        }

        public static void WORA_ElderSpawn_Update(On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.orig_Update orig, WatcherRoomSpecificScript.WORA_ElderSpawn self, bool eu)
        {
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (self.ambientCooldown <= 0)
                {
                    self.SpawnAmbient();
                    self.ambientCooldown = self.ambientSpawn.Count * 2;
                }
                else if (self.ambientSpawn.Count < self.maxSpawn)
                {
                    self.ambientCooldown--;
                }
                if (self.room.game.devToolsActive && Input.GetKey(KeyCode.N) && self.elder == null)
                {
                    self.SpawnElder();
                    self.elderSpotted = true;
                    self.maxSpawn = 100;
                    self.ambientCooldown = 60;
                }
                for (int num = self.ambientSpawn.Count - 1; num >= 0; num--)
                {
                    if (self.ambientSpawn[num].slatedForDeletetion)
                    {
                        self.ambientSpawn.RemoveAt(num);
                    }
                }
                if (self.elder != null && self.elder.slatedForDeletetion)
                {
                    self.elder = null;
                }
                if (!self.elderSpotted)
                {
                    if (self.room.world.regionState.rippleSpawnEggState.OuterRimDialFilled())
                    {
                        self.ElderTimerUpdate();
                    }
                    return;
                }
                if (self.room.warpPoints.Count > 0)
                {
                    return;
                }
                if (timeUntilFloatEnds > 0)
                {
                    timeUntilFloatEnds--;
                    if (timeUntilFloatEnds == 0)
                    {
                        self.room.game.cameras[0].hud.textPrompt.AddMessage("Press SPECIAL to stun yourself and fake death", 120, 160, darken: true, hideHud: true);
                        self.room.game.cameras[0].hud.textPrompt.AddMessage("While stunned this way, you count as invisible", 120, 160, darken: true, hideHud: true);
                    }
                }
                foreach (Player player in self.room.PlayersInRoom)
                {
                    if (self.elder != null && player.graphicsModule is PlayerGraphics playerGraphics)
                    {
                        playerGraphics.LookAtObject(self.elder);
                    }
                    if (!(Vector2.Distance(player.mainBodyChunk.pos, WatcherRoomSpecificScript.WORA_ElderSpawn.orbitCenter) < 200f))
                    {
                        continue;
                    }
                    if (Vector2.Distance(player.mainBodyChunk.pos, WatcherRoomSpecificScript.WORA_ElderSpawn.orbitCenter) < 200f)
                    {
                        bool flag = player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.WallClimb && player.bodyMode != Player.BodyModeIndex.Stand;
                        if (self.playerLevitationTime < (flag ? int.MaxValue : 60))
                        {
                            self.playerLevitationTime++;
                        }
                        if (self.playerLevitationTime > 120 && self.elder?.behavior is WatcherRoomSpecificScript.WORA_ElderSpawn.ElderBehavior elderBehavior)
                        {
                            if (!elderBehavior.closeIn)
                            {
                                elderBehavior.StartCloseIn();
                                self.absorption.SlowStart();
                            }
                            else if (elderBehavior.orbitAngle < elderBehavior.collapseAnimFinish && !player.specialRippleEggWarpPermission)
                            {
                                player.specialRippleEggWarpPermission = true;
                                self.holdTutorial = true;
                                self.elder.startFadeOut = true;
                                SaveFileCode.SetBool(self.room.game.GetStorySession.saveState, "DialComplete", true);
                                timeUntilFloatEnds = 120;
                            }
                            if (elderBehavior.closeIn && elderBehavior.orbitAngle < elderBehavior.collapseAnimStart - 20f)
                            {
                                self.absorption.Start();
                                player.watcherInstability = 0.3f;
                                if (elderBehavior.orbitAngle > elderBehavior.collapseAnimStart - 100f)
                                {
                                    player.Blink(10);
                                }
                                else if (elderBehavior.orbitAngle > elderBehavior.collapseAnimFinish - 80f)
                                {
                                    player.Stun(10);
                                }
                                else if (elderBehavior.orbitAngle > elderBehavior.collapseAnimFinish - 400f)
                                {
                                    int num2 = (int)Custom.LerpMap(elderBehavior.orbitAngle, elderBehavior.collapseAnimFinish - 80f, elderBehavior.collapseAnimFinish - 400f, 8f, 30f);
                                    if (Mathf.Abs(elderBehavior.orbitAngle) % (float)num2 == 0f)
                                    {
                                        player.Blink(5);
                                    }
                                    self.absorption.End();
                                    player.watcherInstability = 0f;
                                }
                            }
                        }
                        if (flag && timeUntilFloatEnds != 0)
                        {
                            player.levitationActive = true;
                            player.TickLevitation(levitateUp: false, self.playerLevitationTime, 5f);
                            player.levitationActive = false;
                            Vector2 vector = WatcherRoomSpecificScript.WORA_ElderSpawn.orbitCenter + new Vector2(0f, Mathf.Abs(Mathf.Sin((float)self.playerLevitationTime / 40f) * 20f) - 10f);
                            Vector2 a = Custom.DirVec(player.mainBodyChunk.pos, vector);
                            a = Vector2.Lerp(a, Vector2.up, Mathf.InverseLerp(30f, 10f, Vector2.Distance(player.mainBodyChunk.pos, vector)));
                            float num3 = Custom.LerpMap(self.playerLevitationTime, 0f, 120f, 0.5f, 1.5f);
                            for (int i = 0; i < player.bodyChunks.Length; i++)
                            {
                                player.bodyChunks[i].vel *= 0.3f;
                                player.bodyChunks[i].vel += a * (Mathf.Abs(Mathf.Sin((float)self.playerLevitationTime / 40f)) * num3);
                            }
                        }
                    }
                    else
                    {
                        self.playerLevitationTime--;
                    }
                    player.customPlayerGravity = Custom.LerpMap(self.playerLevitationTime, 0f, 120f, 0.7f, 0.1f, 0.5f);
                }
            }
            else
            {
                orig(self, eu);
            }
        }

        public static void SleepAndDeathScreen_AddPassageButton(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
        {
            if (self?.saveState != null && self.saveState.saveStateNumber == LookerEnums.looker)
            {
                return;
            }
            orig(self, buttonBlack);
        }

        public static void KarmaLadder_AddEndgameMeters(On.Menu.KarmaLadder.orig_AddEndgameMeters orig, Menu.KarmaLadder self)
        {
            if (self.menu is Menu.SleepAndDeathScreen && SaveFileCode.GetBool((self.menu as Menu.SleepAndDeathScreen).saveState, "DialComplete"))
            {
                self.endGameMeters = new List<Menu.EndgameMeter>();
                if ((self.menu as Menu.KarmaLadderScreen).winState != null)
                {
                    if (self.menu is Menu.SleepAndDeathScreen && (self.menu as Menu.SleepAndDeathScreen).IsAnyDeath)
                    {
                        (self.menu as Menu.KarmaLadderScreen).winState.PlayerDied();
                    }
                    List<WinState.EndgameTracker> list = new();

                    WinState.EndgameTracker endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.Survivor, null);
                    list.Add(endgameTracker);
                    endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.Hunter, null);
                    list.Add(endgameTracker);
                    endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.Saint, null);
                    list.Add(endgameTracker);
                    endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.Outlaw, null);
                    list.Add(endgameTracker);
                    endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.DragonSlayer, null);
                    list.Add(endgameTracker);
                    endgameTracker = WinState.CreateAndAddTracker(WinState.EndgameID.Monk, null);
                    list.Add(endgameTracker);
                    int count = list.Count;
                    int num = 0;
                    for (int j = 0; j < 2; j++)
                    {
                        int num2 = count / 2;
                        if (count % 2 == 1 && j == 1)
                        {
                            num2++;
                        }
                        float num3 = 180f / (float)(num2 + 1);
                        for (int k = 0; k < num2; k++)
                        {
                            float ang = num3 * ((float)k + 1f) + ((j == 0) ? 0f : 180f);
                            self.endGameMeters.Add(new Menu.EndgameMeter(self.menu, self, Custom.DegToVec(ang) * 225f, list[num], self.containers[self.BackgroundContainer], self.containers[self.MainContainer]));
                            num++;
                        }
                    }
                }
                for (int l = 0; l < self.endGameMeters.Count; l++)
                {
                    self.subObjects.Add(self.endGameMeters[l]);
                }
                return;
            }
            else orig(self);
        }

        public static void WRSA_L01_Update(On.Watcher.WatcherRoomSpecificScript.WRSA_L01.orig_Update orig, WatcherRoomSpecificScript.WRSA_L01 self, bool eu)
        {
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker && SaveFileCode.GetBool(self.room.game.GetStorySession.saveState, "PuzzleComplete"))
            {
                PlacedObject placedObject = new(PlacedObject.Type.WarpPoint, null)
                {
                    pos = new Vector2(1520f, 3590f)
                };
                (placedObject.data as WarpPoint.WarpPointData).oneWay = true;
                (placedObject.data as WarpPoint.WarpPointData).destRoom = "WRSA_TREE02";
                (placedObject.data as WarpPoint.WarpPointData).destPos = new Vector2(500f, 1000f);
                (placedObject.data as WarpPoint.WarpPointData).destCam = WarpPoint.GetDestCam((placedObject.data as WarpPoint.WarpPointData));
                self.room.TrySpawnWarpPoint(placedObject, saveInRegionState: false);
                Log.LogMessage("Warp point to puzzle ending spawned");
                self.Destroy();
                return;
            }
            else
            {
                orig(self, eu);
                return;
            }
        }

        
    }
}
