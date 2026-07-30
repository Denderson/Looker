using Looker.CWTs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Looker.Plugin;

namespace Looker.Regions
{
    public static class LCopies
    {
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
            if (player == null) return false;
            if (!PlayerCWT.TryGetData(player, out var data)) return false;
            if (!data.shouldSpawnCopies) return false;
            if (data.timeUntilChaser > 0) { data.timeUntilChaser--; return false; }
            if (CopyContact)
            {
                if (CopyContactTimer > 0) { CopyContactTimer--; }
                if (CopyContactTimer == 0) { CopyContact = false; }
                return false;
            }
            return player?.room != null && CheckMechanics(player.room, "migration", "WMPA")
                   && !player.dead && !player.inShortcut
                   && player.bodyChunks != null && player.bodyChunks.Length > 0;
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
                    positions[i] = player.bodyChunks[i].pos;

                bodyFrames.Add(new BodyFrame(player.room, positions));
                Trim(bodyFrames);
            }

            public void RecordSprites(RoomCamera.SpriteLeaser sLeaser, Vector2 camPos)
            {
                SpriteState[] states = new SpriteState[sLeaser.sprites.Length];
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    // Store sprites only, world position comes from bodyFrames.
                    states[i] = new SpriteState(sLeaser.sprites[i]);
                }

                spriteFrames.Add(new SpriteFrame(states));
                Trim(spriteFrames);
            }

            public void CheckCollision(Player player)
            {
                if (bodyFrames.Count <= (OptionsMenu.copyDelay.Value + 20))
                    return;

                for (int i = 1; i <= OptionsMenu.copyAmount.Value; i++)
                {
                    BodyFrame frame = GetDelayed(bodyFrames, (OptionsMenu.copyDelay.Value + 20) * i);
                    if (frame.room != player.room) continue;

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
                    int delay = (OptionsMenu.copyDelay.Value + 20) * (i + 1);
                    SpriteFrame spriteFrame = GetDelayed(spriteFrames, delay);
                    BodyFrame bodyFrame = GetDelayed(bodyFrames, delay);
                    DrawSet(copySprites[i], spriteFrame, bodyFrame, source, rCam, camPos);
                }
            }

            private static void HideSet(FSprite[] sprites)
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] != null)
                        sprites[i].isVisible = false;
                }
            }

            public void HideSprites()
            {
                for (int i = 0; i < copySprites.Count; i++)
                    for (int j = 0; j < copySprites[i].Length; j++)
                        if (copySprites[i][j] != null)
                            copySprites[i][j].isVisible = false;
            }

            public void ClearSprites()
            {
                for (int i = 0; i < copySprites.Count; i++)
                    for (int j = 0; j < copySprites[i].Length; j++)
                        copySprites[i][j]?.RemoveFromContainer();
                copySprites.Clear();
            }

            private void DrawSet(FSprite[] sprites, SpriteFrame spriteFrame, BodyFrame bodyFrame,
                                 RoomCamera.SpriteLeaser source, RoomCamera rCam, Vector2 camPos)
            {
                if (spriteFrame.states == null || spriteFrame.states.Length != sprites.Length)
                {
                    for (int i = 0; i < sprites.Length; i++)
                        sprites[i].isVisible = false;
                    return;
                }

                Vector2 liveScreenAnchor = source.sprites[0].x == 0 && source.sprites[0].y == 0
                    ? Vector2.zero
                    : new Vector2(source.sprites[0].x, source.sprites[0].y);

                Vector2 liveBodyScreen = bodyFrames.Count > 0
                    ? bodyFrames[bodyFrames.Count - 1].positions[0] - camPos
                    : Vector2.zero;

                Vector2 delayedBodyScreen = bodyFrame.positions.Length > 0
                    ? bodyFrame.positions[0] - camPos
                    : Vector2.zero;

                Vector2 offset = delayedBodyScreen - liveBodyScreen;

                for (int i = 0; i < sprites.Length; i++)
                {
                    FSprite sprite = sprites[i];
                    SpriteState state = spriteFrame.states[i];
                    FSprite sourceSprite = i < source.sprites.Length ? source.sprites[i] : null;
                    FContainer container = sourceSprite?.container ?? rCam.ReturnFContainer("Items");

                    if (sprite.container != container)
                    {
                        sprite.RemoveFromContainer();
                        container.AddChild(sprite);
                    }

                    state.Apply(sprite);

                    if (sourceSprite != null)
                    {
                        sprite.x = sourceSprite.x + offset.x;
                        sprite.y = sourceSprite.y + offset.y;
                        sprite.MoveBehindOtherNode(sourceSprite);
                    }
                    else
                    {
                        sprite.x += offset.x;
                        sprite.y += offset.y;
                    }
                }
            }

            private void EnsureSpriteSets(int spriteCount)
            {
                while (copySprites.Count < OptionsMenu.copyAmount.Value)
                    copySprites.Add(new FSprite[spriteCount]);

                for (int i = 0; i < copySprites.Count; i++)
                {
                    if (copySprites[i].Length != spriteCount)
                    {
                        for (int j = 0; j < copySprites[i].Length; j++)
                            copySprites[i][j]?.RemoveFromContainer();
                        copySprites[i] = new FSprite[spriteCount];
                    }

                    for (int j = 0; j < copySprites[i].Length; j++)
                    {
                        if (copySprites[i][j] == null)
                        {
                            copySprites[i][j] = new FSprite("pixel", true) { isVisible = false };
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
                    frames.RemoveAt(0);
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
            private readonly float rotation;
            private readonly float scaleX;
            private readonly float scaleY;
            private readonly float anchorX;
            private readonly float anchorY;
            private readonly Color color;
            private readonly float alpha;
            private readonly bool visible;
            public SpriteState(FSprite sprite)
            {
                element = sprite.element;
                shader = sprite.shader;
                rotation = sprite.rotation;
                scaleX = sprite.scaleX;
                scaleY = sprite.scaleY;
                anchorX = sprite.anchorX;
                anchorY = sprite.anchorY;
                color = sprite.color;
                alpha = sprite.alpha;
                visible = sprite.isVisible;
            }

            public void Apply(FSprite sprite)
            {
                sprite.element = element;
                sprite.shader = shader;
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