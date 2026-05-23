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

namespace Looker.Regions
{
    public static class LCoral_Migration
    {
        public static void Barnacle_Collide(On.Watcher.Barnacle.orig_Collide orig, Barnacle self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (CheckMechanics(self.room, "caves", "WRFA") && otherObject is Creature creature && creature.Stunned)
            {
                if (creature is Player && (creature as Player).newToRoomInvinsibility > 0)
                {
                    return;
                }
                self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk);
                for (int i = 0; i < 8; i++)
                {
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                }
                for (int k = 0; k < otherObject.bodyChunks.Length; k++)
                {
                    otherObject.bodyChunks[k].vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                    otherObject.bodyChunks[k].pos += Custom.RNV() * 6f * UnityEngine.Random.value;
                }
                if (OptionsMenu.weakerBarnacles.Value || CheckEasyMode(self.room))
                {
                    creature.LoseAllGrasps();
                    creature.Stun(120);
                    self.room.AddObject(new CreatureSpasmer(creature, allowDead: false, creature.stun));
                }
                else
                {
                    creature.LoseAllGrasps();
                    creature.Die();
                    self.room.AddObject(new CreatureSpasmer(creature, allowDead: true, 120));
                }
            }
        }
        public static void Room_Update(On.Room.orig_Update orig, Room self)
        {
            if (self != null) orig(self);
            if (self?.abstractRoom == null || self.PlayersInRoom == null && self.PlayersInRoom.Count() <= 0)
            {
                return;
            }
            int num = 0;
            if (self.abstractRoom.name == "WORA_AI")
            {
                self.game.cameras[0].hud.karmaMeter.forceVisibleCounter = Mathf.Max(self.game.cameras[0].hud.karmaMeter.forceVisibleCounter, 200);
            }
            if (CheckMechanics(self, "caves", "WRFA"))
            {
                if (self.syncTicker % (int)(200f / OptionsMenu.barnacleRate.Value) != 1)
                {
                    return;
                }
                for (int i = 0; i < self.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < self.physicalObjects[i].Count; j++)
                    {
                        if (self.physicalObjects[i][j] is KnotSpawn knot)
                        {
                            AbstractCreature abstractCreature = new(self.world, StaticWorld.GetCreatureTemplate(WatcherEnums.CreatureTemplateType.Barnacle), null, knot.abstractPhysicalObject.pos, self.game.GetNewID())
                            {
                                saveCreature = false
                            };
                            self.abstractRoom.AddEntity(abstractCreature);
                            abstractCreature.RealizeInRoom();
                            self.AddObject(new ShockWave(new Vector2((float)knot.abstractPhysicalObject.pos.x * 20f, (float)knot.abstractPhysicalObject.pos.y * 20f), 300f, 0.2f, 15, false));
                            knot.Destroy();
                        }
                        if (self.physicalObjects[i][j] is Barnacle)
                        {
                            num++;
                        }
                    }
                }
                if (num > 5 * self.cameraPositions.Length && OptionsMenu.barnacleCap.Value) return;
                int k = 0;
                IntVector2 intVector = new(0, 0);
                while (k < 100)
                {
                    int num2 = UnityEngine.Random.Range(1, self.Tiles.GetLength(0) - 1);
                    int num3 = UnityEngine.Random.Range(1, self.Tiles.GetLength(1) - 1);
                    if (!self.Tiles[num2, num3].Solid)
                    {
                        intVector = new IntVector2(num2, num3);
                        break;
                    }
                    k++;
                }
                if (intVector.x != 0 && intVector.y != 0)
                {
                    AbstractPhysicalObject abstractCreature = new(self.world, WatcherEnums.AbstractObjectType.KnotSpawn, null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), self.game.GetNewID());
                    self.abstractRoom.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();
                }

            }
            if (CheckMechanics(self, "rusted", "WRRA") && self.world.rainCycle.TimeUntilRain < 400)
            {
                if (self.syncTicker % (int)(40f / OptionsMenu.frogRainSpeed.Value) != 1)
                {
                    return;
                }
                for (int i = 0; i < self.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < self.physicalObjects[i].Count; j++)
                    {
                        if (self.physicalObjects[i][j] is Frog)
                        {
                            num++;
                        }
                    }
                }
                if (num < 30)
                {
                    IntVector2 intVector = new(0, 0);
                    bool flag = false;
                    for (int e = 0; e < self.abstractRoom.nodes.Length; e++)
                    {
                        if (self.abstractRoom.nodes[e].type == AbstractRoomNode.Type.SkyExit)
                        {
                            flag = true;
                        }
                    }
                    int i = 10;
                    while (flag && i > 0)
                    {
                        int r = UnityEngine.Random.Range(0, self.abstractRoom.nodes.Length);
                        if (self.abstractRoom.nodes[r].type == AbstractRoomNode.Type.SkyExit && self.LocalCoordinateOfNode(r) != null && self.LocalCoordinateOfNode(r).Tile != null)
                        {
                            intVector = self.LocalCoordinateOfNode(r).Tile;
                            flag = false;
                        }
                        i--;
                    }
                    if (intVector.x != 0 && intVector.y != 0)
                    {
                        AbstractCreature abstractCreature = new(self.world, StaticWorld.GetCreatureTemplate(WatcherEnums.CreatureTemplateType.Frog), null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), self.game.GetNewID())
                        {
                            saveCreature = false
                        };
                        abstractCreature.pos = new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1);
                        self.abstractRoom.AddEntity(abstractCreature);
                        abstractCreature.RealizeInRoom();
                        abstractCreature.realizedCreature.RandomChunk.vel = new Vector2(0, -1);
                    }
                }
            }
            if (CheckMechanics(self, "daemon", "WRSA") && self.abstractRoom.name != "WRSA_WEAVER" && !self.abstractRoom.name.Contains("TREE"))
            {
                if (self.syncTicker % 40 != 1)
                {
                    return;
                }
                for (int i = 0; i < self.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < self.physicalObjects[i].Count; j++)
                    {
                        if (self.physicalObjects[i][j] is KnotSpawn)
                        {
                            num++;
                        }
                    }
                }
                if (num > SaveFileCode.LinkCount(self.game.GetStorySession.saveState)) return;
                int k = 0;
                IntVector2 intVector = new(0, 0);
                while (k < 100)
                {
                    int num2 = UnityEngine.Random.Range(1, self.Tiles.GetLength(0) - 1);
                    int num3 = UnityEngine.Random.Range(1, self.Tiles.GetLength(1) - 1);
                    if (!self.Tiles[num2, num3].Solid)
                    {
                        intVector = new IntVector2(num2, num3);
                        break;
                    }
                    k++;
                }
                if (intVector.x != 0 && intVector.y != 0)
                {
                    AbstractPhysicalObject abstractCreature = new(self.world, WatcherEnums.AbstractObjectType.KnotSpawn, null, new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1), self.game.GetNewID());
                    self.abstractRoom.AddEntity(abstractCreature);
                    abstractCreature.RealizeInRoom();
                    self.AddObject(new ShockWave(new Vector2((float)abstractCreature.pos.x * 20f, (float)abstractCreature.pos.y * 20f), 200f, 0.1f, 15, false));
                }
            }
            if (self.abstractRoom.name.ToLowerInvariant() == "wssr_ai" && OptionsMenu.metSliver.Value)
            {
                if (self.abstractRoom.name.ToLowerInvariant() == "wssr_ai" && OptionsMenu.metSliver.Value)
                {
                    if (self.syncTicker > 40)
                    {
                        NukeRoom(self);
                    }
                }
                else if (self.abstractRoom.name.ToLowerInvariant() == "wssr_ai" && !OptionsMenu.metSliver.Value)
                {
                    self.syncTicker = 0;
                }
            }
        }

        public static void NukeRoom(Room room)
        {
            if (room != null)
            {
                int k = 0;
                IntVector2 intVector = new(0, 0);
                while (k < 100)
                {
                    int num2 = UnityEngine.Random.Range(1, room.Tiles.GetLength(0) - 1);
                    int num3 = UnityEngine.Random.Range(1, room.Tiles.GetLength(1) - 1);
                    if (!room.Tiles[num2, num3].Solid)
                    {
                        intVector = new IntVector2(num2, num3);
                        break;
                    }
                    k++;
                }
                if (intVector.x != 0 && intVector.y != 0)
                {
                    AbstractPhysicalObject abstractPhysicalObject = new(room.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, room.GetWorldCoordinate(intVector), room.world.game.GetNewID());
                    room.abstractRoom.AddEntity(abstractPhysicalObject);
                    abstractPhysicalObject.RealizeInRoom();
                    (abstractPhysicalObject.realizedObject as ScavengerBomb).Explode(null);
                    room.ScreenMovement(null, new Vector2(0f, 0f), UnityEngine.Random.value * 10);
                    if (room.syncTicker % 40 == 0)
                    {
                        room.game.framesPerSecond /= 2;
                    }
                    if (room.syncTicker >= 120 || room.game.framesPerSecond < 10)
                    {
                        while (true)
                        {
                            AbstractPhysicalObject finalBombs = new(room.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, room.GetWorldCoordinate(intVector), room.world.game.GetNewID());
                            room.abstractRoom.AddEntity(finalBombs);
                            finalBombs.RealizeInRoom();
                            (finalBombs.realizedObject as ScavengerBomb).Explode(null);
                        }
                    }
                }
            }
            if (UnityEngine.Random.value < 0.5f)
            {
                NukeRoom(room);
                return;
            }
            
        }
    }
}
