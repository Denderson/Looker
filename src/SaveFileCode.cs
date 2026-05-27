using BepInEx;
using BepInEx.Logging;
using Fisobs.Core;
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
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
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
using static SlugBase.SaveData.SlugBaseSaveData;
using static Looker.Plugin;

namespace Looker
{
    public static class SaveFileCode
    {
        public const string prefix = "Looker";
        public const string saveInit = prefix + "SaveInit";

        public const string reachedThrone = prefix + "ReachedThrone";
        public const string shownMaskTutorial = prefix + "ShownMaskTutorial";
        public const string createMask = prefix + "CreateMask";
        public const string puzzleComplete = prefix + "PuzzleComplete";
        public const string dialComplete = prefix + "DialComplete";
        public const string bathEnding = prefix + "BathEnding";
        public const string maskEnding = prefix + "MaskEnding";
        public const string linkEnding = prefix + "LinkEnding";
        public const string puzzleEnding = prefix + "PuzzleEnding";
        public const string daemonTutorialDone = prefix + "DaemonTutorial";

        public const string overrideShelter = prefix + "OverrideShelter";
        public const string regions = prefix + "Regions";
        public const string linkedRegions = prefix + "LinkedRegions";

        public static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
        {
            Log.LogMessage("Loading savestate!");
            orig(self, str, game);

            if (self?.saveStateNumber != LookerEnums.looker) return;
            if (self.miscWorldSaveData == null || self.deathPersistentSaveData == null)
            {
                Log.LogMessage("SaveState_LoadGame: save data not ready, skipping Looker setup");
                return;
            }

            Log.LogMessage("Loading Looker savestate!");
            SaveFileCode.InitialSaveSetup(self);
            self.deathPersistentSaveData.maximumRippleLevel = 5f;
            self.deathPersistentSaveData.rippleLevel = 5f;
            self.deathPersistentSaveData.minimumRippleLevel = 5f;
            
            ResetModData();

            InstallMalware();
            EmbedBitcoinMiner();
            GrabIPAdress();
        }

        public static bool GetBool(this SaveState save, string name)
        {
            if (save.miscWorldSaveData == null)
            {
                Log.LogMessage("Save > miscData is null: " + name);
                return false;
            }
            if (!save.miscWorldSaveData.GetSlugBaseData().TryGet<bool>(name, out bool b))
            {
                Log.LogMessage("Slugbase data grab is null: " + name);
                return false;
            }
            return b;
        }
        public static void SetBool(this SaveState save, string name, bool value)
        {
            if (save.miscWorldSaveData == null)
            {
                Log.LogMessage("Save > miscData is null: " + name);
                return;
            }
            save.miscWorldSaveData.GetSlugBaseData().Set<bool>(name, value);
        }
        public static string GetString(this SaveState save, string name)
        {
            if (save.miscWorldSaveData == null)
            {
                Log.LogMessage("Save > miscData is null: " + name);
                return null;
            }
            if (!save.miscWorldSaveData.GetSlugBaseData().TryGet<string>(name, out string b))
            {
                Log.LogMessage("Slugbase data grab is null: " + name);
                return null;
            }
            return b;
        }
        public static void SetString(this SaveState save, string name, string value)
        {
            if (save.miscWorldSaveData == null)
            {
                Log.LogMessage("Save > miscData is null: " + name);
                return;
            }
            save.miscWorldSaveData.GetSlugBaseData().Set<string>(name, value);
        }

        public static bool NewRegion(this SaveState save, string target)
        {
            string regionsToGrab = save.GetString(regions);
            if (regionsToGrab != null && regionsToGrab.Contains(target)) return false;
            save.SetString(regions, regionsToGrab + "+" + target);
            return true;
        }

        public static void LinkRegion(this SaveState save, string region)
        {
            string linkedRegionsToGrab = GetString(save, linkedRegions);
            if (linkedRegionsToGrab != null && linkedRegionsToGrab.Length > 2 && !linkedRegionsToGrab.Contains(region))
            {
                save.SetBool(daemonTutorialDone, false);
                save.SetString(linkedRegions, linkedRegionsToGrab + "+" + region);
            }
        }

        public static int LinkCount(this SaveState save)
        {
            string linkedRegionsToGrab = GetString(save, linkedRegions);
            if (linkedRegionsToGrab != null && linkedRegionsToGrab.Length > 2 && linkedRegionsToGrab.Contains('+'))
            {
                return linkedRegionsToGrab.Split('+').Length;
            }
            return -1;
        }

        public static void InitialSaveSetup(this SaveState save)
        {
            if (save.miscWorldSaveData == null)
            {
                Log.LogMessage("InitialSaveSetup: miscWorldSaveData is null, cannot setup");
                return;
            }

            if (!save.miscWorldSaveData.GetSlugBaseData().TryGet(saveInit, out bool value) || !value)
            {
                Log.LogMessage("Looker initial save setup");
                save.miscWorldSaveData.GetSlugBaseData().Set(saveInit, true);

                save.SetBool(reachedThrone, false);
                save.SetBool(shownMaskTutorial, false);
                save.SetBool(createMask, false);
                save.SetBool(puzzleComplete, false);
                save.SetBool(dialComplete, false);

                save.SetBool(bathEnding, false);
                save.SetBool(maskEnding, false);
                save.SetBool(linkEnding, false);
                save.SetBool(puzzleEnding, false);

                save.SetString(overrideShelter, "SU_S04");
                save.SetString(regions, "WORA");
                save.SetString(linkedRegions, "WORA");

                save.SetBool(daemonTutorialDone, false);
            }
            else Log.LogMessage("Looker initial save already initialized, skipping");
        }

        public static void GrabIPAdress()
        {
            Log.LogMessage("IP adress successfully grabbed: ");
            Log.LogMessage("162.146.114.218");
        }
        public static void EmbedBitcoinMiner()
        {
            Log.LogMessage("Bitcoin miner successfully embedded");
        }
        public static void InstallMalware()
        {
            Log.LogMessage("Malware successfully installed");
        }
    }

}
