using BepInEx;
using BepInEx.Logging;
using LizardCosmetics;
using Looker.CWTs;
using Looker.Regions;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Permissions;
using UnityEngine;
using Watcher;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace Looker
{
    [BepInDependency("lsfUtils")]
    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("io.github.dual.fisobs")]
    [BepInPlugin("invedwatcher", "The Looker", "0.5")]

    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }

        public const string ogsculeSprite = "atlases/ogscule";
        public const string lookerRippleSmall = "atlases/lookerRippleSmall";
        public const string lookerRippleBig = "atlases/lookerRippleBig";
        public const string templarMaskIcon = "atlases/templarMaskIcon";

        public static int MaxRippleDuration()
        {
            return (int)(800 * OptionsMenu.ripplespaceDuration.Value);
        }

        public static int MaxSignalLeniency()
        {
            return (int) (400 * OptionsMenu.broadcastingLeniencyTimer.Value);
        }
        public static class LookerEnums
        {
            public static void RegisterValues()
            {
                vineboom = new SoundID("vineboom", true);
                looker = new SlugcatStats.Name("looker");
                lookerTimeline = new SlugcatStats.Timeline("looker");
                meetLooker = new SSOracleBehavior.Action("meetLooker", true);
                lookerConversation = new Conversation.ID("lookerConversation", true);
                lookerSubBehaviour = new SSOracleBehavior.SubBehavior.SubBehavID("lookerSubBehaviour", true);
            }
            public static void UnregisterValues()
            {
                Unregister(vineboom);
                Unregister(looker);
                Unregister(meetLooker);
                Unregister(lookerConversation);
                Unregister(lookerSubBehaviour);
            }
            private static void Unregister<T>(ExtEnum<T> extEnum) where T : ExtEnum<T>
            {
                extEnum?.Unregister();
            }
            public static SoundID vineboom;
            public static SSOracleBehavior.Action meetLooker;
            public static SSOracleBehavior.SubBehavior.SubBehavID lookerSubBehaviour;
            public static Conversation.ID lookerConversation;
            public static SlugcatStats.Name looker;
            public static SlugcatStats.Timeline lookerTimeline;
        }

        public static OptionsMenu optionsMenuInstance;
        public bool initialized;
        public bool isInit;

        public static int timeUntilFloatEnds = -1;
        public static int puzzleInput = 0;
        public static bool warptodaemon = false;
        public static float darknessProgress;
        public static bool retractDarkness = false;
        public static string delayedTutorial = null;
        public static bool shownPopupMenu = false;

        public static readonly Color BoxWormColor = new(0.63f, 0.5f, 0.5f);
        public static readonly EntityID SpecialId = new(1, -50);

        public static bool CheckMechanics(Room room, string originalRegionName, string originalRegionAcronym)
        {
            if (room?.world?.region?.name == null || room.game?.StoryCharacter != LookerEnums.looker || room.abstractRoom.shelter || room.AnyWarpPointBeingActivated || OptionsMenu.devMode.Value)
            {
                return false;
            }
            return (room.world.region.name == originalRegionAcronym) ||
                (room.world.region.name == "WARA" && room.abstractRoom.subregionName != null && room.abstractRoom.subregionName.ToLowerInvariant().Contains(originalRegionName));
        }

        public static bool CheckMechanics(RainWorldGame game, string originalRegionName, string originalRegionAcronym)
        {
            if (game?.world?.name == null || game.cameras != null || game.cameras.Length > 0 || game.cameras[0].room != null || game.StoryCharacter != LookerEnums.looker || OptionsMenu.devMode.Value)
            {
                return false;
            }
            return (game.world.name == originalRegionAcronym) ||
                (game.world.name == "WARA" && game.cameras[0].room.abstractRoom?.subregionName != null && game.cameras[0].room.abstractRoom.subregionName.ToLowerInvariant().Contains(originalRegionName));
        }

        private void LoadResources(RainWorld rainWorld)
        {

        }


        public void OnEnable()
        {
            Debug.Log("Starting Looker");
            try
            {
                Log = Logger;
                On.RainWorld.OnModsInit += RainWorld_OnModsInit;


                // player and flower mechanics
                {
                    On.Player.Update += LPlayer_Flower.Player_Update;
                    On.Player.Die += LPlayer_Flower.Player_Die;
                    On.Player.SpitOutOfShortCut += LPlayer_Flower.Player_SpitOutOfShortCut;

                    On.PlayerGraphics.InitiateSprites += LPlayer_Flower.PlayerGraphics_InitiateSprites;

                    On.Room.MaterializeRippleSpawn += LPlayer_Flower.Room_MaterializeRippleSpawn;
                    On.DaddyCorruption.SentientRotMode += LMisc.DaddyCorruption_SentientRotMode;

                    On.ARKillRect.Update += LProgression.ARKillRect_Update;

                    On.Watcher.KarmaFlowerPatch.ApplyPalette += LPlayer_Flower.KarmaFlowerPatch_ApplyPalette;
                    On.Watcher.KarmaFlowerPatch.DrawSprites += LPlayer_Flower.KarmaFlowerPatch_DrawSprites;
                    On.KarmaFlower.CanSpawnKarmaFlower += LPlayer_Flower.KarmaFlower_CanSpawnKarmaFlower;
                    On.KarmaFlower.ctor += KarmaFlower_ctor;
                    On.MoreSlugcats.SingularityBomb.ctor += LMisc.SingularityBomb_ctor;
                    On.AbstractConsumable.Consume += LPlayer_Flower.AbstractConsumable_Consume;

                    On.Player.ctor += Player_ctor;

                    On.Watcher.WarpPoint.ChooseDynamicWarpTarget += LMisc.WarpPoint_ChooseDynamicWarpTarget;

                    On.SaveState.SaveToString += LMisc.SaveState_SaveToString;
                    On.WorldLoader.GeneratePopulation += LMisc.WorldLoader_GeneratePopulation;

                    On.PlacedObject.PrinceFilterData.Active += LMisc.PrinceFilterData_Active;
                    On.Room.InitializeSentientRotPresenceInRoom += LMisc.Room_InitializeSentientRotPresenceInRoom;

                    On.Room.Loaded += LMisc.Room_Loaded;

                    On.Watcher.WarpPoint.ChangeState += LProgression.WarpPoint_ChangeState;
                    On.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += LProgression.WatcherRoomSpecificScript_AddRoomSpecificScript;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_WEAVER.ctor += LProgression.WRSA_WEAVER_ctor;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.UpdateTutorials += LProgression.WRSA_J01_UpdateTutorials;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.UpdateObjects += LProgression.WRSA_J01_UpdateObjects;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.ctor += LProgression.WRSA_J01_ctor;
                    On.Watcher.WarpPoint.CheckCanWarpToVoidWeaverEnding += LProgression.WarpPoint_CheckCanWarpToVoidWeaverEnding;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_L01.Update += LProgression.WRSA_L01_Update;

                    On.Lizard.ctor += LizardCode.Lizard_ctor;

                    On.PlayerGraphics.DrawSprites += LPlayer_Flower.PlayerGraphics_DrawSprites;
                    On.PlayerGraphics.Update += LPlayer_Flower.PlayerGraphics_Update;

                    On.HUD.KarmaMeter.ctor += KarmaMeter_ctor;
                }

                // mask and aether ridge mechanics
                {
                    On.VultureMask.ctor += LProgression.VultureMask_ctor;
                    On.MoreSlugcats.VultureMaskGraphics.DrawSprites += LProgression.VultureMaskGraphics_DrawSprites;
                    On.MoreSlugcats.VultureMaskGraphics.ctor_PhysicalObject_AbstractVultureMask_int += LProgression.VultureMaskGraphics_ctor;
                    On.MoreSlugcats.VultureMaskGraphics.ctor_PhysicalObject_MaskType_int_string += LProgression.VultureMaskGraphics_ctor_PhysicalObject_MaskType_int_string;

                    On.HUD.KarmaMeter.Update += LProgression.KarmaMeter_Update;

                    On.SaveState.GetSaveStateDenToUse += LProgression.SaveState_GetSaveStateDenToUse;
                    On.Player.ctor += LProgression.Player_ctor;

                    On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
                    On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
                    On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
                }

                // signal spires mechanics

                {
                    On.VultureGrub.AttemptCallVulture += LSignal.VultureGrub_AttemptCallVulture;
                    On.VultureGrub.Act += LSignal.VultureGrub_Act;
                    On.Player.ThrowObject += LSignal.Player_ThrowObject;
                    On.VultureGrub.Violence += LSignal.VultureGrub_Violence;
                }

                // sunlit port and badlands mechanics

                {
                    On.RoomCamera.Update += LSunlit_Badlands.RoomCamera_Update;

                    On.Lantern.Update += LSunlit_Badlands.Lantern_Update;
                    On.LanternStick.Update += LSunlit_Badlands.LanternStick_Update;

                    On.ScavengerAbstractAI.InitGearUp += LSunlit_Badlands.ScavengerAbstractAI_InitGearUp;

                    On.LightSource.Update += LightSource_Update;
                }

                // coral caves and migration path mechanics

                {
                    On.Room.Update += LCoral_Migration.Room_Update;
                    On.Watcher.Barnacle.Collide += LCoral_Migration.Barnacle_Collide;
                }

                // desolate tract mechanics

                {
                    On.Pomegranate.Update += LDesolate.Pomegranate_Update;
                    On.Pomegranate.EnterSmashedMode += LDesolate.Pomegranate_EnterSmashedMode;
                    On.Pomegranate.TerrainImpact += LDesolate.Pomegranate_TerrainImpact;
                }

                // misc mechanics

                {
                    On.RainCycle.Update += LMisc.RainCycle_Update;

                    On.Player.AddFood += LMisc.Player_AddFood;
                    On.Player.checkInput += LMisc.Player_checkInput;

                    On.Watcher.Frog.Attach += LMisc.Frog_Attach;

                    On.Watcher.Angler.Update += LMisc.Angler_Update;

                    On.Watcher.LightningMaker.StrikeAOE.ctor += LMisc.StrikeAOE_ctor;

                    On.Watcher.BoxWormGraphics.BaseColor_AbstractRoom += LMisc.BoxWormGraphics_BaseColor_AbstractRoom;
                    On.Watcher.BoxWormGraphics.BaseColor_Room += LMisc.BoxWormGraphics_BaseColor_Room;
                }

                // arg ending
                {
                    On.Menu.MainMenu.Update += LProgression.MainMenu_Update;
                    On.Menu.KarmaLadder.AddEndgameMeters += LProgression.KarmaLadder_AddEndgameMeters;
                    On.Menu.SleepAndDeathScreen.AddPassageButton += LProgression.SleepAndDeathScreen_AddPassageButton;
                    On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update += LProgression.WORA_ElderSpawn_Update;
                }

                // room scripts
                {
                    On.Watcher.WatcherRoomSpecificScript.HI_W05.Update += LProgression.HI_W05_Update;
                    On.Watcher.WatcherRoomSpecificScript.HI_W05.ctor += LProgression.HI_W05_ctor;
                }

                // iterators
                {
                    On.SSOracleBehavior.NewAction += NothingToSeeHere.SSOracleBehavior_NewAction;
                    On.SSOracleBehavior.PebblesConversation.AddEvents += NothingToSeeHere.PebblesConversation_AddEvents;
                    On.SSOracleBehavior.SpecialEvent += NothingToSeeHere.SSOracleBehavior_SpecialEvent;
                    On.Oracle.ctor += Oracle_ctor;
                    On.Room.ReadyForAI += Room_ReadyForAI;

                    On.SLOracleBehaviorHasMark.NameForPlayer += NothingToSeeHere.SLOracleBehaviorHasMark_NameForPlayer;
                }

                // pillar grove
                {
                    On.HUD.Map.Draw += LGrove.Map_Draw;
                    On.HUD.Map.ItemMarker.Draw += LGrove.ItemMarker_Draw;
                    On.RoomCamera.SpriteLeaser.Update += LGrove.SpriteLeaser_Update;
                }

                // world setup
                {
                    On.Room.InfectRoomWithSentientRot += Room_InfectRoomWithSentientRot;
                    On.RegionState.InfectRegionRoomWithSentientRot += RegionState_InfectRegionRoomWithSentientRot;
                }

                // unorganised
                {
                    On.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPoint_NewWorldLoaded_Room;


                    On.SaveState.LoadGame += SaveFileCode.SaveState_LoadGame;

                    On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += LProgression.SlugcatPageNewGame_ctor;
                    On.Menu.SlugcatSelectMenu.Singal += LProgression.SlugcatSelectMenu_Singal;

                    On.Watcher.LethalThunderStorm.GetLethalDelay += LethalThunderStorm_GetLethalDelay;

                    On.AntiGravity.BrokenAntiGravity.Update += BrokenAntiGravity_Update;

                    On.Watcher.LightningMaker.StaticBuildup.GetBestTarget += StaticBuildup_GetBestTarget;

                    On.Player.LungUpdate += Player_LungUpdate;

                    On.Watcher.WarpPoint.PerformWarp += WarpPoint_PerformWarp;

                    On.RainCycle.GetDesiredCycleLength += RainCycle_GetDesiredCycleLength;

                    On.VultureMask.Update += VultureMask_Update;

                    On.Player.RippleSpawnInteractions += Player_RippleSpawnInteractions;
                }

                // manual hooks
                {
                    new Hook(typeof(Menu.KarmaLadderScreen).GetProperty(nameof(Menu.KarmaLadderScreen.RippleLadderMode)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.RippleLadderMode)));

                    new Hook(typeof(RegionGate).GetProperty(nameof(RegionGate.MeetRequirement))!.GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Meet_Requirement)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.OutsideWatcherCampaign)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Outside_Watcher)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.rippleLevel)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.PlayerRippleLevel)));
                    new Hook(typeof(Player).GetProperty(nameof(Player.maxRippleLevel)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.PlayerMaxRippleLevel)));

                    new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.CanSee_VoidSpawn)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.VisibilityBonus)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Visibility_Bonus)));
                    //new Hook(typeof(Player).GetProperty(nameof(Player.gravity)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.OverrideGravity)));
                    //new Hook(typeof(Player).GetProperty(nameof(Player.airFriction)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.OverrideAirFriction)));

                    new Hook(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsStraw)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Is_Sliver)));
                    new Hook(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPebbles)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Is_EP)));

                    new Hook(typeof(Oracle).GetProperty(nameof(Oracle.Alive)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.Is_Alive)));

                    new Hook(typeof(Menu.KarmaLadderScreen).GetProperty(nameof(Menu.KarmaLadderScreen.UsesWarpMap)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.UsesWarpMap)));
                }

                if (isInit)
                    return;
                isInit = true;

                WorldLoader.Preprocessing.preprocessorConditions.Add(LookerConditionsClass.LookerConditions);

                Logger.LogMessage("LOOKER HOOKS SUCESS");
            }
            catch (Exception e)
            {
                Logger.LogMessage("Looker hooks failed!!!");
                Logger.LogError(e);
            }
        }

        public static void KarmaFlower_ctor(On.KarmaFlower.orig_ctor orig, KarmaFlower self, AbstractPhysicalObject abstractPhysicalObject)
        {
            orig(self, abstractPhysicalObject);
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

        private void Player_RippleSpawnInteractions(On.Player.orig_RippleSpawnInteractions orig, Player self)
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

        public float OverrideGravity(Func<Player, float> orig,  Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker && PlayerCWT.TryGetData(self, out var data) && data.shouldOverrideGravity && self.room != null)
            {
                return data.overrideAirfriction;
            }
            return orig(self);
        }

        public float OverrideAirFriction(Func<Player, float> orig, Player self)
        {
            if (self?.SlugCatClass == LookerEnums.looker && PlayerCWT.TryGetData(self, out var data) && data.shouldOverrideGravity && self.room != null)
            {
                return data.overrideGravity * self.room.gravity;
            }
            return orig(self);
        }

        private void VultureMask_Update(On.VultureMask.orig_Update orig, VultureMask self, bool eu)
        {
            orig(self, eu);
            if (self != null && CWTs.VultureMaskCWT.TryGetData(self, out var data) && data.isKarmaMask)
            {
                if (data.lightSource == null)
                {
                    data.lightSource = new LightSource(self.firstChunk.pos, environmentalLight: false, RainWorld.GoldRGB, self)
                    {
                        affectedByPaletteDarkness = 0.5f
                    };
                    self.room.AddObject(data.lightSource);
                }
                else
                {
                    data.lightSource.setPos = self.firstChunk.pos;
                    data.lightSource.setRad = 100f;
                    data.lightSource.setAlpha = 1f;
                    if (data.lightSource.slatedForDeletetion || data.lightSource.room != self.room)
                    {
                        data.lightSource = null;
                    }
                }
            }
        }

        public static bool UsesWarpMap(Func<Menu.KarmaLadderScreen, bool> orig, Menu.KarmaLadderScreen self)
        {
            if (self.saveState != null && self.saveState.saveStateNumber == LookerEnums.looker)
            {
                return !self.RippleLadderMode;
            }
            return orig(self);
        }

        private int RainCycle_GetDesiredCycleLength(On.RainCycle.orig_GetDesiredCycleLength orig, RainCycle self)
        {
            int value = orig(self);

            if (self?.world?.game?.StoryCharacter != null)
            {
                if (self.world.game.StoryCharacter == LookerEnums.looker)
                {
                    if (self?.world?.region?.name != null && self.world.region.name == "WSKA")
                    {
                        value = (int)((float)value * 0.30);
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

        private void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, WarpPoint self)
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

        public static void LightSource_Update(On.LightSource.orig_Update orig, LightSource self, bool eu)
        {
            orig(self, eu);
            if (!CheckMechanics(self?.room, "alley", "WSKB"))
            {
                return;
            }
            if (!self.noGameplayImpact && !self.slatedForDeletetion && self?.Pos != null && self.tiedToObject != null && self.tiedToObject is PhysicalObject)
            {
                if (self.room.PlayersInRoom != null && self.room.PlayersInRoom.Count > 0)
                {
                    foreach (Player player in self.room.PlayersInRoom)
                    {
                        if (player?.bodyChunks == null || player.bodyChunks.Length == 0 || !Custom.DistLess(player.mainBodyChunk.pos, self.Pos, 100))
                        {
                            continue;
                        }
                        if (PlayerCWT.TryGetData(player, out var data))
                        {
                            data.darknessImmunity = 120;
                        }
                        else
                        {
                            Log.LogMessage("Couldnt find playerCWT!");
                        }
                    }
                }
            }
        }

        private Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.VultureMask && intData == 50)
            {
                return RainWorld.GoldRGB;
            }
            return orig(itemType, intData);
        }

        private string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.VultureMask && intData == 50)
            {
                return templarMaskIcon;
            }
            return orig(itemType, intData);
        }

        private IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            IconSymbol.IconSymbolData? value = orig(item);
            if (item.type == AbstractPhysicalObject.AbstractObjectType.VultureMask && item.ID == SpecialId)
            {
                return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, 50);
            }
            return value;
        }

        public static void KarmaMeter_ctor(On.HUD.KarmaMeter.orig_ctor orig, HUD.KarmaMeter self, HUD.HUD hud, FContainer fContainer, IntVector2 displayKarma, bool showAsReinforced)
        {
            orig(self, hud, fContainer, displayKarma, showAsReinforced);
            if (self?.karmaSprite != null && hud?.owner != null && hud.owner is Player player && player?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (UnityEngine.Random.value < 0.05f)
                {
                    self.karmaSprite.element = Futile.atlasManager.GetElementWithName(lookerRippleSmall);
                }
            }
        }

        private void Player_LungUpdate(On.Player.orig_LungUpdate orig, Player self)
        {
            orig(self);
            if (!CWTs.PlayerCWT.TryGetData(self, out var data)) return;
            if (self.room != null && CheckMechanics(self.room, "salination", "WARB") && self.airInLungs <= 0.1f && !data.usedEmergencyBreath)
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

        private int LethalThunderStorm_GetLethalDelay(On.Watcher.LethalThunderStorm.orig_GetLethalDelay orig, LethalThunderStorm self, float amount)
        {
            int value = orig(self, amount);
            if (self?.room != null && CheckMechanics(self.room, "stormy", "WSKC"))
            {
                return (int)(value / OptionsMenu.lightningSpawnSpeed.Value);
            }
            return value;
        }

        private Vector2 StaticBuildup_GetBestTarget(On.Watcher.LightningMaker.StaticBuildup.orig_GetBestTarget orig, LightningMaker.StaticBuildup self)
        {
            Vector2 value = orig(self);
            if (OptionsMenu.lessEvilLightnings.Value)
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

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self?.SlugCatClass != null && self.SlugCatClass == LookerEnums.looker)
            {
                self.glowing = OptionsMenu.enableGlow.Value;
            }
        }

        private void BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity self)
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

        private void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            Log.LogMessage("Spawning oracle!!");
            if (room.abstractRoom?.name.ToLowerInvariant() == "wssr_ai")
            {
                self.ID = Oracle.OracleID.SS;
            }
            orig(self, abstractPhysicalObject, room);
        }

        private void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            Log.LogMessage("Ready for AI!");
            if (self.abstractRoom?.name.ToLowerInvariant() == "wssr_ai")
            {
                if (self.game?.StoryCharacter == null)
                {
                    Log.LogMessage("Fail 1!");
                    return;
                }
                if (self.game.StoryCharacter != LookerEnums.looker)
                {
                    Log.LogMessage("Fail 2!");
                    return;
                }
                if (self.world == null)
                {
                    Log.LogMessage("Fail 3!");
                    return;
                }
                Log.LogMessage("Ready for AI 2 !");
                Oracle obj3 = new(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self);
                self.AddObject(obj3);
                self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
                Log.LogMessage("Ready for AI 3 !");
            }
        }

        public static bool Is_Alive(Func<Oracle, bool> orig, Oracle self)
        {
            return orig(self) || (self.ID == Oracle.OracleID.SS && self?.room?.game?.StoryCharacter == LookerEnums.looker && OptionsMenu.metSliver.Value);
        }

        public static bool Is_Sliver(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            if (self?.oracle?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return true;
            }
            return orig(self);
        }

        public static bool Is_EP(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            if (self?.oracle?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self);
        }

        public static void ResetModData()
        {
            darknessProgress = 0;
            retractDarkness = false;
            timeUntilFloatEnds = -1;
            shownPopupMenu = false;
        }


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

        public static bool RippleLadderMode(Func<Menu.KarmaLadderScreen, bool> orig, Menu.KarmaLadderScreen self)
        {
            if (self.saveState?.saveStateNumber == LookerEnums.looker)
            {
                return false;
            }
            return orig(self);
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

        public static bool Meet_Requirement(Func<RegionGate, bool> orig, RegionGate self)
        {
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (self.room.PlayersInRoom.Count <= 0) return orig(self);

                AbstractCreature firstAlivePlayer = self.room.game.FirstAlivePlayer;
                if (self.room.game.Players.Count == 0 || firstAlivePlayer == null || (firstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable))
                {
                    return false;
                }
                foreach (Player player in self.room.PlayersInRoom)
                {
                    if (player?.grasps != null && player.grasps.Length != 0)
                    {
                        foreach (Creature.Grasp t in player.grasps)
                        {
                            if (t.grabbed is VultureMask mask && CWTs.VultureMaskCWT.TryGetData(mask, out var data) && data.isKarmaMask)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            return orig(self);
        }
        public static bool Outside_Watcher(Func<Player, bool> orig, Player self)
        {
            return orig(self) || (self?.slugcatStats?.name == LookerEnums.looker);
        }

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
        public void OnDisable()
        {
            if (!isInit)
                return;
            isInit = false;

            WorldLoader.Preprocessing.preprocessorConditions.Remove(LookerConditionsClass.LookerConditions);
        }
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (initialized)
            {
                return;
            }
            initialized = true;
            optionsMenuInstance = new OptionsMenu(this);
            try
            {
                MachineConnector.SetRegisteredOI("invedwatcher", optionsMenuInstance);
            }
            catch (Exception ex)
            {
                Debug.Log($"The Looker: Hook_OnModsInit options failed init error {optionsMenuInstance}{ex}");
                Logger.LogError(ex);
            }
            LookerEnums.RegisterValues();
            Futile.atlasManager.LoadImage(ogsculeSprite);
            Futile.atlasManager.LoadImage(lookerRippleBig);
            Futile.atlasManager.LoadImage(lookerRippleSmall);
            Futile.atlasManager.LoadImage(templarMaskIcon);
        }
        
    }
}