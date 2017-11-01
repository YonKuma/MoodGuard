using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using System.Threading.Tasks;

namespace MoodGuard
{
    class ModEntry : Mod
    {
        public static ModConfig Config;

        public Dictionary<long, byte> happinessMap;

        enum NightFixMode: int
        {
            Standard = 0,
            Increased = 1,
            Maximized = 2
        }

        NightFixMode nightFixMode;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            // Initialize happiness dictionary
            if (Config.NightFix.Enabled)
            {
                switch (Config.NightFix.Mode)
                {
                    case "Increased":
                        nightFixMode = NightFixMode.Increased;
                        break;
                    case "Maximized":
                        nightFixMode = NightFixMode.Maximized;
                        break;
                    case "Standard":
                    default:
                        nightFixMode = NightFixMode.Standard;
                        break;
                }
                Monitor.Log($"Mode is [{nightFixMode.ToString()}]", LogLevel.Info);
                happinessMap = new Dictionary<long, byte>();
                TimeEvents.TimeOfDayChanged += this.ControlEvents_TimeOfDayChanged;
            }
        }

        private void ControlEvents_TimeOfDayChanged(object sender, EventArgsIntChanged e)
        {
            if (nightFixMode == NightFixMode.Maximized)
            {
                foreach (FarmAnimal animal in Game1.getFarm().getAllFarmAnimals())
                {
                    animal.happiness = (byte)255;
                }
            }
            else
            {
                // Happiness is calculated correctly in the winter, so only fix it if it's not winter
                if (!Game1.currentSeason.Equals("winter"))
                {
                    // At 5:50pm, record animals' happiness
                    if (e.NewInt == 1750)
                    {
                        happinessMap = new Dictionary<long, byte>();
                        foreach (FarmAnimal animal in Game1.getFarm().getAllFarmAnimals())
                        {
                            happinessMap[animal.myID] = animal.happiness;
                        }
                    }
                    if (e.NewInt >= 1800)
                    {
                        // Each time change after that, if the animal is inside, reset the happiness to the last known good value
                        foreach (Building building in Game1.getFarm().buildings)
                        {
                            if (building.indoors != null && building.indoors.GetType() == typeof(AnimalHouse))
                            {
                                foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)((AnimalHouse)building.indoors).animals)
                                {
                                    if (!happinessMap.ContainsKey(animal.Key))
                                    {
                                        // This should only happen if the user cheats to get a new animal after 6pm
                                        happinessMap[animal.Key] = animal.Value.happiness;
                                        continue;
                                    }
                                    var happiness = (int)happinessMap[animal.Key];
                                    int newHappiness = (int)animal.Value.happiness;
                                    if (newHappiness >= happiness)
                                    {
                                        // Not sure why this would happen, but just in case
                                        happiness = newHappiness;
                                    }
                                    if (nightFixMode == NightFixMode.Increased)
                                    {
                                        // If the user config mode is Increased, add happiness for being safe in the stable after 6pm
                                        happiness = Math.Min(byte.MaxValue, (happiness + animal.Value.happinessDrain));
                                    }
                                    animal.Value.happiness = (byte)happiness;
                                    happinessMap[animal.Key] = (byte)happiness;
                                }
                            }
                        }
                        // If the animal is outside, record a new known good value
                        foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)Game1.getFarm().animals)
                        {
                            happinessMap[animal.Key] = animal.Value.happiness;
                        }

                    }
                }
            }
        }
    }
}
