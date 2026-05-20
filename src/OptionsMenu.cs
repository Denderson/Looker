using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Looker
{
    public class OptionsMenu : OptionInterface
    {
        public static readonly Color unfinishedColor = new(0.85f, 0.35f, 0.4f);
        private OpCheckBox CheckBox(Configurable<bool> config, int x, int y, bool isUnfinished = false)
        {
            if (config == null)
            {
                Plugin.Log.LogError("Error with " + x + " " + y);
                return null;
            }
            OpCheckBox checkBox = new(config, x * 160, 503 - y * 80) { description = config.info.description };
            if (isUnfinished) checkBox.colorEdge = unfinishedColor;
            return checkBox;
        }

        private OpCheckBox CheckBox(Configurable<bool> config, int x, int y, Color checkboxColor)
        {
            if (config == null)
            {
                Plugin.Log.LogError("Error with " + x + " " + y);
                return null;
            }
            OpCheckBox checkBox = new(config, x * 160, 503 - y * 80) { description = config.info.description };
            checkBox.colorEdge = checkboxColor;
            return checkBox;
        }

        private static OpFloatSlider LookerFloatSlider(Configurable<float> config, int y, float decideMax, bool isUnfinished = false)
        {
            if (config == null)
            {
                Plugin.Log.LogError("Error with " + y);
                return null;
            }
            OpFloatSlider slider = new(config, new Vector2(0, 460 - y * 80), 100) { max = decideMax, description = config.info.description };
            if (isUnfinished) slider.colorEdge = unfinishedColor;
            if (isUnfinished) slider.colorLine = unfinishedColor;
            return slider;
        }

        private static OpFloatSlider LookerFloatSlider(Configurable<float> config, int y, float decideMax, Color sliderColor)
        {
            if (config == null)
            {
                Plugin.Log.LogError("Error with " + y);
                return null;
            }
            OpFloatSlider slider = new(config, new Vector2(0, 460 - y * 80), 100) { max = decideMax, description = config.info.description };
            slider.colorEdge = sliderColor;
            slider.colorLine = sliderColor;
            return slider;
        }

        private static OpLabel Label(string text, float x, float y, bool isUnfinished = false)
        {
            OpLabel label = new(x * 160 + 30, 500 - y * 80, text);
            if (isUnfinished) label.color = new Color(0.85f, 0.35f, 0.4f);
            return label;
        }

        private static OpLabel Label(string text, float x, float y, Color labelColor)
        {
            OpLabel label = new(x * 160 + 30, 500 - y * 80, text);
            label.color = labelColor;
            return label;
        }

        private static int _creditsY = -1;
        private static OpLabel CreditsLabel(string text, float x, int increase = 1)
        {
            if (x == 0) _creditsY += increase;
            return new OpLabel(200 * x, 500 - _creditsY * 25, text);
        }

        private static OpLabel BigLabel(string text, float y, bool isUnfinished = false)
        {
            OpLabel label = new(410, 480 - y * 80, text, true);
            if (isUnfinished) label.color = new Color(0.85f, 0.35f, 0.4f);
            return label;
        }

        private static OpLabel SliderLabel(string text, int y, bool isUnfinished = false)
        {
            OpLabel label = new(110, 460 - y * 80, text) { description = text };
            if (isUnfinished) label.color = new Color(0.85f, 0.35f, 0.4f);
            return label;
        }
        
        private static OpLabel SliderLabel(string text, int y, Color labelColor)
        {
            OpLabel label = new(110, 460 - y * 80, text) { description = text };
            label.color = labelColor;
            return label;
        }


        public OptionsMenu(Plugin plugin)
        {
            //general
            differentAbility = config.Bind("looker_differentAbility", false, new ConfigurableInfo("Replaces usual Looker ability with Watchers float"));
            enableGlow = config.Bind("looker_enableGlow", false, new ConfigurableInfo("Makes Looker have neuron glow effect"));
            checkpointWarps = config.Bind("looker_saveFromPortal", false, new ConfigurableInfo("Each portal entry counts as a hibernation"));
            spawnFileDifficulty = config.Bind("looker_spawnFileDifficulty", 2, new ConfigurableInfo("Decides how hard the creature spawns should be. 2 by default"));
            deathExplosion = config.Bind("looker_deathExplosion", true, new ConfigurableInfo("Looker causes an explosion along a vineboom sfx on death"));
            ripplespaceDuration = config.Bind("looker_ripplespaceDuration", 1f, new ConfigurableInfo("Multiplies karma flower effect duration"));


            //warb
            emergencyBreath = config.Bind("looker_emergencyBreath", false, new ConfigurableInfo("Regain all breath the first time you would drown per cycle")); 
            breathZoneSize = config.Bind("looker_breathZoneSize", 1f, new ConfigurableInfo("Multiplies the size of angler breath zones"));

            //warc
            stableMovement = config.Bind("looker_stableMovement", false, new ConfigurableInfo("Horizontal and vertical controls arent randomised"));
            controlAnnouncement = config.Bind("looker_controlAnnouncement", false, new ConfigurableInfo("Get informed on current randomised controls whenever you exit a pipe"));

            //ward
            normalGravity = config.Bind("looker_normalGravity", false, new ConfigurableInfo("Disables periodical zero gravity"));

            //ware


            //warf
            constantShelters = config.Bind("looker_constantShelters", false, new ConfigurableInfo("Disables the shelter randomisation mechanic"));

            //warg
            lizardsCanLeap = config.Bind("looker_keepLeap", true, new ConfigurableInfo("Makes The Surface lizards able to jump"));
            lizardsCanShield = config.Bind("looker_keepShield", true, new ConfigurableInfo("Makes The Surface lizards able to shield themselves"));
            strongerLizardChance = config.Bind("looker_strongerLizardChance", 80, new ConfigurableInfo("Chance for buffed lizards to spawn"));

            //wmpa
            weakerCopies = config.Bind("looker_weakerCopies", false, new ConfigurableInfo("Touching the copies teleports you instead of killing you"));
            legacyChaser = config.Bind("looker_legacyChaser", false, new ConfigurableInfo("Replaces current Migration Path mechanic with its old one"));
            copyAmount = config.Bind("looker_copyAmount", 5, new ConfigurableInfo("Determines the amount of slugcat copies that will appear"));
            copyDelay = config.Bind("looker_copyDelay", 50, new ConfigurableInfo("Changes the distance that copies will keep from you and each other"));

            //wpga
            colorfulOgscules = config.Bind("looker_colorfulOgscules", false, new ConfigurableInfo("Ogscules inherit color from the base creature or item"));

            //wpta
            weakerBroadcast = config.Bind("looker_lethalBroadcast", true, new ConfigurableInfo("Letting the signal fade in Signal Spires stuns you instead of killing you"));
            noSkyWhales = config.Bind("looker_noSkyWhales", false, new ConfigurableInfo("Disables the primary mechanic of Signal Spires, but Sky Whales no longer spawn"));
            broadcastingLeniencyTimer = config.Bind("looker_broadcastingLeniencyTimer", 1f, new ConfigurableInfo("Multiplies the time you can spend without a broadcast"));

            //wrfa
            weakerBarnacles = config.Bind("looker_lethalBarnacles", true, new ConfigurableInfo("Touching barnacles causes a stun instead of an explosion"));
            barnacleCap = config.Bind("looker_barnacleCap", true, new ConfigurableInfo("Barnacles cannot spawn if the room already has a lot of them"));
            barnacleRate = config.Bind("looker_barnacleRate", 1f, new ConfigurableInfo("Multiplies Barnacle spawn rate"));

            //wrfb
            moreJetfish = config.Bind("looker_moreJetfish", false, new ConfigurableInfo("Spawn a jetfish each time you exit a pipe"));

            //wrra
            noFrogStacking = config.Bind("looker_noFrogStacking", false, new ConfigurableInfo("Attaching a second frog no longer causes an explosion"));
            halvedPoison = config.Bind("looker_halvedPoison", false, new ConfigurableInfo("Halves poison from eating food"));
            frogRainSpeed = config.Bind("looker_frogRainSpeed", 1f, new ConfigurableInfo("Multiplies the speed at which the frogs fall from the sky"));

            //wska
            rainTimerMult = config.Bind("looker_rainTimerMult", 1f, new ConfigurableInfo("Multiplies the duration of the region's rain timer"));

            //wskb
            weakerDarkness = config.Bind("looker_lethalDarkness", true, new ConfigurableInfo("Darkness slows you down instead of killing you"));
            resetDarkness = config.Bind("looker_resetDarknessViaShortcuts", false, new ConfigurableInfo("Entering a pipe resets darkness mechanic, but darkness appears faster"));
            darknessSpeed = config.Bind("looker_darknessSpeed", 1f, new ConfigurableInfo("Multiplies the darkness speed"));

            //wskc
            smallerLightnings = config.Bind("looker_smallerLightnings", false, new ConfigurableInfo("Makes lightning hits normal-sized"));
            lessEvilLightnings = config.Bind("looker_lessEvilLightnings", false, new ConfigurableInfo("Lightnings no longer prioritise Looker"));
            lightningSpawnSpeed = config.Bind("looker_lightningSpawnSpeed", 1f, new ConfigurableInfo("Determines the speed at which lightning bolts appear"));

            //wtdb
            bouncierMelons = config.Bind("looker_bouncierMelons", true, new ConfigurableInfo("Melons bounce when they should break"));
            legacyMelons = config.Bind("looker_legacyMelons", false, new ConfigurableInfo("Makes melons FAR more difficult and chaotic"));
            melonCooldown = config.Bind("looker_melonCooldown", 1f, new ConfigurableInfo("Multiplies the melon cooldown between leaps"));

            //wvwa
            acidProtection = config.Bind("looker_acidProtection", false, new ConfigurableInfo("Acid doesn't kill when touched for a brief moment"));

            //wara
            easierFinale = config.Bind("looker_easierFinale", false, new ConfigurableInfo("Tones down all mechanics during the finale sequence"));

            //debug
            difficultyChosen = config.Bind("looker_difficultyChosen", false, new ConfigurableInfo("Setup for the difficulty selection menu"));
            metSliver = config.Bind("looker_metSliver", false, new ConfigurableInfo("Setup for WSSR gimmick"));
            devMode = config.Bind("looker_unfunMode", false, new ConfigurableInfo("Disables all code-based Looker gimmicks"));
        }

        public override void Initialize()
        {

            base.Initialize();

            Tabs = new[] { new OpTab(this, "General"), new OpTab(this, "Mechanics 1"), new OpTab(this, "Mechanics 2"), new OpTab(this, "Mechanics 3"), new OpTab(this, "Mechanics 4"), new OpTab(this, "Credits"), new OpTab(this, "Debug"){colorButton = unfinishedColor} };

            // Tab 1
            UIelement[] UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "General options", true), new OpLabel(160, 550, "(red means not implemeted yet)", true){color = unfinishedColor},

                Label("Different ability", 0, 0),
                CheckBox(differentAbility, 0, 0),

                Label("Neuron glow", 0, 1),
                CheckBox(enableGlow, 0, 1),

                Label("Checkpoint portals", 1, 0),
                CheckBox(checkpointWarps, 1, 0),

                Label("Death explosion", 1, 1),
                CheckBox(deathExplosion, 1, 1),

                SliderLabel("Ripplespace duration", 0, RainWorld.RippleGold),
                LookerFloatSlider(ripplespaceDuration, 0, 3, RainWorld.RippleGold),

                SliderLabel("Creature difficulty", 1),
                new OpSlider(spawnFileDifficulty, new Vector2(0, 380), 100){max = 3, description = OptionsMenu.spawnFileDifficulty.info.description}
            };
            Tabs[0].AddItems(UIArrayElements);


            // Tab 2
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Mechanics", true), new OpLabel(110, 550, "(red means not implemented yet)", true){color = unfinishedColor},

                Label("Emergency breath", 0, 0, new Color (0.42f, 0.56f, 0.7f)),
                CheckBox(emergencyBreath, 0, 0, new Color (0.42f, 0.56f, 0.7f)),
                SliderLabel("Breath zone size multiplier", 0, new Color (0.42f, 0.56f, 0.7f)),
                LookerFloatSlider(breathZoneSize, 0, 2, new Color (0.42f, 0.56f, 0.7f)),

                Label("Stable movement", 0, 1, new Color(0.78f, 0.47f, 0.25f)),
                CheckBox(stableMovement, 0, 1, new Color(0.78f, 0.47f, 0.25f)),
                Label("Control announcement", 1, 1, new Color(0.78f, 0.47f, 0.25f)),
                CheckBox(controlAnnouncement, 1, 1, new Color(0.78f, 0.47f, 0.25f)),

                Label("Normal gravity", 0, 2, new Color(0.49f, 0.33f, 0.79f)),
                CheckBox(normalGravity, 0, 2, new Color(0.49f, 0.33f, 0.79f)),



                Label("Constant Shelters", 0, 4, new Color(0.58f, 0.65f, 0.78f)),
                CheckBox(constantShelters, 0, 4, new Color(0.58f, 0.65f, 0.78f)),

                Label("Lizards can leap", 0, 5, new Color(0.62f, 0.5f, 0.47f)),
                CheckBox(lizardsCanLeap, 0, 5, new Color(0.62f, 0.5f, 0.47f)),
                Label("Lizards can shield", 1, 5, new Color(0.62f, 0.5f, 0.47f)),
                CheckBox(lizardsCanShield, 1, 5, new Color(0.62f, 0.5f, 0.47f)),
                SliderLabel("Stronger lizard chance", 5, new Color(0.62f, 0.5f, 0.47f)),
                new OpSlider(strongerLizardChance, new Vector2(0, 60), 100){min = 0, max = 100, description = OptionsMenu.strongerLizardChance.info.description, colorEdge = new Color(0.62f, 0.5f, 0.47f), colorLine = new Color(0.62f, 0.5f, 0.47f)},
            };
            Tabs[1].AddItems(UIArrayElements);

            // Tab 3
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Mechanics", true), new OpLabel(110, 550, "(red means not implemented yet)", true){color = unfinishedColor},

                Label("Weaker copies", 0, 0, new Color(0.7f, 0.55f, 0.53f)),
                CheckBox(weakerCopies, 0, 0, new Color(0.7f, 0.55f, 0.53f)),
                Label("Legacy chaser", 1, 0, new Color(0.7f, 0.55f, 0.53f)),
                CheckBox(legacyChaser, 1, 0, new Color(0.7f, 0.55f, 0.53f)),
                SliderLabel("Copy amount", 0, new Color(0.7f, 0.55f, 0.53f)),
                new OpSlider(copyAmount, new Vector2(0, 460), 100){min = 1, max = 10, description = OptionsMenu.copyAmount.info.description, colorEdge = new Color(0.7f, 0.55f, 0.53f), colorLine = new Color(0.7f, 0.55f, 0.53f)},
                new OpLabel(350, 460, "Copy delay"){color =  new Color(0.7f, 0.55f, 0.53f)},
                new OpSlider(copyDelay, new Vector2(240, 460), 100){min = 0, max = 100, description = OptionsMenu.copyDelay.info.description, colorEdge = new Color(0.7f, 0.55f, 0.53f), colorLine = new Color(0.7f, 0.55f, 0.53f)},

                Label("Colorful ogscules", 0, 1, new Color(0.28f, 0.85f, 0.66f)),
                CheckBox(colorfulOgscules, 0, 1, new Color(0.28f, 0.85f, 0.66f)),

                Label("Weaker broadcast", 0, 2, new Color(0.89f, 0.51f, 0.69f)),
                CheckBox(weakerBroadcast, 0, 2, new Color(0.89f, 0.51f, 0.69f)),
                Label("Alternate broadcast", 1, 2, new Color(0.89f, 0.51f, 0.69f)),
                CheckBox(noSkyWhales, 1, 2, new Color(0.89f, 0.51f, 0.69f)),
                SliderLabel("Broadcast leniency", 2, new Color(0.89f, 0.51f, 0.69f)),
                LookerFloatSlider(broadcastingLeniencyTimer, 2, 5, new Color(0.89f, 0.51f, 0.69f)),

                Label("Weaker barnacles", 0, 3, new Color(0.38f, 0.7f, 0.89f)),
                CheckBox(weakerBarnacles, 0, 3, new Color(0.38f, 0.7f, 0.89f)),
                Label("Barnacle cap", 1, 3, new Color(0.38f, 0.7f, 0.89f)),
                CheckBox(barnacleCap, 1, 3, new Color(0.38f, 0.7f, 0.89f)),
                SliderLabel("Barnacle spawn rate", 3, new Color(0.38f, 0.7f, 0.89f)),
                LookerFloatSlider(barnacleRate, 3, 3, new Color(0.38f, 0.7f, 0.89f)),

                Label("More jetfish", 0, 4, new Color(0.42f, 0.7f, 0.65f)),
                CheckBox(moreJetfish, 0, 4, new Color(0.42f, 0.7f, 0.65f)),

                Label("No frog stacking", 0, 5, new Color(0.7f, 0.42f, 0.4f)),
                CheckBox(noFrogStacking, 0, 5, new Color(0.7f, 0.42f, 0.4f)),
                Label("Halved poison", 1, 5, new Color(0.7f, 0.42f, 0.4f)),
                CheckBox(halvedPoison, 1, 5, new Color(0.7f, 0.42f, 0.4f)),
                SliderLabel("Frog rain multiplier", 5, new Color(0.7f, 0.42f, 0.4f)),
                LookerFloatSlider(frogRainSpeed, 5, 5, new Color(0.7f, 0.42f, 0.4f))
            };
            Tabs[2].AddItems(UIArrayElements);

            // Tab 4
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Mechanics", true), new OpLabel(110, 550, "(red means not implemented yet)", true){color = unfinishedColor},

                SliderLabel("Rain timer multiplier", 0, new Color(0.62f, 0.66f, 0.7f)),
                LookerFloatSlider(rainTimerMult, 0, 2, new Color(0.62f, 0.66f, 0.7f)),

                Label("Weaker darkness", 0, 1, new Color(0.95f, 0.72f, 0.74f)),
                CheckBox(weakerDarkness, 0, 1, new Color(0.95f, 0.72f, 0.74f)),
                Label("Reset darkness on shortcut", 1, 1, new Color(0.95f, 0.72f, 0.74f)),
                CheckBox(resetDarkness, 1, 1, new Color(0.95f, 0.72f, 0.74f)),
                SliderLabel("Darkness speed", 1, new Color(0.95f, 0.72f, 0.74f)),
                LookerFloatSlider(darknessSpeed, 1, 3, new Color(0.95f, 0.72f, 0.74f)),

                Label("Smaller lightnings", 0, 2, new Color(0.62f, 0.62f, 0.7f)),
                CheckBox(smallerLightnings, 0, 2, new Color(0.62f, 0.62f, 0.7f)),
                Label("Less evil lightnings", 1, 2, new Color(0.62f, 0.62f, 0.7f)),
                CheckBox(lessEvilLightnings, 1, 2, new Color(0.62f, 0.62f, 0.7f)),
                SliderLabel("Lightning spawn rate", 2, new Color(0.62f, 0.62f, 0.7f)),
                LookerFloatSlider(lightningSpawnSpeed, 2, 3, new Color(0.62f, 0.62f, 0.7f)),

                Label("Bouncier melons", 0, 3, new Color(0.69f, 0.7f, 0.67f)),
                CheckBox(bouncierMelons, 0, 3, new Color(0.69f, 0.7f, 0.67f)),
                Label("Legacy melons", 1, 3, new Color(0.69f, 0.7f, 0.67f)),
                CheckBox(legacyMelons, 1, 3, new Color(0.69f, 0.7f, 0.67f)),
                SliderLabel("Melon cooldown", 3, new Color(0.69f, 0.7f, 0.67f)),
                LookerFloatSlider(melonCooldown, 3, 3, new Color(0.69f, 0.7f, 0.67f)),

                Label("Acid protection", 0, 4, unfinishedColor), //new Color(0.59f, 0.65f, 0.42f)
                CheckBox(acidProtection, 0, 4, unfinishedColor) //new Color(0.59f, 0.65f, 0.42f)
            };
            Tabs[3].AddItems(UIArrayElements);

            //Tab 5
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Mechanics", true), new OpLabel(110, 550, "(red means not implemented yet)", true){color = unfinishedColor},

                Label("Easier finale", 0, 0, new Color(0.93f, 0.82f, 0.57f)),
                CheckBox(easierFinale, 0, 0, new Color(0.93f, 0.82f, 0.57f)),
            };


            Tabs[4].AddItems(UIArrayElements);
            // Tab 6
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Thanks to our playtesters!", true),
                CreditsLabel("OverpoweredPizza", 0),
                CreditsLabel("Ivvy", 1),
                CreditsLabel("Greytail", 2),
                CreditsLabel("T1kva", 0),
                CreditsLabel("Lise", 1),
                CreditsLabel("Mockey_Mouse", 2),
                CreditsLabel("Narreator", 0),
                CreditsLabel("#1 Rot Lover", 1),
                CreditsLabel("Chad The Chad", 2),
                CreditsLabel("Lychi", 0),
                CreditsLabel("Gal3o", 1),
                CreditsLabel("meme_man", 2),
                //opCreditsLabel("Império Otomano", 0),
                CreditsLabel("Thysi", 0),
                CreditsLabel("Watvera WaNaBe", 1),

                new OpLabel(0, 450 - _creditsY * 25, "Special thanks to people who helped develop the mod!", true),

                CreditsLabel("The Local Group for custom Looker threat music", 0, 4),
                CreditsLabel("FrogTurtle56 for custom Looker sleep screen", 0, 2),
                CreditsLabel("Pebbel for server organising and Playtesting", 0, 2),
                CreditsLabel("Meme for pearl writing and Playtesting", 0, 2),
                CreditsLabel("hamborgirl :3 for the thumbnail and ending arts", 0, 2) 
            };
            Tabs[5].AddItems(UIArrayElements);

            // Tab 7
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Debug", true){color = unfinishedColor}, new OpLabel(160, 550, "(for debugging purposes obviously)", true){color = unfinishedColor},

                Label("Dev mode", 0, 0, unfinishedColor),
                CheckBox(devMode, 0, 0, unfinishedColor),

                Label("Difficulty chosen", 0, 1, unfinishedColor),
                CheckBox(difficultyChosen, 0, 1, unfinishedColor),

                Label("Reached Sliver", 0, 2, unfinishedColor),
                CheckBox(metSliver, 0, 2, unfinishedColor)
            };
            Tabs[6].AddItems(UIArrayElements);
        }

        public static Configurable<bool>

            differentAbility, checkpointWarps, deathExplosion, enableGlow, //general

            emergencyBreath, //desalination
            stableMovement, controlAnnouncement, //fetid glen
            normalGravity, //cold storage
                           //heat ducts
            constantShelters, //aether ridge
            lizardsCanLeap, lizardsCanShield, //the surface
            weakerCopies, legacyChaser, //migration path
            colorfulOgscules, //pillar grove
            weakerBroadcast, noSkyWhales, //signal spires
            weakerBarnacles, barnacleCap, //coral caves
            moreJetfish, //turbulent pump
            noFrogStacking, halvedPoison, //rusted wrecks TODO
                           //torrential railways
            weakerDarkness, resetDarkness, //sunbaked alley
            smallerLightnings, lessEvilLightnings, //stormy coast
            bouncierMelons, legacyMelons, //desolate tract
            acidProtection, //verdant waterways
            easierFinale, //shattered terrace

            difficultyChosen, metSliver, devMode; //debug

        public static Configurable<float>

            ripplespaceDuration, //general

            breathZoneSize, //desalination
                            //fetid glen
                            //cold storage
                            //heat ducts
                            //aether ridge
                            //the surface
            broadcastingLeniencyTimer, //signal spires
            barnacleRate, //coral caves
            frogRainSpeed, //rusted wrecks
            rainTimerMult, //torrential railways
            darknessSpeed, //sunbaked alley
            lightningSpawnSpeed, //stormy coast
            melonCooldown; //desolate tract
                            //shattered terrace


        public static Configurable<int>

            spawnFileDifficulty, //general
            strongerLizardChance,//the surface
            copyAmount, copyDelay; //migration path
    }
}
