using BepInEx;
using BepInEx.Logging;
using Looker.CWTs;
using Looker.Regions;
using Menu.Remix.MixedUI;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using UnityEngine;
using Watcher;
using static Looker.Plugin;
using LizardCosmetics;

namespace Looker.Regions
{
    public static class LGrove
    {
        public const int ogsculeNumber = 9000;

        public static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (intData == ogsculeNumber && !OptionsMenu.colorfulOgscules.Value)
            {
                return Color.red;
            }
            return orig(itemType, intData);
        }

        public static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (intData == ogsculeNumber)
            {
                return ogsculeIcon;
            }
            return orig(itemType, intData);
        }

        public static IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            IconSymbol.IconSymbolData? value = orig(item);
            if (item?.world?.game != null && CheckMechanics(item.world.game, "pillar", "WPGA"))
            {
                return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, ogsculeNumber);
            }
            return value;
        }

        public static void ItemMarker_Draw(On.HUD.Map.ItemMarker.orig_Draw orig, HUD.Map.ItemMarker self, float timeStacker)
        {
            if (ModManager.MMF && CheckMechanics(self?.obj?.Room?.realizedRoom, "pillar", "WPGA"))
            {
                bool value = MoreSlugcats.MMF.cfgCreatureSense.Value;
                MoreSlugcats.MMF.cfgCreatureSense.Value = false;
                orig(self, timeStacker);
                MoreSlugcats.MMF.cfgCreatureSense.Value = value;
                return;
            }
            orig(self, timeStacker);
        }

        public static void Map_Draw(On.HUD.Map.orig_Draw orig, HUD.Map self, float timeStacker)
        {
            if (ModManager.MMF && self?.hud?.owner?.GetOwnerType() == HUD.HUD.OwnerType.Player && self.hud.owner is Player player && CheckMechanics(player?.room, "pillar", "WPGA"))
            {
                bool value = MoreSlugcats.MMF.cfgCreatureSense.Value;
                MoreSlugcats.MMF.cfgCreatureSense.Value = false;
                orig(self, timeStacker);
                MoreSlugcats.MMF.cfgCreatureSense.Value = value;
                return;
            }
            orig(self, timeStacker);
        }

        public static bool IsSpriteBlacklisted(IDrawable obj, out int mainSprite)
        {
            mainSprite = -1;
            if (obj == null) return false;
            if (obj is ComplexGraphicsModule.GraphicsSubModule)
            {
                return true;
            }    
            if (obj is PhysicalObject)
            {
                mainSprite = 0;
                return true;
            }
            if (obj is GraphicsModule module)
            {
                mainSprite = 0;
                if (module.owner?.bodyChunks != null && module.owner.bodyChunks.Length > 0)
                {
                    
                    if (module.owner is Creature)
                    {
                        mainSprite = (module.owner as Creature).mainBodyChunkIndex;
                    }
                }
                return true;
            }
            if (obj is LightSource light && light.tiedToObject is Creature)
            {
                return true;
            }
            if (obj is LizardBubble)
            {
                return true;
            }
            return false;
        }

        public static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self, timeStacker, rCam, camPos);
            if (IsSpriteBlacklisted(self?.drawableObject, out int mainSprite) && CheckMechanics(rCam?.room, "pillar", "WPGA"))
            {
                for (int i = 0; i < self.sprites.Length; i++)
                {
                    if (i != mainSprite)
                    {
                        self.sprites[i].color = Color.black;
                    }
                    else if (self.sprites[i].element.name != ogsculeSprite)
                    {
                        self.sprites[i].SetElementByName(ogsculeSprite);
                        self.sprites[i].height = 30f;
                        self.sprites[i].width = 30f;
                        self.sprites[i].rotation = 0;
                        self.sprites[i].MoveToFront();
                        if (!OptionsMenu.colorfulOgscules.Value)
                        {
                            self.sprites[i].color = Color.red;
                        }
                    }
                }
            }
        }
    }
}
