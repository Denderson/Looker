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
            string value = orig(itemType, intData);
            if (intData == ogsculeNumber)
            {
                Log.LogMessage("Forcing ogscule image");
                return ogsculeIcon;
            }
            return value;
        }

        public static IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            IconSymbol.IconSymbolData? value = orig(item);
            Log.LogMessage("Stating the ogscule override");
            if (item?.Room?.realizedRoom != null)
            {
                Log.LogMessage("Ogspores have been spread");
                if (CheckMechanics(item.Room.realizedRoom, "pillar", "WPGA"))
                {
                    Log.LogMessage("Ogspores have been spread 2");
                    return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, ogsculeNumber);
                }
            }
            return value;
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
