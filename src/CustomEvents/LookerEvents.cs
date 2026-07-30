using lsfUtils.DevtoolsObjects.EventRectangle;
using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Looker.Plugin;

namespace Looker.CustomEvents
{
    public static class LookerEvents
    {
        public static void RegisterLookerEvents()
        {
            EventLogic.RegisterEvent("LookerBathEnding", LookerBathEndingEvent);
        }

        public static void LookerBathEndingEvent(Room room, Player player, string eventValue, int currentTimer, out int timer)
        {
            timer = currentTimer;
            if (timer == -1)
            {
                timer = 80;
            }

            if (player == null || room?.game?.cameras == null || room.game.cameras.Length < 1)
            {
                Log.LogMessage("Error in EndingEvent!");
                return;
            }

            EventLogic.EndingEvent(room, player, eventValue, currentTimer, out timer);

            player.freezeControls = true;
            if (timer == 0)
            {
                SaveFileCode.SetBool(room.game.GetStorySession.saveState, SaveFileCode.bathEnding, true);
                SaveFileCode.SetString(room.game.GetStorySession.saveState, SaveFileCode.lastEndingDone, "BathEnding");
                room.game.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set("lookerEndingBath", 1);
                room.game.rainWorld.progression.SaveProgression(false, true);
            }
            return;
        }

        public static void LookerMaskEndingEvent(Room room, Player player, string eventValue, int currentTimer, out int timer)
        {
            timer = currentTimer;
            if (timer == -1)
            {
                timer = 80;
            }

            if (player == null || room?.game?.cameras == null || room.game.cameras.Length < 1)
            {
                Log.LogMessage("Error in EndingEvent!");
                return;
            }

            EventLogic.EndingEvent(room, player, eventValue, currentTimer, out timer);

            player.freezeControls = true;
            if (timer == 0)
            {
                SaveFileCode.SetBool(room.game.GetStorySession.saveState, SaveFileCode.maskEnding, true);
                SaveFileCode.SetString(room.game.GetStorySession.saveState, SaveFileCode.lastEndingDone, "MaskEnding");
                room.game.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set("lookerEndingMask", 1);
                room.game.rainWorld.progression.SaveProgression(false, true);
            }
            return;
        }

        public static void LookerLinkEndingEvent(Room room, Player player, string eventValue, int currentTimer, out int timer)
        {
            timer = currentTimer;
            if (timer == -1)
            {
                timer = 120;
            }

            if (player == null || room?.game?.cameras == null || room.game.cameras.Length < 1)
            {
                Log.LogMessage("Error in EndingEvent!");
                return;
            }

            EventLogic.EndingEvent(room, player, eventValue, currentTimer, out timer);

            player.freezeControls = true;
            if (timer == 0)
            {
                SaveFileCode.SetBool(room.game.GetStorySession.saveState, SaveFileCode.linkEnding, true);
                SaveFileCode.SetString(room.game.GetStorySession.saveState, SaveFileCode.lastEndingDone, "LinkEnding");
                room.game.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set("lookerEndingLink", 1);
                room.game.rainWorld.progression.SaveProgression(false, true);
            }
            return;
        }

        public static void LookerPuzzleEndingEvent(Room room, Player player, string eventValue, int currentTimer, out int timer)
        {
            timer = currentTimer;
            if (timer == -1)
            {
                timer = 120;
            }

            if (player == null || room?.game?.cameras == null || room.game.cameras.Length < 1)
            {
                Log.LogMessage("Error in EndingEvent!");
                return;
            }

            EventLogic.EndingEvent(room, player, eventValue, currentTimer, out timer);

            player.freezeControls = true;
            if (timer < 40)
            {
                player.sleepCurlUp = Mathf.Min(1f, player.sleepCurlUp + (1f / 40f));
            }
            if (timer == 0)
            {
                SaveFileCode.SetBool(room.game.GetStorySession.saveState, SaveFileCode.puzzleEnding, true);
                SaveFileCode.SetString(room.game.GetStorySession.saveState, SaveFileCode.lastEndingDone, "PuzzleEnding");
                room.game.rainWorld.progression.miscProgressionData.GetSlugBaseData().Set("lookerEndingPuzzle", 1);
                room.game.rainWorld.progression.SaveProgression(false, true);
            }
            return;
        }
    }
}
