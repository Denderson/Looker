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
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using Watcher;
using static Looker.Plugin;
using LizardCosmetics;

namespace Looker.Regions
{
    public static class LGrove
    {
        // The icon ID, must not collide with vanilla or other mods
        public const int ogsculeNumber = 9000;

        // Size of the in-game ogscule sprite
        public const float ogsculeSize = 40f;

        public static IconSymbol.IconSymbolData CreatureSymbol_SymbolDataFromCreature(On.CreatureSymbol.orig_SymbolDataFromCreature orig, AbstractCreature creature)
        {
            IconSymbol.IconSymbolData value = orig(creature);
            if (!CheckMechanics(creature?.Room, "pillar", "WPGA")) return value;
            return new IconSymbol.IconSymbolData(creature.creatureTemplate.type, value.itemType, ogsculeNumber);
        }

        public static string CreatureSymbol_SpriteNameOfCreature(On.CreatureSymbol.orig_SpriteNameOfCreature orig, IconSymbol.IconSymbolData symbolData)
        {
            if (symbolData.intData == ogsculeNumber)
            {
                return ogsculeIcon;
            }
            return orig(symbolData);
        }
        public static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            string value = orig(itemType, intData);
            if (intData == ogsculeNumber)
            {
                return ogsculeIcon;
            }
            return value;
        }

        public static Color CreatureSymbol_ColorOfCreature(On.CreatureSymbol.orig_ColorOfCreature orig, IconSymbol.IconSymbolData symbolData)
        {
            if (symbolData.intData == ogsculeNumber && !OptionsMenu.colorfulOgscules.Value)
            {
                return Color.red;
            }
            return orig(symbolData);
        }

        public static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
        {
            if (intData == ogsculeNumber && !OptionsMenu.colorfulOgscules.Value)
            {
                return Color.red;
            }
            return orig(itemType, intData);
        }

        public static IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
        {
            IconSymbol.IconSymbolData? value = orig(item);
            if (!CheckMechanics(item?.Room, "pillar", "WPGA")) return value;
            return new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, item.type, ogsculeNumber);
        }
        public static bool TryGetOgsculeMainSprite(IDrawable obj, out int mainSprite, out Color? overrideColor)
        {
            overrideColor = null;
            mainSprite = -1;
            if (obj == null) return false;

            if (obj is GraphicsModule module)
            {
                if (module.owner is Creature creature)
                {
                    if (module is ScavengerGraphics scavGraphics)
                    {
                        Log.LogMessage("Scav override!");
                        mainSprite = scavGraphics.HeadSprite;
                        return true;
                    }
                    if (module is VultureGraphics vultureGraphics)
                    {
                        mainSprite = vultureGraphics.HeadSprite;
                        return true;
                    }
                    if (module is LizardGraphics lizardGraphics)
                    {
                        mainSprite = lizardGraphics.SpriteHeadStart;
                        overrideColor = lizardGraphics.effectColor;
                        return true;
                    }
                    mainSprite = creature.mainBodyChunkIndex;
                    if (mainSprite == -1) mainSprite = 0;
                    return true;
                }
                else if (module.owner is not null)
                {
                    mainSprite = 0;
                    return true;
                }
                return false;
            }

            if (obj is PhysicalObject)
            {
                mainSprite = 0;
                return true;
            }

            if (obj is LightSource light && light.tiedToObject is Creature) return true;
            if (obj is LizardBubble) return true;

            return false;
        }

        public static void ApplyOgsculeEffect(RoomCamera.SpriteLeaser sLeaser, int mainSprite, Color? overrideColor = null)
        {
            if (sLeaser?.sprites == null) return;

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                var sprite = sLeaser.sprites[i];
                if (sprite == null) continue;

                if (i == mainSprite)
                {
                    if (sprite.element?.name != ogsculeSprite) sprite.SetElementByName(ogsculeSprite);

                    sprite.height = ogsculeSize;
                    sprite.width = ogsculeSize;
                    sprite.rotation = 0f;
                    sprite.isVisible = true;
                    sprite.MoveToFront();

                    if (!OptionsMenu.colorfulOgscules.Value)
                    {
                        sprite.color = Color.red;
                    }
                    else if (overrideColor != null)
                    {
                        sprite.color = overrideColor.Value;
                    }
                }
                else
                {
                    sprite.isVisible = false;
                    sprite.color = new Color(0f, 0f, 0f, 0f);

                    if (sprite is TriangleMesh triMesh && triMesh.verticeColors != null)
                    {
                        for (int v = 0; v < triMesh.verticeColors.Length; v++)
                        {
                            triMesh.verticeColors[v] = new Color(0f, 0f, 0f, 0f);
                        }
                    }
                    else if (sprite is CustomFSprite customSprite && customSprite.verticeColors != null)
                    {
                        for (int v = 0; v < customSprite.verticeColors.Length; v++)
                        {
                            customSprite.verticeColors[v] = new Color(0f, 0f, 0f, 0f);
                        }
                    }
                }
            }
        }

        private static void MarkDirty(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, IDrawable obj)
        {
            if (!CheckMechanics(rCam?.room, "pillar", "WPGA")) return;
            if (!TryGetOgsculeMainSprite(obj, out int mainSprite, out Color? overrideColor)) return;

            if (!SLeaserCWT.TryGetData(sLeaser, out var data)) return;
            data.ogsculeSprite = mainSprite;
            data.dirty = true;
            data.overrideColor = overrideColor;
        }

        public static void GraphicsModule_DrawSprites(On.GraphicsModule.orig_DrawSprites orig, GraphicsModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            MarkDirty(sLeaser, rCam, self);
        }

        public static void ComplexGraphicsModule_DrawSprites(On.ComplexGraphicsModule.orig_DrawSprites orig, ComplexGraphicsModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            MarkDirty(sLeaser, rCam, self);
        }

        public static void GraphicsSubModule_DrawSprites(On.ComplexGraphicsModule.GraphicsSubModule.orig_DrawSprites orig, ComplexGraphicsModule.GraphicsSubModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (!CheckMechanics(rCam?.room, "pillar", "WPGA")) return;
            if (self.owner == null) return;

            if (!TryGetOgsculeMainSprite(self.owner, out int mainSprite, out Color? overrideColor)) return;

            if (!SLeaserCWT.TryGetData(sLeaser, out var data)) return;
            data.ogsculeSprite = mainSprite;
            data.dirty = true;
            data.overrideColor = overrideColor;
        }

        public static void SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(self, timeStacker, rCam, camPos);

            if (self?.drawableObject is GraphicsModule) return;
            MarkDirty(self, rCam, self?.drawableObject);
        }

        public static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);

            if (!CheckMechanics(self?.room, "pillar", "WPGA")) return;
            if (self.spriteLeasers == null) return;

            foreach (var sLeaser in self.spriteLeasers)
            {
                if (sLeaser == null) continue;
                if (!SLeaserCWT.TryGetData(sLeaser, out var state)) continue;
                if (!state.dirty) continue;
                ApplyOgsculeEffect(sLeaser, state.ogsculeSprite);
                state.dirty = false;
            }
        }
    }
}