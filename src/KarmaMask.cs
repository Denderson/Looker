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

        public static void VultureMask_Update(On.VultureMask.orig_Update orig, VultureMask self, bool eu)
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
        public static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.VultureMask && intData == 50)
            {
                return RainWorld.GoldRGB;
            }
            return orig(itemType, intData);
        }

        public static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (itemType == AbstractPhysicalObject.AbstractObjectType.VultureMask && intData == 50)
            {
                return templarMaskIcon;
            }
            return orig(itemType, intData);
        }

        public static IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            IconSymbol.IconSymbolData? value = orig(item);
            if (item.type == AbstractPhysicalObject.AbstractObjectType.VultureMask && item.ID == SpecialId)
            {
                return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, 50);
            }
            return value;
        }

        public static void KarmaMeter_Update(On.HUD.KarmaMeter.orig_Update orig, HUD.KarmaMeter self)
        {
            orig(self);
            if (self.hud?.owner is Player player && player.room?.game.StoryCharacter == LookerEnums.looker && CWTs.PlayerCWT.TryGetData(player, out var data))
            {

                if (!data.karmaMode && data.previousKarmaMode)
                {
                    self.karmaSprite.element = Futile.atlasManager.GetElementWithName(HUD.KarmaMeter.RippleSymbolSprite(small: true, 5));
                    self.forceVisibleCounter = Math.Max(self.forceVisibleCounter, 120);
                }
                if (data.karmaMode && !data.previousKarmaMode)
                {
                    self.displayKarma.x = 9;
                    self.displayKarma.y = 9;
                    self.karmaSprite.element = Futile.atlasManager.GetElementWithName(HUD.KarmaMeter.KarmaSymbolSprite(small: true, self.displayKarma));
                    self.forceVisibleCounter = Math.Max(self.forceVisibleCounter, 120);
                }
            }
        }

        public static void VultureMaskGraphics_ctor_PhysicalObject_MaskType_int_string(On.MoreSlugcats.VultureMaskGraphics.orig_ctor_PhysicalObject_MaskType_int_string orig, VultureMaskGraphics self, PhysicalObject attached, VultureMask.MaskType type, int firstSprite, string overrideSprite)
        {
            orig(self, attached, type, firstSprite, overrideSprite);
            if (self.attachedTo is VultureMask && (self.attachedTo as VultureMask).abstractPhysicalObject.ID == SpecialId)
            {
                self.maskType = VultureMask.MaskType.SCAVTEMPLAR;
                self.glimmer = true;
                self.ignoreDarkness = true;
            }
        }

        public static void VultureMaskGraphics_ctor(On.MoreSlugcats.VultureMaskGraphics.orig_ctor_PhysicalObject_AbstractVultureMask_int orig, VultureMaskGraphics self, PhysicalObject attached, VultureMask.AbstractVultureMask abstractMask, int firstSprite)
        {
            orig(self, attached, abstractMask, firstSprite);
            if (self.attachedTo is VultureMask && (self.attachedTo as VultureMask).abstractPhysicalObject.ID == SpecialId)
            {
                self.maskType = VultureMask.MaskType.SCAVTEMPLAR;
                self.glimmer = true;
                self.ignoreDarkness = true;
            }
        }

        public static void VultureMaskGraphics_DrawSprites(On.MoreSlugcats.VultureMaskGraphics.orig_DrawSprites orig, VultureMaskGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.attachedTo is VultureMask && (self.attachedTo as VultureMask).abstractPhysicalObject.ID == SpecialId)
            {
                sLeaser.sprites[self.firstSprite].color = RainWorld.GoldRGB;
                sLeaser.sprites[self.firstSprite].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
            }
        }

        public static void VultureMask_ctor(On.VultureMask.orig_ctor orig, VultureMask self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            orig(self, abstractPhysicalObject, world);
            if (self.abstractPhysicalObject.ID == SpecialId)
            {
                self.abstractPhysicalObject.rippleBothSides = true;
                if (CWTs.VultureMaskCWT.TryGetData(self, out var data))
                {
                    data.isKarmaMask = true;
                }
                else Log.LogMessage("Couldnt grab CWT in Vulture Mask ctor!");
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
                if (shelter != null && shelter != "SU_S04")
                {
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
