using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

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
                TimeEvents.TimeOfDayChanged += this.TimeEvents_TimeOfDayChanged;
                InputEvents.ButtonPressed += this.InputEvents_ButtonPressed;
            }
        }

        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            Monitor.Log($"Button pressed", LogLevel.Info);
            
            Game1.oldMouseState = new MouseState(Game1.oldMouseState.X, Game1.oldMouseState.Y, Game1.oldMouseState.ScrollWheelValue, Game1.oldMouseState.LeftButton, ButtonState.Released, Game1.oldMouseState.RightButton, Game1.oldMouseState.XButton1, Game1.oldMouseState.XButton2);
            // Activation Requirements:
            // * Is Action Button
            // * Is within player's reach
            // * Player has one of the vulnerable professions
            // * Player is on the farm or in a farm building
            // * grabTile has collision with an animal
            // * Animal has not been pet
            // * The particular profession and animal combination will cause overflow
            if (e.IsActionButton)
            {
                ICursorPosition cursorPosition = e.Cursor;
                Microsoft.Xna.Framework.Vector2 grabTile = cursorPosition.GrabTile;
                if (Utility.tileWithinRadiusOfPlayer((int)grabTile.X, (int)grabTile.Y, 1, Game1.player))
                {
                    StardewValley.Farmer farmer = Game1.player;
                    if (farmer.FarmerSprite.pauseForSingleAnimation)
                        return;
                    Monitor.Log($"Tile within player radius X: [{grabTile.X}] Y: [{grabTile.Y}]", LogLevel.Info);
                    if (farmer.professions.Contains(2) || farmer.professions.Contains(3))
                    {
                        Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)grabTile.X * Game1.tileSize, (int)grabTile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                        if (Game1.currentLocation is Farm farm)
                        {
                            foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)farm.animals)
                            {
                                if (Game1.timeOfDay >= 1900 && !animal.Value.isMoving())
                                    return;
                                if (animal.Value.GetBoundingBox().Intersects(rectangle))
                                {
                                    if (!animal.Value.wasPet
                                        && (
                                            (farmer.professions.Contains(3) && !animal.Value.isCoopDweller())
                                            || (farmer.professions.Contains(2) && animal.Value.isCoopDweller())
                                           )
                                       )
                                    {
                                        e.SuppressButton();
                                        Monitor.Log($"Pet override triggered", LogLevel.Info);
                                        this.pet(animal.Value, farmer);
                                        return;
                                    }
                                }
                            }
                        } else if (Game1.currentLocation is AnimalHouse animalHouse)
                        {
                            foreach (KeyValuePair<long, FarmAnimal> animal in (Dictionary<long, FarmAnimal>)animalHouse.animals)
                            {
                                if (Game1.timeOfDay >= 1900 && !animal.Value.isMoving())
                                    return;
                                if (!animal.Value.wasPet
                                    && (
                                        (farmer.professions.Contains(3) && !animal.Value.isCoopDweller())
                                        || (farmer.professions.Contains(2) && animal.Value.isCoopDweller())
                                       )
                                   )
                                {
                                    if (animal.Value.GetBoundingBox().Intersects(rectangle))
                                    {
                                        e.SuppressButton();
                                        Monitor.Log($"Pet override triggered", LogLevel.Info);
                                        this.pet(animal.Value, farmer);
                                        return;
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        public void pet(FarmAnimal animal, StardewValley.Farmer farmer)
        {
            farmer.Halt();
            farmer.faceGeneralDirection(animal.position, 0);
            animal.Halt();
            animal.sprite.StopAnimation();
            animal.uniqueFrameAccumulator = -1;
            switch (Game1.player.FacingDirection)
            {
                case 0:
                    animal.sprite.currentFrame = 0;
                    break;
                case 1:
                    animal.sprite.currentFrame = 12;
                    break;
                case 2:
                    animal.sprite.currentFrame = 8;
                    break;
                case 3:
                    animal.sprite.currentFrame = 4;
                    break;
            }
            animal.wasPet = true;
            animal.friendshipTowardFarmer = Math.Min(1000, animal.friendshipTowardFarmer + 15);
            if (farmer.professions.Contains(3) && !animal.isCoopDweller())
            {
                animal.friendshipTowardFarmer = Math.Min(1000, animal.friendshipTowardFarmer + 15);
                animal.happiness = (byte) Math.Min((int)byte.MaxValue, ((uint)animal.happiness + (uint)Math.Max(5, 40 - (int)animal.happinessDrain)));
            }
            else if (farmer.professions.Contains(2) && animal.isCoopDweller())
            {
                animal.friendshipTowardFarmer = Math.Min(1000, animal.friendshipTowardFarmer + 15);
                animal.happiness = (byte) Math.Min((int)byte.MaxValue, ((uint)animal.happiness + (uint)Math.Max(5, 40 - (int)animal.happinessDrain)));
            }
            animal.doEmote((int)animal.moodMessage == 4 ? 12 : 20, true);
            animal.happiness = (byte)Math.Min((int)byte.MaxValue, (int)animal.happiness + Math.Max(5, 40 - (int)animal.happinessDrain));
            if (animal.sound != null && Game1.soundBank != null)
            {
                Cue cue = Game1.soundBank.GetCue(animal.sound);
                string name = "Pitch";
                double num = (double)(1200 + Game1.random.Next(-200, 201));
                cue.SetVariable(name, (float)num);
                cue.Play();
            }
            farmer.gainExperience(0, 5);
            if (!animal.type.Equals("Sheep") || animal.friendshipTowardFarmer < 900)
                return;
            animal.daysToLay = (byte)2;
        }

        private void TimeEvents_TimeOfDayChanged(object sender, EventArgsIntChanged e)
        {
            foreach (FarmAnimal animal in Game1.getFarm().getAllFarmAnimals())
            {
                Monitor.Log($"[{animal.displayName}] has happiness [{animal.happiness.ToString()}]", LogLevel.Info);

            }
            return;

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
