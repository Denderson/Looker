using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using static Looker.Plugin;

namespace Looker
{
    public static class NothingToSeeHere
    {
        

        public static void SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
        {
            orig(self, eventName);
            if (eventName == "LookerTripleAffirmative" && self?.oracle?.room != null)
            {
                OptionsMenu.metSliver.Value = true;
                self.oracle.room.syncTicker = 0;
                AbstractPhysicalObject abstractPhysicalObject = new(self.oracle.room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, self.oracle.room.GetWorldCoordinate(self.oracle.firstChunk.pos), self.oracle.room.world.game.GetNewID());
                self.oracle.room.abstractRoom.AddEntity(abstractPhysicalObject);
                abstractPhysicalObject.RealizeInRoom();
                (abstractPhysicalObject.realizedObject as MoreSlugcats.SingularityBomb).Explode();
            }
        }

        public static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if (self.id == LookerEnums.lookerConversation)
            {
                OptionsMenu.metSliver.Value = false;
                self.events.Add(new Conversation.TextEvent(self, 0, "Hello", 0));
                self.events.Add(new Conversation.TextEvent(self, 0, "I am Sliver Of Straw", 0));
                self.events.Add(new Conversation.TextEvent(self, 0, "I made the triple affirmative", 0));
                self.events.Add(new Conversation.TextEvent(self, 0, "It was difficult but I managed to pull it together", 0));
                self.events.Add(new Conversation.SpecialEvent(self, 0, "LookerTripleAffirmative"));
            }
            orig(self);
        }



        public static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
        {
            if (nextAction == self.action)
            {
                return;
            }
            if (self.oracle.room.game.StoryCharacter == LookerEnums.looker)
            {
                nextAction = LookerEnums.meetLooker;
                var subBehavior = new SSOracleLooker(self);
                subBehavior.Activate(self.action, nextAction);
                self.currSubBehavior.Deactivate();
                Custom.Log($"Switching subbehavior to: {subBehavior.ID} from: {self.currSubBehavior.ID}");
                self.currSubBehavior = subBehavior;
                self.inActionCounter = 0;
                self.action = nextAction;
                return;
            }
            orig(self, nextAction);
        }



        public static string SLOracleBehaviorHasMark_NameForPlayer(On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized)
        {
            string text = orig(self, capitalized);
            if (self.oracle.room.game.StoryCharacter == LookerEnums.looker)
            {
                switch (UnityEngine.Random.value)
                {
                    case (< 0.1f): return self.Translate("little") + "looker";
                    case (< 0.2f): return self.Translate("little") + "Looker";
                    case (< 0.3f): return self.Translate("little") + "Observer";
                    case (< 0.4f): return self.Translate("little") + "Garner";
                    case (< 0.5f): return self.Translate("little") + "Peeper";
                    case (< 0.6f): return self.Translate("little") + "Pipe Cleaner";
                    case (< 0.7f): return self.Translate("little") + "Triple Affirmative";
                    case (< 0.8f): return self.Translate("little") + "Sofanitel";
                    case (< 0.9f): return self.Translate("little") + "Enor";
                    default: return self.Translate("little") + "Rain World: The Looker";
                }
            }
            return text;

        }

        public static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            Log.LogMessage("Spawning oracle!!");
            if (room.abstractRoom?.name.ToLowerInvariant() == "wssr_ai")
            {
                self.ID = Oracle.OracleID.SS;
            }
            orig(self, abstractPhysicalObject, room);
        }

        public static void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            Log.LogMessage("Ready for AI!");
            if (self.abstractRoom?.name.ToLowerInvariant() == "wssr_ai")
            {
                if (self.game?.StoryCharacter == null)
                {
                    Log.LogMessage("Fail 1!");
                    return;
                }
                if (self.game.StoryCharacter != LookerEnums.looker)
                {
                    Log.LogMessage("Fail 2!");
                    return;
                }
                if (self.world == null)
                {
                    Log.LogMessage("Fail 3!");
                    return;
                }
                Log.LogMessage("Ready for AI 2 !");
                Oracle obj3 = new(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self);
                self.AddObject(obj3);
                self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
                Log.LogMessage("Ready for AI 3 !");
            }
        }

        public static bool Is_Alive(Func<Oracle, bool> orig, Oracle self)
        {
            return orig(self) || (self.ID == Oracle.OracleID.SS && self?.room?.game?.StoryCharacter == LookerEnums.looker && OptionsMenu.metSliver.Value);
        }

        public static bool Is_Sliver(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            if (self?.oracle?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return true;
            }
            return orig(self);
        }

        public static bool Is_EP(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            if (self?.oracle?.room?.game?.StoryCharacter == LookerEnums.looker)
            {
                return false;
            }
            return orig(self);
        }
    }
}
