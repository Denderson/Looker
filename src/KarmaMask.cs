using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Looker.Plugin;

namespace Looker
{
    public static class KarmaMask
    {
        public static bool RippleLadderMode(Func<Menu.KarmaLadderScreen, bool> orig, Menu.KarmaLadderScreen self)
        {
            if (self.saveState?.saveStateNumber == LookerEnums.looker)
            {
                return false;
            }
            return orig(self);
        }

        public static bool Meet_Requirement(Func<RegionGate, bool> orig, RegionGate self)
        {
            if (self?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self);
        }

        public static bool Outside_Watcher(Func<Player, bool> orig, Player self)
        {
            return orig(self) || (self?.slugcatStats?.name == LookerEnums.looker);
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

        public static string SaveState_GetSaveStateDenToUse(On.SaveState.orig_GetSaveStateDenToUse orig, SaveState self)
        {
            string text = orig(self);
            if (self.saveStateNumber == LookerEnums.looker)
            {
                if (warptodaemon)
                {
                    warptodaemon = false;
                    SaveFileCode.SetBool(self, "PuzzleComplete", true);
                    return "WRSA_WEAVER02";
                }
                string shelter = SaveFileCode.GetString(self, "OverrideShelter");
                Log.LogMessage("Checking for overriding shelter!");
                if (shelter != null && shelter != "SU_S04")
                {
                    Log.LogMessage("Shelter override starting:" + shelter);
                    SaveFileCode.SetString(self, "OverrideShelter", "SU_S04");
                    return shelter;
                }
            }
            return text;
        }

        public static void CheckMaskMechanics(Room room)
        {
            bool usingMask = false;
            bool usingFlower = false;
            for (int i = 0; i < room.physicalObjects.Length; i++)
            {
                foreach (PhysicalObject item in room.physicalObjects[i])
                {
                    if (item is VultureMask mask)
                    {
                        if (mask.abstractPhysicalObject.ID == SpecialId)
                        {
                            string newshelter = CleansedShelter(room, out bool successful);
                            if (successful)
                            {
                                Log.LogMessage("Mask triggered successfully!");
                                SaveFileCode.SetString(room.game.GetStorySession.saveState, "OverrideShelter", newshelter);
                                SaveFileCode.SetBool(room.game.GetStorySession.saveState, "CreateMask", true);
                                mask.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, mask.abstractPhysicalObject.pos.Vec2(), 1f, 1f);
                                for (int j = 0; j < 20; j++)
                                {
                                    mask.room.AddObject(new Spark(mask.abstractPhysicalObject.pos.Vec2(), Custom.RNV() * (25f * UnityEngine.Random.value), RainWorld.GoldRGB, null, 70, 150));
                                }
                                mask.Destroy();
                                return;
                            }
                        }
                        else usingMask = true;
                    }
                    if (item is KarmaFlower karmaFlower)
                    {
                        usingFlower = true;
                    }
                }
            }
            if (CheckMechanics(room, "ridge", "WARF") && !OptionsMenu.constantShelters.Value)
            {
                SaveFileCode.SetString(room.game.GetStorySession.saveState, "OverrideShelter", RandomShelter());
            }
            if (usingFlower && usingMask)
            {
                SaveFileCode.SetBool(room.game.GetStorySession.saveState, "CreateMask", true);
            }
        }

        public static string RandomShelter()
        {
            return (float)(UnityEngine.Random.value * 9) switch
            {
                (< 1) => "WARF_S01",
                (< 2) => "WARF_S02",
                (< 3) => "WARF_S03",
                (< 4) => "WARF_S04",
                (< 5) => "WARF_S06",
                (< 6) => "WARF_S08",
                (< 7) => "WARF_S14",
                (< 8) => "WARF_S18",
                _ => "WARF_S32",
            };
        }

        public static string CleansedShelter(Room room, out bool successful)
        {
            string name = room.abstractRoom.name;
            successful = true;
            if (name.StartsWith("WSUR_S"))
            {
                return "SU_S" + name.Substring(6);
            }
            else if (name.StartsWith("WDSR_S"))
            {
                return "DS_S" + name.Substring(6);
            }
            else if (name.StartsWith("WGWR_S"))
            {
                return "GW_S" + name.Substring(6);
            }
            successful = false;
            Log.LogMessage("Didn't warp");
            return name;
        }

        public static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (world.game?.StoryCharacter == LookerEnums.looker)
            {
                if (SaveFileCode.GetBool(world.game.GetStorySession.saveState, "CreateMask"))
                {
                    VultureMask.AbstractVultureMask abstractVultureMask = new(world, null, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), SpecialId, self.abstractCreature.ID.RandomSeed, false);
                    self.room.abstractRoom.AddEntity(abstractVultureMask);
                    abstractVultureMask.RealizeInRoom();
                }
                return;
            }

        }
    }
}
