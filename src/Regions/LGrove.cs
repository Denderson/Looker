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
        // the icon ID, cannot use numbers that other mods or vanilla uses or it will break
        public const int ogsculeNumber = 9000;

        // size of the ingame ogscule
        public const float ogsculeSize = 40f;

        // put 0f to be constantly pointing upwards
        public const float ogsculeRotation = 0f;

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

            if (!CheckMechanics(item?.Room, "pillar", "WPGA")) return value;

            return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, ogsculeNumber);
        }

        public static bool ShouldApplyOgsculeEffect(IDrawable obj, out int mainSprite)
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

        public static void ApplyOgsculeEffect(RoomCamera.SpriteLeaser sLeaser, int mainSprite)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                var sprite = sLeaser.sprites[i];
                if (sprite == null) continue;

                if (i == mainSprite)
                {
                    if (sprite.element?.name != ogsculeSprite)
                    {
                        sprite.SetElementByName(ogsculeSprite);
                    }

                    // set stats even if not overriding, to make sure it properly updates every tick
                    sprite.height = ogsculeSize;
                    sprite.width = ogsculeSize;
                    sprite.rotation = ogsculeRotation;
                    sprite.MoveToFront();

                    // in case colour isnt set every frame, it will just be set by the vanilla code instead
                    if (!OptionsMenu.colorfulOgscules.Value)
                    {
                        sprite.color = Color.red;
                    }
                }
                else
                {
                    // both ways to make non-ogscule sprites invisible as safety checks
                    // if it still doesnt work, will prob need custom implementation or grabbing properties / fields
                    sprite.color = new Color(0f, 0f, 0f, 0f);
                    sprite.isVisible = false;
                }
            }
        }

        public static void GraphicsModule_DrawSprites(On.GraphicsModule.orig_DrawSprites orig, GraphicsModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (!CheckMechanics(rCam?.room, "pillar", "WPGA")) return;

            int mainSprite = 0;
            if (self.owner is Creature creature)
            {
                mainSprite = creature.mainBodyChunkIndex;
            }
            ApplyOgsculeEffect(sLeaser, mainSprite);
        }

        public static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self, timeStacker, rCam, camPos);

            if (!CheckMechanics(rCam?.room, "pillar", "WPGA")) return;

            // Creatures are handled by GraphicsModule_DrawSprites to avoid being overwritten
            if (self?.drawableObject is GraphicsModule) return;
            if (!ShouldApplyOgsculeEffect(self?.drawableObject, out int mainSprite)) return;

            ApplyOgsculeEffect(self, mainSprite);
        }

        public static void GraphicsSubModule_DrawSprites(On.ComplexGraphicsModule.GraphicsSubModule.orig_DrawSprites orig, ComplexGraphicsModule.GraphicsSubModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (!CheckMechanics(rCam?.room, "pillar", "WPGA")) return;

            // Hide all sprites (arms, legs, tails), no main sprite to turn into ogscule here
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                FSprite sprite = sLeaser.sprites[i];
                if (sprite == null) continue;
                sprite.color = new Color(0f, 0f, 0f, 0f);
                sprite.isVisible = false;
            }
        }
    }
}