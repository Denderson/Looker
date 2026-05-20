using Looker.CWTs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Looker.Plugin;

namespace Looker.Regions
{
    public static class LCopies
    {
        private const float KillDistance = 3f;
        private static bool CopyContact = false;
        private static int CopyContactTimer = 0;

        private static readonly ConditionalWeakTable<Player, Data> playerData = new();

        public static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (!IsCopyActive(self))
            {
                if (self != null && playerData.TryGetValue(self, out var inactiveData))
                {
                    inactiveData.ClearSprites();
                    inactiveData.bodyFrames.Clear();
                    inactiveData.spriteFrames.Clear();
                }
                return;
            }

            Data data = playerData.GetOrCreateValue(self);
            data.RecordBody(self);
            data.CheckCollision(self);
        }

        public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (!IsCopyActive(self?.player) || sLeaser?.sprites == null || rCam == null)
            {
                if (self?.player != null && playerData.TryGetValue(self.player, out var inactiveData))
                {
                    inactiveData.HideSprites();
                }
                return;
            }

            Data data = playerData.GetOrCreateValue(self.player);
            data.RecordSprites(sLeaser, camPos);
            data.DrawCopies(sLeaser, rCam, camPos);
        }

        public static bool IsCopyActive(this Player player)
        {
            if (player == null)
            {
                return false;
            }
            if (!PlayerCWT.TryGetData(player, out var data))
            {
                return false;
            }
            if (!data.shouldSpawnCopies)
            {
                return false;
            }
<<<<<<< Updated upstream
            if (data.timeUntilChaser > 0)
            {
                data.timeUntilChaser--;
                return false;
            }
            if (CopyContact)
            {
                if (CopyContactTimer > 0){CopyContactTimer--;}
                if (CopyContactTimer == 0){CopyContact = false;}
                return false;
            }

                return player?.room != null && CheckMechanics(player.room, "migration", "WMPA") && !player.dead && !player.inShortcut && player.bodyChunks != null && player.bodyChunks.Length > 0;
=======
            return player?.room != null && CheckMechanics(player.room, "migration", "WMPA") && !player.dead && !player.inShortcut && player.bodyChunks != null && player.bodyChunks.Length > 0;
>>>>>>> Stashed changes
        }

        private class Data
        {
            public readonly List<BodyFrame> bodyFrames = [];
            public readonly List<SpriteFrame> spriteFrames = [];
            private readonly List<FSprite[]> copySprites = [];

            public void RecordBody(Player player)
            {
                Vector2[] positions = new Vector2[player.bodyChunks.Length];
                for (int i = 0; i < player.bodyChunks.Length; i++)
                {
                    positions[i] = player.bodyChunks[i].pos;
                }

                bodyFrames.Add(new BodyFrame(player.room, positions));
                Trim(bodyFrames);
            }

            public void RecordSprites(RoomCamera.SpriteLeaser sLeaser, Vector2 camPos)
            {
                SpriteState[] states = new SpriteState[sLeaser.sprites.Length];
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    states[i] = new SpriteState(sLeaser.sprites[i], camPos);
                }

                spriteFrames.Add(new SpriteFrame(states));
                Trim(spriteFrames);
            }

            public void CheckCollision(Player player)
            {
                if (bodyFrames.Count <= (OptionsMenu.copyDelay.Value + 20))
                {
                    return;
                }

                for (int i = 1; i <= OptionsMenu.copyAmount.Value; i++)
                {
<<<<<<< Updated upstream
                    BodyFrame frame = GetDelayed(bodyFrames, (OptionsMenu.copyDelay.Value + 20) * i);
=======
                    int delay = DelayStep * i;
                    if (bodyFrames.Count <= delay + 1)
                    {
                        continue;
                    }
                    BodyFrame frame = GetDelayed(bodyFrames, delay);
>>>>>>> Stashed changes
                    if (frame.room != player.room)
                    {
                        continue;
                    }

                    for (int j = 0; j < player.bodyChunks.Length; j++)
                    {
                        for (int k = 0; k < frame.positions.Length; k++)
                        {
                            float killDistance = player.bodyChunks[j].rad + 6f;
                            if (RWCustom.Custom.DistLess(player.bodyChunks[j].pos, frame.positions[k], killDistance))
                            {
                                if (!OptionsMenu.weakerCopies.Value && !CheckEasyMode(player.room))
                                {
                                    player.Die();
                                }
                                else if (PlayerCWT.TryGetData(player, out var data) && player.dangerGraspTime == 0)
                                {
                                    player.SuperHardSetPosition(data.oldPipePosition);
                                    CopyContact = true;
                                    CopyContactTimer = 240;
                                    player.Stun(30);
                                }
                                return;
                            }
                        }
                    }
                }
            }

            public void DrawCopies(RoomCamera.SpriteLeaser source, RoomCamera rCam, Vector2 camPos)
            {
                EnsureSpriteSets(source.sprites.Length);

                for (int i = 0; i < OptionsMenu.copyAmount.Value; i++)
                {
<<<<<<< Updated upstream
                    SpriteFrame frame = GetDelayed(spriteFrames, (OptionsMenu.copyDelay.Value + 20) * (i + 1));
=======
                    int delay = DelayStep * (i + 1);
                    if (spriteFrames.Count <= delay)
                    {
                        HideSet(copySprites[i]);
                        continue;
                    }

                    SpriteFrame frame = GetDelayed(spriteFrames, delay);
>>>>>>> Stashed changes
                    DrawSet(copySprites[i], frame, source, rCam, camPos);
                }
            }

            private static void HideSet(FSprite[] sprites)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                    {
                        sprites[i].isVisible = false;
                    }
                }
            }

            public void HideSprites()
            {
                for (int i = 0; i < copySprites.Count; i++)
                {
                    for (int j = 0; j < copySprites[i].Length; j++)
                    {
                        if (copySprites[i][j] != null)
                        {
                            copySprites[i][j].isVisible = false;
                        }
                    }
                }
            }

            public void ClearSprites()
            {
                for (int i = 0; i < copySprites.Count; i++)
                {
                    for (int j = 0; j < copySprites[i].Length; j++)
                    {
                        copySprites[i][j]?.RemoveFromContainer();
                    }
                }
                copySprites.Clear();
            }

            private void DrawSet(FSprite[] sprites, SpriteFrame frame, RoomCamera.SpriteLeaser source, RoomCamera rCam, Vector2 camPos)
            {
                if (frame.states == null || frame.states.Length != sprites.Length)
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        sprites[i].isVisible = false;
                    }
                    return;
                }

                for (int i = 0; i < sprites.Length; i++)
                {
                    FSprite sprite = sprites[i];
                    SpriteState state = frame.states[i];
                    FSprite sourceSprite = i < source.sprites.Length ? source.sprites[i] : null;
                    FContainer container = sourceSprite?.container ?? rCam.ReturnFContainer("Items");

                    if (sprite.container != container)
                    {
                        sprite.RemoveFromContainer();
                        container.AddChild(sprite);
                    }

                    state.Apply(sprite, camPos);
                    if (sourceSprite != null)
                    {
                        sprite.MoveBehindOtherNode(sourceSprite);
                    }
                }
            }

            private void EnsureSpriteSets(int spriteCount)
            {
                while (copySprites.Count < OptionsMenu.copyAmount.Value)
                {
                    copySprites.Add(new FSprite[spriteCount]);
                }

                for (int i = 0; i < copySprites.Count; i++)
                {
                    if (copySprites[i].Length != spriteCount)
                    {
                        for (int j = 0; j < copySprites[i].Length; j++)
                        {
                            copySprites[i][j]?.RemoveFromContainer();
                        }
                        copySprites[i] = new FSprite[spriteCount];
                    }

                    for (int j = 0; j < copySprites[i].Length; j++)
                    {
                        if (copySprites[i][j] == null)
                        {
                            copySprites[i][j] = new FSprite("pixel", true)
                            {
                                isVisible = false
                            };
                        }
                    }
                }
            }

            private static T GetDelayed<T>(List<T> frames, int delay)
            {
                int index = Mathf.Max(0, frames.Count - 1 - delay);
                return frames[index];
            }

            private static void Trim<T>(List<T> frames)
            {
                while (frames.Count > (OptionsMenu.copyDelay.Value + 20) * OptionsMenu.copyAmount.Value + 5)
                {
                    frames.RemoveAt(0);
                }
            }
        }

        private readonly struct BodyFrame
        {
            public readonly Room room;
            public readonly Vector2[] positions;

            public BodyFrame(Room room, Vector2[] positions)
            {
                this.room = room;
                this.positions = positions;
            }
        }

        private readonly struct SpriteFrame
        {
            public readonly SpriteState[] states;

            public SpriteFrame(SpriteState[] states)
            {
                this.states = states;
            }
        }

        private readonly struct SpriteState
        {
            private readonly FAtlasElement element;
            private readonly FShader shader;
            private readonly Vector2 worldPos;
            private readonly float rotation;
            private readonly float scaleX;
            private readonly float scaleY;
            private readonly float anchorX;
            private readonly float anchorY;
            private readonly Color color;
            private readonly float alpha;
            private readonly bool visible;

            public SpriteState(FSprite sprite, Vector2 camPos)
            {
                element = sprite.element;
                shader = sprite.shader;
                worldPos = new Vector2(sprite.x + camPos.x, sprite.y + camPos.y);
                rotation = sprite.rotation;
                scaleX = sprite.scaleX;
                scaleY = sprite.scaleY;
                anchorX = sprite.anchorX;
                anchorY = sprite.anchorY;
                color = sprite.color;
                alpha = sprite.alpha;
                visible = sprite.isVisible;
            }

            public void Apply(FSprite sprite, Vector2 camPos)
            {
                sprite.element = element;
                sprite.shader = shader;
                sprite.x = worldPos.x - camPos.x;
                sprite.y = worldPos.y - camPos.y;
                sprite.rotation = rotation;
                sprite.scaleX = scaleX;
                sprite.scaleY = scaleY;
                sprite.anchorX = anchorX;
                sprite.anchorY = anchorY;
                sprite.color = color;
                sprite.alpha = alpha;
                sprite.isVisible = visible;
            }
        }
    }
}
