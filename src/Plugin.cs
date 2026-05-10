using BepInEx;
using BepInEx.Logging;
using LizardCosmetics;
using Looker.CWTs;
using Looker.Regions;
using Menu;
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
        public const string ogsculeIcon = "atlases/ogsculeIcon";
        public const string lookerRippleSmall = "atlases/lookerRippleSmall";
        public const string lookerRippleBig = "atlases/lookerRippleBig";
        public const string templarMaskIcon = "atlases/templarMaskIcon";
        public const string lookerIntroRoll = "illustrations/intro_roll_c_looker";

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
                    On.MoreSlugcats.SingularityBomb.ctor += LMisc.SingularityBomb_ctor;
                    On.AbstractConsumable.Consume += LPlayer_Flower.AbstractConsumable_Consume;
                    On.Player.ctor += LPlayer_Flower.Player_ctor;

                    On.PlayerGraphics.DrawSprites += LPlayer_Flower.PlayerGraphics_DrawSprites;
                    On.PlayerGraphics.Update += LPlayer_Flower.PlayerGraphics_Update;

                    
                }

                // progression
                {
                    On.Watcher.WarpPoint.ChangeState += LProgression.WarpPoint_ChangeState;
                    On.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += LProgression.WatcherRoomSpecificScript_AddRoomSpecificScript;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_WEAVER.ctor += LProgression.WRSA_WEAVER_ctor;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.UpdateTutorials += LProgression.WRSA_J01_UpdateTutorials;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.UpdateObjects += LProgression.WRSA_J01_UpdateObjects;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_J01.ctor += LProgression.WRSA_J01_ctor;
                    On.Watcher.WarpPoint.CheckCanWarpToVoidWeaverEnding += LProgression.WarpPoint_CheckCanWarpToVoidWeaverEnding;
                    On.Watcher.WatcherRoomSpecificScript.WRSA_L01.Update += LProgression.WRSA_L01_Update;

                    On.Watcher.WarpPoint.NewWorldLoaded_Room += LProgression.WarpPoint_NewWorldLoaded_Room;
                    On.Watcher.WarpPoint.PerformWarp += LProgression.WarpPoint_PerformWarp;
                }

                // mask and aether ridge mechanics
                {
                    On.HUD.KarmaMeter.ctor += KarmaMask.KarmaMeter_ctor;

                    On.VultureMask.ctor += KarmaMask.VultureMask_ctor;
                    On.MoreSlugcats.VultureMaskGraphics.DrawSprites += KarmaMask.VultureMaskGraphics_DrawSprites;
                    On.MoreSlugcats.VultureMaskGraphics.ctor_PhysicalObject_AbstractVultureMask_int += KarmaMask.VultureMaskGraphics_ctor;
                    On.MoreSlugcats.VultureMaskGraphics.ctor_PhysicalObject_MaskType_int_string += KarmaMask.VultureMaskGraphics_ctor_PhysicalObject_MaskType_int_string;

                    On.VultureMask.Update += KarmaMask.VultureMask_Update;

                    On.HUD.KarmaMeter.Update += KarmaMask.KarmaMeter_Update;

                    On.SaveState.GetSaveStateDenToUse += KarmaMask.SaveState_GetSaveStateDenToUse;
                    On.Player.ctor += KarmaMask.Player_ctor;

                    On.ItemSymbol.SymbolDataFromItem += KarmaMask.ItemSymbol_SymbolDataFromItem;
                    On.ItemSymbol.SpriteNameForItem += KarmaMask.ItemSymbol_SpriteNameForItem;
                    On.ItemSymbol.ColorForItem += KarmaMask.ItemSymbol_ColorForItem;
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

                    On.LightSource.Update += LSunlit_Badlands.LightSource_Update;
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
                    On.Player.LungUpdate += LMisc.Player_LungUpdate;

                    On.Watcher.LightningMaker.StrikeAOE.ctor += LMisc.StrikeAOE_ctor;

                    On.Watcher.BoxWormGraphics.BaseColor_AbstractRoom += LMisc.BoxWormGraphics_BaseColor_AbstractRoom;
                    On.Watcher.BoxWormGraphics.BaseColor_Room += LMisc.BoxWormGraphics_BaseColor_Room;

                    On.Watcher.LethalThunderStorm.GetLethalDelay += LMisc.LethalThunderStorm_GetLethalDelay;
                    On.Watcher.LightningMaker.StaticBuildup.GetBestTarget += LMisc.StaticBuildup_GetBestTarget;

                    On.AntiGravity.BrokenAntiGravity.Update += LMisc.BrokenAntiGravity_Update;

                    On.Room.Loaded += LMisc.Room_Loaded;
                    On.Lizard.ctor += LMisc.Lizard_ctor;

                    On.Watcher.WarpPoint.ChooseDynamicWarpTarget += LMisc.WarpPoint_ChooseDynamicWarpTarget;

                    On.SaveState.SaveToString += LMisc.SaveState_SaveToString;
                    On.WorldLoader.GeneratePopulation += LMisc.WorldLoader_GeneratePopulation;

                    On.PlacedObject.PrinceFilterData.Active += LMisc.PrinceFilterData_Active;
                    On.Room.InitializeSentientRotPresenceInRoom += LMisc.Room_InitializeSentientRotPresenceInRoom;

                    On.RainCycle.GetDesiredCycleLength += LMisc.RainCycle_GetDesiredCycleLength;
                }

                // arg ending
                {
                    On.Menu.MainMenu.Update += LProgression.MainMenu_Update;
                    On.Menu.KarmaLadder.AddEndgameMeters += LProgression.KarmaLadder_AddEndgameMeters;
                    On.Menu.SleepAndDeathScreen.AddPassageButton += LProgression.SleepAndDeathScreen_AddPassageButton;
                    On.Watcher.WatcherRoomSpecificScript.WORA_ElderSpawn.Update += LProgression.WORA_ElderSpawn_Update;
                    On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += LProgression.SlugcatPageNewGame_ctor;
                    On.Menu.SlugcatSelectMenu.Singal += LProgression.SlugcatSelectMenu_Singal;
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
                    On.Oracle.ctor += NothingToSeeHere.Oracle_ctor;
                    On.Room.ReadyForAI += NothingToSeeHere.Room_ReadyForAI;

                    On.SLOracleBehaviorHasMark.NameForPlayer += NothingToSeeHere.SLOracleBehaviorHasMark_NameForPlayer;
                }

                // pillar grove
                {
                    On.RoomCamera.SpriteLeaser.Update += LGrove.SpriteLeaser_Update;
                    On.ItemSymbol.ColorForItem += LGrove.ItemSymbol_ColorForItem;
                    On.ItemSymbol.SpriteNameForItem += LGrove.ItemSymbol_SpriteNameForItem;
                    On.ItemSymbol.SymbolDataFromItem += LGrove.ItemSymbol_SymbolDataFromItem;
                }

                // world setup
                {
                    On.Room.InfectRoomWithSentientRot += LProgression.Room_InfectRoomWithSentientRot;
                    On.RegionState.InfectRegionRoomWithSentientRot += LProgression.RegionState_InfectRegionRoomWithSentientRot;
                }

                // unorganised
                {
                    On.SaveState.LoadGame += SaveFileCode.SaveState_LoadGame;

                    On.Player.RippleSpawnInteractions += LMigration.Player_RippleSpawnInteractions;
                    IL.Menu.IntroRoll.ctor += IntroRoll_ctor;
                }

                // manual hooks
                {
                    new Hook(typeof(Menu.KarmaLadderScreen).GetProperty(nameof(Menu.KarmaLadderScreen.RippleLadderMode)).GetGetMethod(), typeof(KarmaMask).GetMethod(nameof(KarmaMask.RippleLadderMode)));

                    new Hook(typeof(RegionGate).GetProperty(nameof(RegionGate.MeetRequirement))!.GetGetMethod(), typeof(KarmaMask).GetMethod(nameof(KarmaMask.Meet_Requirement)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.OutsideWatcherCampaign)).GetGetMethod(), typeof(KarmaMask).GetMethod(nameof(KarmaMask.Outside_Watcher)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.rippleLevel)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.PlayerRippleLevel)));
                    new Hook(typeof(Player).GetProperty(nameof(Player.maxRippleLevel)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.PlayerMaxRippleLevel)));

                    new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.CanSee_VoidSpawn)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.VisibilityBonus)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.Visibility_Bonus)));
                    //new Hook(typeof(Player).GetProperty(nameof(Player.gravity)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.OverrideGravity)));
                    //new Hook(typeof(Player).GetProperty(nameof(Player.airFriction)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(Plugin.OverrideAirFriction)));

                    new Hook(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsStraw)).GetGetMethod(), typeof(NothingToSeeHere).GetMethod(nameof(NothingToSeeHere.Is_Sliver)));
                    new Hook(typeof(OracleGraphics).GetProperty(nameof(OracleGraphics.IsPebbles)).GetGetMethod(), typeof(NothingToSeeHere).GetMethod(nameof(NothingToSeeHere.Is_EP)));
                    new Hook(typeof(Oracle).GetProperty(nameof(Oracle.Alive)).GetGetMethod(), typeof(NothingToSeeHere).GetMethod(nameof(NothingToSeeHere.Is_Alive)));

                    new Hook(typeof(Menu.KarmaLadderScreen).GetProperty(nameof(Menu.KarmaLadderScreen.UsesWarpMap)).GetGetMethod(), typeof(LProgression).GetMethod(nameof(LProgression.UsesWarpMap)));
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
        public static void IntroRoll_ctor(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(5)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((IntroRoll self) =>
                {
                    if (self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == LookerEnums.looker)
                    {
                        self.illustrations[2] = new(self, self.pages[0], "", lookerIntroRoll, Vector2.zero, true, false);
                    }
                });
            }
            else Log.LogMessage("Error in Introroll IL hook!");
        }

        public static void ResetModData()
        {
            darknessProgress = 0;
            retractDarkness = false;
            timeUntilFloatEnds = -1;
            shownPopupMenu = false;
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
            Futile.atlasManager.LoadImage(ogsculeIcon);
            Futile.atlasManager.LoadImage(lookerRippleBig);
            Futile.atlasManager.LoadImage(lookerRippleSmall);
            Futile.atlasManager.LoadImage(templarMaskIcon);
            Futile.atlasManager.LoadImage(lookerIntroRoll);
        }
    }
}