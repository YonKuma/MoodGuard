using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using System.Threading.Tasks;

namespace NightChicken
{
    class ModEntry : Mod
    {
        public Dictionary<long, byte> happinessMap;

        public override void Entry(IModHelper helper)
        {
            // Initialize happiness dictionary
            happinessMap = new Dictionary<long, byte>();
            TimeEvents.TimeOfDayChanged += this.ControlEvents_TimeOfDayChanged;
        }

        private void ControlEvents_TimeOfDayChanged(object sender, EventArgsIntChanged e)
        {
            // TODO: At 6pm, record animals' happiness
            if (e.NewInt == 1750)
            {
                happinessMap = new Dictionary<long, byte>();
                foreach (FarmAnimal animal in Game1.getFarm().getAllFarmAnimals())
                {
                    happinessMap[animal.myID] = animal.happiness;
                }
            }
            // TODO: Each time change after that, if the happiness has lowered, reset it to the recorded value
            if (e.NewInt >= 1800)
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.indoors != null && building.indoors.GetType() == typeof(AnimalHouse))
                    {
                        foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)((AnimalHouse)building.indoors).animals)
                        {
                            var happiness = happinessMap[animal.Key];
                            animal.Value.happiness = happiness;

                            Monitor.Log($"Indoor animal [{animal.Value.displayName}] had its happiness reset to  [{happiness.ToString()}]", LogLevel.Info);
                        }
                    }
                }
                foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)Game1.getFarm().animals)
                {
                    happinessMap[animal.Key] = animal.Value.happiness;
                }

            }
            
        }
    }
}
