using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Looker.Plugin;
using BepInEx;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Music;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
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
using Looker.CWTs;

namespace Looker.Regions
{
    internal class LSunlit_Badlands
    {
        public static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            if (self.room != null && CheckMechanics(self.room, "alley", "WSKB"))
            {
                if (retractDarkness)
                {
                    darknessProgress = Math.Max(0f, darknessProgress - (0.0075f * OptionsMenu.darknessSpeed.Value));
                }
                else
                {
                    darknessProgress = Math.Min(1f, darknessProgress + (0.0010f * OptionsMenu.darknessSpeed.Value * (OptionsMenu.resetDarkness.Value ? 2 : 1)));
                }
                self.sofBlackFade = darknessProgress;
                self.effect_darkness = darknessProgress;
                self.lightBloomAlpha = 1f - darknessProgress;
            }
        }

        public static void LanternStick_Update(On.LanternStick.orig_Update orig, LanternStick self, bool eu)
        {
            orig(self, eu);
            if (CheckMechanics(self.room, "alley", "WSKB"))
            {
                foreach (Player player in self.room.PlayersInRoom)
                {
                    if (player?.abstractCreature?.realizedCreature != null && self?.lantern != null && Vector2.Distance(self.lantern.firstChunk.pos, player.firstChunk.pos) < 100 && PlayerCWT.TryGetData(player, out var data))
                    {
                        data.darknessImmunity = 120;
                    }
                }
            }
        }

        public static void ScavengerAbstractAI_InitGearUp(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            orig(self);
            if (self?.world?.game?.StoryCharacter == LookerEnums.looker)
            {
                if (self.world.region?.name == "WSKB")
                {
                    AbstractPhysicalObject abstractPhysicalObject4 = new(self.world, AbstractPhysicalObject.AbstractObjectType.Lantern, null, self.parent.pos, self.world.game.GetNewID());
                    self.world.GetAbstractRoom(self.parent.pos).AddEntity(abstractPhysicalObject4);
                    if (self.parent.stuckObjects.Count > 0 && self.parent.stuckObjects[0] != null) self.DropAndDestroy(self.parent.stuckObjects[0]);
                    new AbstractPhysicalObject.CreatureGripStick(self.parent, abstractPhysicalObject4, 0, carry: true);
                }
            }
        }

        public static void Lantern_Update(On.Lantern.orig_Update orig, Lantern self, bool eu)
        {
            orig(self, eu);
            if (CheckMechanics(self?.room, "alley", "WSKB") && self.stick == null)
            {
                foreach (Player player in self.room.PlayersInRoom)
                {
                    if (player.abstractCreature?.realizedCreature != null && Custom.DistLess(self.firstChunk.pos, player.firstChunk.pos, 100) && PlayerCWT.TryGetData(player, out var data))
                    {
                        data.darknessImmunity = 120;
                    }
                }
                if (darknessProgress > 0.8f && LanternCWT.TryGetData(self, out var lanterndata))
                {
                    bool inLight = false;
                    foreach (LightSource light in self.room.lightSources)
                    {
                        if (light?.pos != null && !light.noGameplayImpact && light.tiedToObject != self && Custom.DistLess(self.firstChunk.pos, light.pos, 100)) inLight = true;
                    }
                    if (!inLight)
                    {
                        lanterndata.health--;
                        if (UnityEngine.Random.value < 0.05f)
                        {
                            for (int i = 0; i < (240 - lanterndata.health) / 40; i++)
                            {
                                Vector2 vector = Custom.RNV();
                                self.room.AddObject(new Spark(self.firstChunk.pos + vector * (UnityEngine.Random.value * 20f), vector * Mathf.Lerp(4f, 10f, UnityEngine.Random.value), new Color(1f, 0.2f, 0f), null, 4, 18));
                            }
                        }
                        if (lanterndata.health == 0)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Vector2 vector = Custom.RNV();
                                self.room.AddObject(new Spark(self.firstChunk.pos + vector * (UnityEngine.Random.value * 25f), vector * Mathf.Lerp(8f, 16f, UnityEngine.Random.value), new Color(1f, 0.2f, 0f), null, 8, 23));
                            }
                            self.Destroy();
                        }
                    }
                    else
                    {
                        lanterndata.health = 200;
                    }
                }
            }
        }
    }
}
