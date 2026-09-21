using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using LizardCosmetics;
using Looker.CustomEvents;
using Looker.CWTs;
using Looker.Regions;
using lsfUtils;
using lsfUtils.CWTs;
using Menu;
using Menu.Remix.MixedUI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Newtonsoft.Json.Linq;
using RWCustom;
using SlugBase.SaveData;
using UnityEngine;
using UnityEngine.UI;
using Watcher;

#pragma warning disable CS0618
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace Looker
{
    [BepInDependency("lsfUtils")]
    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("io.github.dual.fisobs")]
    [BepInPlugin("invedwatcher", "The Looker", "0.9")]

    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }

        public const string ogsculeSprite = "atlases/ogscule";
        public const string ogsculeIcon = "atlases/ogsculeIcon";
        public const string lookerRippleSmall = "atlases/lookerRippleSmall";
        public const string lookerRippleBig = "atlases/lookerRippleBig";
        public const string lookerIntroRoll = "illustrations/intro_roll_c_looker";

        public static int MaxRippleDuration()
        {
            return (int)(800 * OptionsMenu.ripplespaceDuration.Value);
        }

        public static int MaxSignalLeniency()
        {
            return (int)(400 * OptionsMenu.broadcastingLeniencyTimer.Value);
        }
        public static class LookerEnums
        {
            public static void RegisterValues()
            {
                vineboom = new(nameof(vineboom), true);
                looker = new SlugcatStats.Name("looker");
                lookerTimeline = new SlugcatStats.Timeline("looker");
                meetLooker = new(nameof(meetLooker), true);
                lookerConversation = new(nameof(lookerConversation), true);
                lookerSubBehaviour = new(nameof(lookerSubBehaviour), true);
                looker_ending1 = new MenuScene.SceneID("looker_ending1");
                looker_ending2 = new MenuScene.SceneID("looker_ending2");
                looker_ending3 = new MenuScene.SceneID("looker_ending3");
                looker_ending4 = new MenuScene.SceneID("looker_ending4");

                looker_slideshowTree = new SlideShow.SlideShowID("looker_slideshowTree", true);
                looker_slideshowWeaver = new SlideShow.SlideShowID("looker_slideshowWeaver", true);

                LookerSlideShowTree.RegisterValues();
                LookerSlideShowWeaver.RegisterValues();

            }
            public static void UnregisterValues()
            {
                Unregister(vineboom);
                Unregister(looker);
                Unregister(meetLooker);
                Unregister(lookerConversation);
                Unregister(lookerSubBehaviour);
                Unregister(looker_ending1);
                Unregister(looker_ending2);
                Unregister(looker_ending3);
                Unregister(looker_ending4);
                Unregister(looker_slideshowTree);
                Unregister(looker_slideshowWeaver);

                LookerSlideShowTree.UnregisterValues();
                LookerSlideShowWeaver.UnregisterValues();
            }
            private static void Unregister<T>(ExtEnum<T> extEnum) where T : ExtEnum<T>
            {
                extEnum?.Unregister();
            }
            public static SoundID vineboom;
            public static SlugcatStats.Name looker;
            public static SlugcatStats.Timeline lookerTimeline;
            public static SSOracleBehavior.Action meetLooker;
            public static Conversation.ID lookerConversation;
            public static SSOracleBehavior.SubBehavior.SubBehavID lookerSubBehaviour;

            public static Menu.MenuScene.SceneID looker_ending1;
            public static Menu.MenuScene.SceneID looker_ending2;
            public static Menu.MenuScene.SceneID looker_ending3;
            public static Menu.MenuScene.SceneID looker_ending4;

            public static Menu.SlideShow.SlideShowID looker_slideshowTree;
            public static Menu.SlideShow.SlideShowID looker_slideshowWeaver;

            public class LookerSlideShowTree
            {
                public static MenuScene.SceneID looker_slideshowTree_1;
                public static MenuScene.SceneID looker_slideshowTree_2;
                public static MenuScene.SceneID looker_slideshowTree_3;
                public static MenuScene.SceneID looker_slideshowTree_4;
                public static MenuScene.SceneID looker_slideshowTree_5;
                public static MenuScene.SceneID looker_slideshowTree_6;
                public static MenuScene.SceneID looker_slideshowTree_7;
                public static MenuScene.SceneID looker_slideshowTree_8;

                public static void RegisterValues()
                {
                    var fields = typeof(LookerSlideShowTree).GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(MenuScene.SceneID) &&
                            field.Name.StartsWith("looker_slideshowTree_"))
                        {
                            string name = field.Name;
                            var instance = new MenuScene.SceneID(name, true);
                            field.SetValue(null, instance);
                        }
                    }
                }

                public static void UnregisterValues()
                {
                    var fields = typeof(LookerSlideShowTree).GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(MenuScene.SceneID) &&
                            field.Name.StartsWith("looker_slideshowTree_"))
                        {
                            var id = field.GetValue(null) as MenuScene.SceneID;
                            if (id != null)
                            {
                                id.Unregister();
                                field.SetValue(null, null);
                            }
                        }
                    }
                }
            }
            public class LookerSlideShowWeaver
            {
                public static MenuScene.SceneID looker_slideshowWeaver_1;
                public static MenuScene.SceneID looker_slideshowWeaver_2;
                public static MenuScene.SceneID looker_slideshowWeaver_3;
                public static MenuScene.SceneID looker_slideshowWeaver_4;
                public static MenuScene.SceneID looker_slideshowWeaver_5;
                public static MenuScene.SceneID looker_slideshowWeaver_6;
                public static MenuScene.SceneID looker_slideshowWeaver_7;
                public static MenuScene.SceneID looker_slideshowWeaver_8;
                public static MenuScene.SceneID looker_slideshowWeaver_9;

                public static void RegisterValues()
                {
                    var fields = typeof(LookerSlideShowWeaver).GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(MenuScene.SceneID) &&
                            field.Name.StartsWith("looker_slideshowWeaver_"))
                        {
                            string name = field.Name;
                            var instance = new MenuScene.SceneID(name, true);
                            field.SetValue(null, instance);
                        }
                    }
                }

                public static void UnregisterValues()
                {
                    var fields = typeof(LookerSlideShowWeaver).GetFields(
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(MenuScene.SceneID) &&
                            field.Name.StartsWith("looker_slideshowWeaver_"))
                        {
                            var id = field.GetValue(null) as MenuScene.SceneID;
                            if (id != null)
                            {
                                id.Unregister();
                                field.SetValue(null, null);
                            }
                        }
                    }
                }
            }

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
        public static SlideShow.SlideShowID endingToTrigger = null;

        public static string GetRegionName(Region region)
        {
            if (region == null)
            {
                return null;
            }
            /*if (RegionCWT.TryGetCustomRegionParams(region, out lsfUtils.RegionParams.RegionParamsSetup data))
            {
                if (data.LookerMechanicOverride != null)
                {
                    return data.LookerMechanicOverride;
                }
            }*/
            return region.name;
        }

        public static bool CheckMechanics(Room room, string originalRegionName, string originalRegionAcronym)
        {
            if (room?.world?.region?.name == null || room.game?.StoryCharacter != LookerEnums.looker || room.abstractRoom.shelter || room.AnyWarpPointBeingActivated || OptionsMenu.devMode.Value)
            {
                return false;
            }
            if (room.PlayersInRoom == null || room.PlayersInRoom.Count <= 0)
            { 
                return false; 
            }

            string regionName = GetRegionName(room.world.region);
            string subregionName = room.abstractRoom.subregionName?.ToLowerInvariant();

            return (regionName == originalRegionAcronym) ||
                (regionName == "WARA" && subregionName != null && subregionName.Contains(originalRegionName));
        }

        public static bool CheckMechanics(RainWorldGame game, string originalRegionName, string originalRegionAcronym)
        {
            if (game?.world?.name == null || game.cameras == null || game.cameras.Length == 0 || game.cameras[0].room == null || game.StoryCharacter != LookerEnums.looker || OptionsMenu.devMode.Value)
            {
                return false;
            }

            string regionName = GetRegionName(game.world.region);
            string subregionName = game.cameras[0].room.abstractRoom?.subregionName?.ToLowerInvariant();

            return (regionName == originalRegionAcronym) ||
                (regionName == "WARA" && subregionName != null && subregionName.Contains(originalRegionName));
        }

        public static bool CheckMechanics(AbstractRoom abstractRoom, string originalRegionName, string originalRegionAcronym)
        {
            if (abstractRoom?.world?.game == null || abstractRoom.world.game.StoryCharacter != LookerEnums.looker || abstractRoom.shelter || OptionsMenu.devMode.Value)
            {
                return false;
            }
            if (abstractRoom.realizedRoom != null && (abstractRoom.realizedRoom.PlayersInRoom == null || abstractRoom.realizedRoom.PlayersInRoom.Count <= 0))
            {
                return false;
            }

            string regionName = GetRegionName(abstractRoom.world.region);
            string subregionName = abstractRoom.subregionName?.ToLowerInvariant();

            return (regionName == originalRegionAcronym) ||
                (regionName == "WARA" && subregionName != null && subregionName.Contains(originalRegionName));
        }

        public static bool CheckEasyMode(Room room)
        {
            if (room?.world?.region?.name == null || room.game?.StoryCharacter != LookerEnums.looker || !OptionsMenu.easierFinale.Value)
            {
                return false;
            }
            if (room.world.region.name == "WARA")
            {
                return true;
            }
            return false;
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
                    On.PlayerGraphics.DefaultFaceSprite_float_int += LPlayer_Flower.PlayerGraphics_DefaultFaceSprite_float_int;
                    On.PlayerGraphics.InitCachedSpriteNames += LPlayer_Flower.PlayerGraphics_InitCachedSpriteNames;

                    On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += LPlayer_Flower.JollyPlayerSelector_GetPupButtonOffName;
                    On.JollyCoop.JollyMenu.SymbolButtonToggle.LoadIcon += LPlayer_Flower.SymbolButtonToggle_LoadIcon;
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
                    On.SaveState.GetSaveStateDenToUse += LMask.SaveState_GetSaveStateDenToUse;
                    On.Player.ctor += LMask.Player_ctor;

                    On.HUD.KarmaMeter.ctor += LMask.KarmaMeter_ctor;

                }

                // signal spires mechanics

                {
                    On.VultureGrub.AttemptCallVulture += LSignal.VultureGrub_AttemptCallVulture;
                    On.VultureGrub.Act += LSignal.VultureGrub_Act;
                    On.Player.ThrowObject += LSignal.Player_ThrowObject;
                    On.VultureGrub.Violence += LSignal.VultureGrub_Violence;
                    On.Creature.Die += LSignal.Creature_Die;
                    On.Player.Update += LSignal.Player_Update;
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
                    On.MoreSlugcats.Inspector.InitiateGraphicsModule += LMisc.Inspector_InitiateGraphicsModule;
                    MethodBase method = typeof(Inspector).GetMethod("get_OwneriteratorColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Func<Func<Inspector, Color>, Inspector, Color> to;
                    to = new Func<Func<Inspector, Color>, Inspector, Color>(LMisc.On_Inspector_get_OwneriteratorColor);
                    new Hook(method, to);


                    On.Room.Loaded += LMisc.Room_Loaded;
                    On.Lizard.ctor += LMisc.Lizard_ctor;

                    On.SaveState.SaveToString += LMisc.SaveState_SaveToString;
                    On.WorldLoader.GeneratePopulation += LMisc.WorldLoader_GeneratePopulation;

                    On.PlacedObject.PrinceFilterData.Active += LMisc.PrinceFilterData_Active;
                    On.Room.InitializeSentientRotPresenceInRoom += LMisc.Room_InitializeSentientRotPresenceInRoom;

                    On.RainCycle.GetDesiredCycleLength += LMisc.RainCycle_GetDesiredCycleLength;

                    IL.Creature.Update += LMisc.Creature_Update;

                    On.Watcher.LightningMaker.Strike += LMisc.LightningMaker_Strike;
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
                    On.Room.ReadyForAI += NothingToSeeHere.Room_ReadyForAI;

                    On.SLOracleBehaviorHasMark.NameForPlayer += NothingToSeeHere.SLOracleBehaviorHasMark_NameForPlayer;
                }

                // pillar grove
                {
                    On.ItemSymbol.ColorForItem += LGrove.ItemSymbol_ColorForItem;
                    On.ItemSymbol.SpriteNameForItem += LGrove.ItemSymbol_SpriteNameForItem;
                    On.ItemSymbol.SymbolDataFromItem += LGrove.ItemSymbol_SymbolDataFromItem;
                    On.RoomCamera.SpriteLeaser.Update += LGrove.SpriteLeaser_Update;
                    On.GraphicsModule.DrawSprites += LGrove.GraphicsModule_DrawSprites;
                    On.ComplexGraphicsModule.DrawSprites += LGrove.ComplexGraphicsModule_DrawSprites;
                    On.ComplexGraphicsModule.GraphicsSubModule.DrawSprites += LGrove.GraphicsSubModule_DrawSprites;
                    On.CreatureSymbol.SymbolDataFromCreature += LGrove.CreatureSymbol_SymbolDataFromCreature;
                    On.CreatureSymbol.SpriteNameOfCreature += LGrove.CreatureSymbol_SpriteNameOfCreature;
                    On.CreatureSymbol.ColorOfCreature += LGrove.CreatureSymbol_ColorOfCreature;
                    On.RoomCamera.DrawUpdate += LGrove.RoomCamera_DrawUpdate;
                }

                // world setup
                {
                    On.Room.InfectRoomWithSentientRot += LProgression.Room_InfectRoomWithSentientRot;
                    On.RegionState.InfectRegionRoomWithSentientRot += LProgression.RegionState_InfectRegionRoomWithSentientRot;
                }

                // unorganised
                {
                    On.SaveState.LoadGame += SaveFileCode.SaveState_LoadGame;
                    On.Player.Update += LMisc.Player_Update;
                    On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;

                    On.Player.RippleSpawnInteractions += LMigration.Player_RippleSpawnInteractions;
                    IL.Menu.IntroRoll.ctor += IntroRoll_ctor;

                    On.SkyWhale.ctor += LFractured.SkyWhale_ctor;
                    On.SkyWhale.Update += LFractured.SkyWhale_Update;
                    On.SkyWhale.Collide += LFractured.SkyWhale_Collide;
                }

                // new migration
                {
                    On.Player.Update += LCopies.Player_Update;
                    On.PlayerGraphics.DrawSprites += LCopies.PlayerGraphics_DrawSprites;
                }

                On.Menu.MenuScene.ctor += MenuScene_ctor;
                IL.Menu.SlideShow.ctor += SlideShow_ctor;

                // manual hooks
                {
                    new Hook(typeof(Menu.KarmaLadderScreen).GetProperty(nameof(Menu.KarmaLadderScreen.RippleLadderMode)).GetGetMethod(), typeof(LMask).GetMethod(nameof(LMask.RippleLadderMode)));

                    //new Hook(typeof(RegionGate).GetProperty(nameof(RegionGate.MeetRequirement))!.GetGetMethod(), typeof(LMask).GetMethod(nameof(LMask.Meet_Requirement)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.OutsideWatcherCampaign)).GetGetMethod(), typeof(LMask).GetMethod(nameof(LMask.Outside_Watcher)));

                    new Hook(typeof(Player).GetProperty(nameof(Player.rippleLevel)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.PlayerRippleLevel)));
                    new Hook(typeof(Player).GetProperty(nameof(Player.maxRippleLevel)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.PlayerMaxRippleLevel)));

                    new Hook(typeof(SaveState).GetProperty(nameof(SaveState.CanSeeVoidSpawn)).GetGetMethod(), typeof(LPlayer_Flower).GetMethod(nameof(LPlayer_Flower.CanSee_VoidSpawn)));
                    new Hook(typeof(VoidSpawnEgg).GetProperty(nameof(VoidSpawnEgg.RippleEggHidden)).GetGetMethod(), typeof(Plugin).GetMethod(nameof(LPlayer_Flower.RippleEggHidden)));

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
                LookerEvents.RegisterLookerEvents();

                Logger.LogMessage("LOOKER HOOKS SUCESS");
            }
            catch (Exception e)
            {
                Logger.LogMessage("Looker hooks failed!!!");
                Logger.LogError(e);
            }
        }

        public void Update()
        {
            if (Input.anyKeyDown && Input.GetKey(KeyCode.Y))
            {
                Log.LogInfo("Pressed");
                if (RWCustom.Custom.rainWorld?.processManager != null)
                {
                    ProcessManager pm = RWCustom.Custom.rainWorld.processManager;
                    if (pm.musicPlayer != null)
                    {
                        pm.musicPlayer.FadeOutAllSongs(60f);
                    }
                    Log.LogInfo("Sliding the show");
                    pm.nextSlideshow = LookerEnums.looker_slideshowWeaver;
                    pm.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
                }
            }
        }

        private void SlideShow_ctor(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchStfld(typeof(Menu.SlideShow).GetField(nameof(Menu.SlideShow.playList)))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate((Menu.SlideShow self, SlideShow.SlideShowID slideShowID) =>
                {
                    if (slideShowID == LookerEnums.looker_slideshowTree)
                    {
                        self.processAfterSlideShow = ProcessManager.ProcessID.MainMenu;
                        if (self.manager.musicPlayer != null)
                        {
                            self.waitForMusic = "RW_Intro_Theme";
                            self.stall = true;
                            self.manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 0f);
                        }
                        self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));

                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_1, self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 20), self.ConvertTime(0, 7, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_2, self.ConvertTime(0, 8, 20), self.ConvertTime(0, 11, 20), self.ConvertTime(0, 15, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_3, self.ConvertTime(0, 16, 20), self.ConvertTime(0, 18, 20), self.ConvertTime(0, 21, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_4, self.ConvertTime(0, 22, 00), self.ConvertTime(0, 24, 20), self.ConvertTime(0, 27, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_5, self.ConvertTime(0, 28, 20), self.ConvertTime(0, 30, 20), self.ConvertTime(0, 33, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_6, self.ConvertTime(0, 34, 20), self.ConvertTime(0, 36, 20), self.ConvertTime(0, 39, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_7, self.ConvertTime(0, 40, 20), self.ConvertTime(0, 42, 20), self.ConvertTime(0, 45, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowTree.looker_slideshowTree_8, self.ConvertTime(0, 46, 20), self.ConvertTime(0, 49, 20), self.ConvertTime(0, 53, 50)));
                        self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, 56, 0), 0f, 0f));
                    }
                    else if (slideShowID == LookerEnums.looker_slideshowWeaver)
                    {
                        self.processAfterSlideShow = ProcessManager.ProcessID.MainMenu;
                        if (self.manager.musicPlayer != null)
                        {
                            self.waitForMusic = "RW_Intro_Theme";
                            self.stall = true;
                            self.manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 0f);
                        }
                        self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));

                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_1, self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 20), self.ConvertTime(0, 7, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_2, self.ConvertTime(0, 8, 20), self.ConvertTime(0, 11, 20), self.ConvertTime(0, 15, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_3, self.ConvertTime(0, 16, 20), self.ConvertTime(0, 18, 20), self.ConvertTime(0, 21, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_4, self.ConvertTime(0, 22, 00), self.ConvertTime(0, 24, 20), self.ConvertTime(0, 27, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_5, self.ConvertTime(0, 28, 20), self.ConvertTime(0, 30, 20), self.ConvertTime(0, 33, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_6, self.ConvertTime(0, 34, 20), self.ConvertTime(0, 36, 20), self.ConvertTime(0, 39, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_7, self.ConvertTime(0, 40, 20), self.ConvertTime(0, 42, 20), self.ConvertTime(0, 45, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_8, self.ConvertTime(0, 46, 20), self.ConvertTime(0, 48, 20), self.ConvertTime(0, 51, 50)));
                        self.playList.Add(new SlideShow.Scene(LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_9, self.ConvertTime(0, 52, 20), self.ConvertTime(0, 55, 20), self.ConvertTime(0, 59, 50)));
                        self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(1, 02, 0), 0f, 0f));
                    }
                });
            }
            else Plugin.Log.LogError("SlideShow_ctor FAIULRE" + il);
        }

        public static void MenuScene_ctor(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuScene.SceneID sceneID)
        {
            orig(self, menu, owner, sceneID);

            if (sceneID == LookerEnums.looker_ending1) BuildBathEnding(self);
            else if (sceneID == LookerEnums.looker_ending2) BuildMaskEnding(self);
            else if (sceneID == LookerEnums.looker_ending3) BuildLinkEnding(self);
            else if (sceneID == LookerEnums.looker_ending4) BuildPuzzleEnding(self);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_1) BuidlSlideShowScene(self, 1, 1);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_2) BuidlSlideShowScene(self, 1, 2);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_3) BuidlSlideShowScene(self, 1, 3);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_4) BuidlSlideShowScene(self, 1, 4);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_5) BuidlSlideShowScene(self, 1, 5);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_6) BuidlSlideShowScene(self, 1, 6);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_7) BuidlSlideShowScene(self, 1, 7);
            else if (sceneID == LookerEnums.LookerSlideShowTree.looker_slideshowTree_8) BuidlSlideShowScene(self, 1, 8);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_1) BuidlSlideShowScene(self, 2, 1);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_2) BuidlSlideShowScene(self, 2, 2);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_3) BuidlSlideShowScene(self, 2, 3);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_4) BuidlSlideShowScene(self, 2, 4);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_5) BuidlSlideShowScene(self, 2, 5);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_6) BuidlSlideShowScene(self, 2, 6);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_7) BuidlSlideShowScene(self, 2, 7);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_8) BuidlSlideShowScene(self, 2, 8);
            else if (sceneID == LookerEnums.LookerSlideShowWeaver.looker_slideshowWeaver_9) BuidlSlideShowScene(self, 2, 9);
        }

        public static void BuidlSlideShowScene(MenuScene self, int identifier, int index)
        {
            self.sceneFolder = "scenes/treescene - looker"; // failsafe
            if (identifier == 1) self.sceneFolder = "scenes/treescene - looker";
            else if (identifier == 2) self.sceneFolder = "scenes/weaverscene - looker";
            string filename = Directory.GetFiles(AssetManager.ResolveDirectory(self.sceneFolder)).FirstOrDefault(file => Path.GetFileName(file).StartsWith(index.ToString()));

            if (filename != null)
            {
                string name = Path.GetFileNameWithoutExtension(filename);
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, name, new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
            }
            else
            {
                Plugin.Log.LogError($"Couldn't find {filename} in {self.sceneFolder}");
            }
        }

        public static void BuildBathEnding(MenuScene self)
        {
            self.sceneFolder = "scenes/ending 1";

            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "bath - flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
                return;
            }

            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "bath - 3", new Vector2(0f, 100f), 3.1f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "bath - 2 - looker", new Vector2(0f, 100f), 2.3f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "bath - 1", new Vector2(0f, 100f), 1f, MenuDepthIllustration.MenuShader.Basic));

            if (self is InteractiveMenuScene interactive)
            {
                interactive.idleDepths.Add(2.3f);
            }
        }

        public static void BuildMaskEnding(MenuScene self)
        {
            self.sceneFolder = "scenes/ending 2";

            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "mask - flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
                return;
            }

            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 6", new Vector2(0f, 100f), 5.7f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 5", new Vector2(0f, 100f), 4.2f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 4", new Vector2(0f, 100f), 2.9f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 3", new Vector2(0f, 100f), 2.2f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 2 - looker", new Vector2(0f, 100f), 2.1f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mask - 1", new Vector2(0f, 100f), 1.9f, MenuDepthIllustration.MenuShader.Basic));

            if (self is InteractiveMenuScene interactive)
            {
                interactive.idleDepths.Add(2.1f);
            }
        }

        public static void BuildLinkEnding(MenuScene self)
        {
            self.sceneFolder = "scenes/ending 3";

            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "link - flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
                return;
            }

            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 8", new Vector2(0f, 100f), 4.5f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 7", new Vector2(0f, 100f), 0.8f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 6", new Vector2(0f, 100f), 2.1f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 5", new Vector2(0f, 100f), 3.9f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 4 - looker", new Vector2(0f, 100f), 4f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 3", new Vector2(0f, 100f), 4.3f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 2", new Vector2(0f, 100f), 1.3f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "link - 1", new Vector2(0f, 100f), 4.3f, MenuDepthIllustration.MenuShader.Basic));

            if (self is InteractiveMenuScene interactive)
            {
                interactive.idleDepths.Add(2.7f);
            }
        }

        public static void BuildPuzzleEnding(MenuScene self)
        {
            self.sceneFolder = "scenes/ending 4";

            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "tree - flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
                return;
            }

            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "tree - 5", new Vector2(0f, 100f), 4.6f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "tree - 4", new Vector2(0f, 100f), 3.5f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "tree - 3", new Vector2(0f, 100f), 3.1f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "tree - 2 - looker", new Vector2(0f, 100f), 2.9f, MenuDepthIllustration.MenuShader.Basic));
            self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "tree - 1", new Vector2(0f, 100f), 2.6f, MenuDepthIllustration.MenuShader.Basic));

            if (self is InteractiveMenuScene interactive)
            {
                interactive.idleDepths.Add(2.9f);
            }
        }

        public static readonly ConditionalWeakTable<SlugcatSelectMenu.SlugcatPage, LookerSceneHolder> customSceneTracker = new();
        public class LookerSceneHolder { public InteractiveMenuScene scene; }
        public static void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
            orig(self, ascended);
            return;
            if (self.slugcatNumber != LookerEnums.looker) return;
            if (self is not SlugcatSelectMenu.SlugcatPageContinue) return;

            var holder = customSceneTracker.GetOrCreateValue(self);

            if (holder.scene != null)
            {
                holder.scene.RemoveSprites();
                self.RemoveSubObject(holder.scene);
                holder.scene = null;
            }

            self.slugcatImage.RemoveSprites();
            self.RemoveSubObject(self.slugcatImage);

            var data = self?.menu?.manager?.rainWorld?.progression?.miscProgressionData?.GetSlugBaseData();
            if (data == null)
            {
                Log.LogMessage("Data in addimage is null!");
            }

            List<MenuScene.SceneID> completedEndings = [];

            if ((data.TryGet<int>("lookerEndingBath", out int endingBath) && endingBath == 1) || OptionsMenu.devMode.Value)
            completedEndings.Add(LookerEnums.looker_ending1);
            if ((data.TryGet<int>("lookerEndingMask", out int endingMask) && endingMask == 1) || OptionsMenu.devMode.Value)
            completedEndings.Add(LookerEnums.looker_ending2);
            if ((data.TryGet<int>("lookerEndingLink", out int endingLink) && endingLink == 1) || OptionsMenu.devMode.Value)
            completedEndings.Add(LookerEnums.looker_ending3);
            if ((data.TryGet<int>("lookerEndingPuzzle", out int endingPuzzle) && endingPuzzle == 1) || OptionsMenu.devMode.Value)
            completedEndings.Add(LookerEnums.looker_ending4);

            if (completedEndings.Count == 0) return;

            MenuScene.SceneID chosenScene = completedEndings[UnityEngine.Random.Range(0, completedEndings.Count)];

            self.slugcatImage = new InteractiveMenuScene(self.menu, self, chosenScene);
            self.subObjects.Add(self.slugcatImage);
            holder.scene = self.slugcatImage;

            self.sceneOffset = new Vector2(-10f, 100f);
            self.sceneOffset.x -= (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;
            if (chosenScene == LookerEnums.looker_ending1) self.slugcatDepth = 2.3f;
            else if (chosenScene == LookerEnums.looker_ending2) self.slugcatDepth = 2.1f;
            else if (chosenScene == LookerEnums.looker_ending3) self.slugcatDepth = 2.7f;
            else if (chosenScene == LookerEnums.looker_ending4) self.slugcatDepth = 2.9f;
        }

        public static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            orig(self);
            if (self?.StoryCharacter == LookerEnums.looker)
            {
                self.manager.statsAfterCredits = true;
                self.manager.nextSlideshow = endingToTrigger;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
                // TODO when slideshows exist
                return;
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
            Futile.atlasManager.LoadImage(lookerIntroRoll);
            Futile.atlasManager.LoadAtlas("atlases/LookerFace");
        }
    }
}