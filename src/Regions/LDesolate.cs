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
    public static class LDesolate
    {
        public static void Pomegranate_Update(On.Pomegranate.orig_Update orig, Pomegranate self, bool eu)
        {
            orig(self, eu);
            if (CheckMechanics(self?.room, "desolate", "WTDB") && CWTs.PomegranateCWT.TryGetData(self, out var data))
            {
                if (data.cooldown > 0)
                {
                    data.cooldown--;
                    if (data.cooldown == 20 && self.firstChunk.vel.y == 0)
                    {
                        self.firstChunk.vel.y += 15;
                    }
                    return;
                }
                foreach (Player player in self.room.PlayersInRoom)
                {
                    if (self.disconnected && Vector2.Distance(self.firstChunk.pos, player.DangerPos) < 1200)
                    {
                        if (!OptionsMenu.legacyMelons.Value)
                        {
                            self.firstChunk.vel += Custom.DirVec(self.firstChunk.pos, player.DangerPos) * 25f;
                            data.cooldown = (int)(60 * OptionsMenu.melonCooldown.Value);
                        }
                        else
                        {
                            self.firstChunk.vel += Custom.DirVec(self.firstChunk.pos, player.DangerPos) * 5f;
                            data.cooldown--;
                            if (data.cooldown < -80)
                            {
                                data.cooldown = (int)(60 * OptionsMenu.melonCooldown.Value);
                            }
                        }
                        return;
                    }
                    if (!self.disconnected && Vector2.Distance(self.firstChunk.pos, player.DangerPos) < 800)
                    {
                        self.Disconnect();
                        data.cooldown = 80;
                        return;
                    }
                }
            }
        }

        public static void Pomegranate_EnterSmashedMode(On.Pomegranate.orig_EnterSmashedMode orig, Pomegranate self)
        {
            if (self.room?.game?.StoryCharacter == LookerEnums.looker && PomegranateCWT.TryGetData(self, out var data) && CheckMechanics(self.room, "desolate", "WTDB"))
            {
                data.cooldown += (int)(40 * OptionsMenu.melonCooldown.Value);
                return;
            }
            orig(self);
        }

        public static void Pomegranate_TerrainImpact(On.Pomegranate.orig_TerrainImpact orig, Pomegranate self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (self.room?.game?.StoryCharacter == LookerEnums.looker && self.firstChunk.vel.magnitude > 1f)
            {
                Vector2 vel = self.firstChunk.vel;
                vel.y *= -1f;
                orig(self, chunk, direction, speed, firstContact);
                self.firstChunk.vel = vel;
                return;
            }
            orig(self, chunk, direction, speed, firstContact);
        }

    }
}
