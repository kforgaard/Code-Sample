using System;

/* This program is designed to randomly generate an NPC for D&D 5th edition and output the result to the console.
 * A Dungeon Master can use this program to quickly create a group of NPC's that are plausible, but randomly different from each other.
 * The NPC's are assumed to be bandits or something similar. They are all fairly ordinary races and non-magical classes.
 * The user can input a level and/or class if desired, or let the program choose randomly.
 * Having a customized group of enemies is fun because it adds to the game's strategy and immersion, is time-consuming to create on paper.
 * This program allows the group to be created instantly, the dungeon master just has to copy down the stats that are printed out.
 * 
 * This code assumes that characters are using generic equipment.
 * It also ignores most class features since they are not needed for NPC's and would clutter this code sample.
 */

namespace DnDCharacterGenerator
{
    //These are the three available classes. A typical assortment for a bandit group.
    enum CharacterClass { Barbarian, Fighter, Rogue };
    //These are the four available races. Each is typical for a D&D setting and comfortable in a martial class.
    enum Race { Human, HalfOrc, Dwarf, HalfElf };
    //These are the six attributes every D&D character has. Strength, Dexterity, Constitution, Intelligence, Wisdom, and Charisma.
    enum Attribute { Str, Dex, Con, Int, Wis, Cha };

    class Character
    {
        static Random rand = new Random(); //used for various random functions through the program

        
        const int LEVEL_MIN = 1;
        const int LEVEL_MAX = 20; //D&D characters are always between levels 1-20
        const int NUM_CLASS = 3; //number of character classes available. useful for iteration
        const int NUM_RACE = 4; //number of character races available. useful for iteration
        static readonly int[] CLASS_WEIGHTS = new int[NUM_CLASS] { 1, 1, 1 }; //relative probability of each class
        static readonly int[] RACE_WEIGHTS = new int[NUM_RACE] { 5, 2, 2, 1 }; //relative probability of each race
        //properly formatted names of races. Used when printing the character summary
        static readonly string[] RACE_NAMES = new string[NUM_RACE] { "Human", "Half-Orc", "Dwarf", "Half-Elf" };
        private int level; //this character's level 1-20
        private CharacterClass charClass; //this character's class
        private Race charRace; //this character's race

        const int NUM_ATTRIBUTES = 6; //used frequently for iterating over arrays of attributes
        const int BASE_ATTRIBUTE_VALUE = 8; //minimum value for any attribute
        const int MAX_BASE_ATTRIBUTE_VALUE = 18; //maximum value for any attribute before modifiers
        const int NUM_ATTRIBUTE_LEVEL_BREAKPOINTS = 5; //used when adding attribute points onto base values based on the character's level
        private int[] attributes = new int[NUM_ATTRIBUTES]; //contains this character's six primary attribute values
        private int[] attributeMods = new int[NUM_ATTRIBUTES]; //contains the attribute modifiers derived from the primary attribute values

        //miscellaneous character features - specific use of these is relevant to the user, but not relevant to execution of this program
        private int proficiency; //general purpose value that makes stronger characters more effective at various tasks
        private int ac; //the character's armor class
        private int maxHP; //the character's max health
        private int initiative; //the character's bonus when determining turn order for an encounter
        private int speed; //the character's move speed in feet per round
        private int perception; //the character's bonus to detecting another character in stealth
        private int[] savingThrows; //the character's resistances. one for each attribute

        //holds a character level and the attribute point bonus each class gets at that level
        struct AttrLevels
        {
            public int levelThreshold;
            public int[,] attrData;
        }

        //complex data array containing all the levels that give bonus attribute points and the number of points given per class
        //contains data only for attribute bonuses from levels. does not include base values, random bonuses, or racial bonuses
        static readonly AttrLevels[] allAttrLevelData = new AttrLevels[]
           {
                    //example: this AttrLevels says that at level 19, the 0-index class (barbarian) earns
                    //7 points in the 0-index attribute (strength) and 3 points in the 2-index attribute (constitution)
                    //these values can vary from character to character, but I have chosen reasonable template values.
                 new AttrLevels {levelThreshold = 19, attrData = new int[NUM_CLASS,NUM_ATTRIBUTES] { 
                     { 7, 0, 3, 0, 0, 0 }, 
                     { 5, 0, 5, 0, 0, 0 }, 
                     { 2, 6, 2, 0, 0, 0 } } },
                 new AttrLevels {levelThreshold = 16, attrData = new int[NUM_CLASS,NUM_ATTRIBUTES] { 
                     { 6, 0, 2, 0, 0, 0 }, 
                     { 4, 0, 4, 0, 0, 0 }, 
                     { 1, 5, 2, 0, 0, 0 } } },
                 new AttrLevels {levelThreshold = 12, attrData = new int[NUM_CLASS,NUM_ATTRIBUTES] { 
                     { 4, 0, 2, 0, 0, 0 }, 
                     { 3, 0, 3, 0, 0, 0 }, 
                     { 1, 4, 1, 0, 0, 0 } } },
                 new AttrLevels {levelThreshold = 8, attrData = new int[NUM_CLASS,NUM_ATTRIBUTES] { 
                     { 3, 0, 1, 0, 0, 0 }, 
                     { 2, 0, 2, 0, 0, 0 }, 
                     { 0, 4, 0, 0, 0, 0 } } },
                 new AttrLevels {levelThreshold = 4, attrData = new int[NUM_CLASS,NUM_ATTRIBUTES] { 
                     { 2, 0, 0, 0, 0, 0 }, 
                     { 2, 0, 0, 0, 0, 0 }, 
                     { 0, 2, 0, 0, 0, 0 } } },
           };

        //data array containing all the bonus attribute points based on chosen race
        static readonly int[,] attrRacialData = new int[NUM_RACE, NUM_ATTRIBUTES] {
            //example: the 0-index race (human) earns one bonus point in each attribute
            { 1, 1, 1, 1, 1, 1 }, 
            { 2, 0, 1, 0, 0, 0 }, 
            { 0, 0, 2, 0, 0, 0 }, 
            { 0, 1, 1, 0, 0, 2 } };

        //data array for determining for each class which attributes get to add their proficiency bonus to their saving throws
        static readonly bool[,] throwBonusData = new bool[NUM_CLASS, NUM_ATTRIBUTES] {
            //example: both the 0 and 1-index classes (barbarian and fighter) get to add their proficiency 
            //bonus to their 0 and 2-index saving throws (strength and constitution)
            { true, false, true, false, false, false }, 
            { true, false, true, false, false, false }, 
            { false, true, false, true, false, false} };

        //class has four constructors. the user can choose wether or not to specify a character level or class

        //if the user passes in a parameter it is simply assigned
        public Character( int _level, CharacterClass _charClass )
        {
            level = _level;
            charClass = _charClass;

            Init();
        }

        public Character( int _level )
        {
            level = _level;
            SetClass();

            Init();
        }

        public Character( CharacterClass _charClass )
        {
            SetLevel();
            charClass = _charClass;

            Init();
        }

        //if the user doesn't pass a paramter the missing value is determined randomly.
        public Character()
        {
            SetLevel();
            SetClass();

            Init();
        }

        //calls all the step-by-step character creation functions
        private void Init()
        {
            ValidateLevel(level);
            SetRace();
            SetAttributes();
            SetProficiency();
            SetAC();
            SetHP();
            SetInitiative();
            SetSpeed();
            SetPerception();
            SetSavingThrows();
        }

        //forces the character level to be within the acceptable range (1-20)
        private void ValidateLevel(int _level)
        {
            if (_level > LEVEL_MAX)
                level =  LEVEL_MAX;

            else if (_level < LEVEL_MIN)
                level =  LEVEL_MIN;
        }

        //randomly determines the character level if not given as a parameter by the user
        private void SetLevel()
        {
            level = rand.Next(LEVEL_MAX - LEVEL_MIN) + LEVEL_MIN;
        }

        //randomly determines the character class based on weight if not given as a parameter by the user
        private void SetClass()
        {
            charClass = (CharacterClass)WeightedRandom(CLASS_WEIGHTS);
        }

        //randomly determines the character race based on weight
        private void SetRace()
        {
            charRace = (Race)WeightedRandom(RACE_WEIGHTS);
        }

        //general purpose weighted randomization function
        //takes an array of possible results with their relative weight and outputs the array index of the chosen result
        private int WeightedRandom(int[] weights)
        {
            //adds up the total weight of all possibilities
            int total = 0;
            foreach (int num in weights)
            {
                total += num;
            }

            //generates a random int and iterates through the possibilities
            //each iteration subtracts the weight of the current possibility from the random int
            //returns chosen result when the random int reaches 0
            int randomChoice = rand.Next(total) + 1;
            for(int i = 0; i < weights.Length; i++)
            {
                randomChoice -= weights[i];
                if (randomChoice <= 0)
                    return i;
            }
            //returns 0 if something went wrong (parameter array was all 0's, etc.)
            return 0;
        }

        //calls all the functions to add attribute points from various sources
        private void SetAttributes()
        {
            //sets all attributes to a base value plus some random amount (within bounds, 8-18)
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                attributes[i] = BASE_ATTRIBUTE_VALUE + RandomIncrease(0.75, MAX_BASE_ATTRIBUTE_VALUE - BASE_ATTRIBUTE_VALUE);
            }
            AddAttrFromLevel(); //adds additional points based on the character's level and class
            AddAttrFromRace(); //adds additional points based on the character's race
            UpdateAttrMods(); //sets and attribute modifiers (derived from attribute points)
        }

        /*general purpose function that returns a random int with higher numbers being exponentially less likely
        * used to add variable attribute points
        * chance affects the likelyhood of each increase
        * returns an int between 0 and maxIncrease
        */
        private int RandomIncrease(double chance, int maxIncrease)
        {
            double initialChance = chance;
            int increase = 0;
            //each time through the loop increases the result but makes the next increase only chance% as likely as the last time through
            while (chance > rand.NextDouble() && increase < maxIncrease)
            {
                increase++;
                chance *= initialChance;
            }
            return increase;
        }

        //iterates through the attributes and adds the data value specific to the character's level and class
        private void AddAttrFromLevel()
        {
            //iterates through the different levels the character earns points at to find the highest acceptable one
            //example: if the character is level 13, they will not earn the level 19 or 16 points, but they will earn the level 12 points
            //if the character level is less than the lowest breakpoint they earn no bonus points and the method exits
            for(int i = 0; i < NUM_ATTRIBUTE_LEVEL_BREAKPOINTS; i++)
            {
                if(level >= allAttrLevelData[i].levelThreshold)
                {
                    for(int k = 0; k < NUM_ATTRIBUTES; k++)
                    {
                        attributes[k] += allAttrLevelData[i].attrData[(int)charClass, k];
                    }
                    return;
                }
            }
        }

        //adds the bonus attribute points given to the character's race
        //this is independent of class or level
        private void AddAttrFromRace()
        {
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                attributes[i] += attrRacialData[(int)charRace, i];
            }
        }

        //determines the character's attribute mods based on their final attribute scores
        //these values are important to the user and used elsewhere in the program
        private void UpdateAttrMods()
        {
            //according to the D&D player's handbook, the formula for calculating an attribute modifier is (attribute value) /2 - 5
            const int ATTRIBUTE_HALVING = 2;
            const int ATTRIBUTE_BASE_REDUCTION = -5;
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                attributeMods[i] = (attributes[i] / ATTRIBUTE_HALVING) + ATTRIBUTE_BASE_REDUCTION;
            }
        }

        //sets the character's proficiency value
        //based on a formula in the D&D player's handbook, characters start with 2 and get an increase every 4 levels
        private void SetProficiency()
        {
            const int BASE_PROFICIENCY = 2;
            const int PROFICIENCY_GAIN_RATE = 4;

            proficiency = ((level - 1) / PROFICIENCY_GAIN_RATE) + BASE_PROFICIENCY;
        }

        /*AC (armor class) represents the character's chance to block an incoming attack
         * this is increased by certain attribute modifiers and the character's chosen armor
         * this function calculate's the character's AC assuming that the character is wearing typical armor for their class
         */
        private void SetAC()
        {
            const int BASE_UNARMORED_AC = 10;
            const int BASE_HEAVY_AC = 15;
            const int BASE_LIGHT_AC = 12;
            const int MAX_HEAVY_DEX_MOD = 2;
            switch (charClass)
            {
                //unarmored barbarians get a base AC plus their dexterity and constitution modifiers
                case CharacterClass.Barbarian:
                    ac = BASE_UNARMORED_AC + attributeMods[(int)Attribute.Dex] + attributeMods[(int)Attribute.Con];
                    break;
                //heavily armored fighters get a base AC plus their dex modifier, but their bonus from this is capped
                case CharacterClass.Fighter:
                    int dexMod = Math.Min(attributeMods[(int)Attribute.Dex], MAX_HEAVY_DEX_MOD);
                    ac = BASE_HEAVY_AC + dexMod;
                    break;
                //lightly armored rogues get their base AC plus their dex modifier
                case CharacterClass.Rogue:
                    ac = BASE_LIGHT_AC + attributeMods[(int)Attribute.Dex];
                    break;
            }
        }

        //this function determines the character's maximum health
        //the formula for this is somewhat complex and relies on the character's class, level, and constitution modifier
        private void SetHP()
        {
            //different classes have different "hit dice" that are used for determining HP
            //barbarians use a D12, fighters use a D10, and rogues use a D8
            int[] hitDice = { 12, 10, 8 };
            //basically, the formula says at level 1 the character's HP is equal to their hit die
            //then every level after that the character earns additional hp equal to half their hit die rounded up
            maxHP = hitDice[(int)charClass] + (((hitDice[(int)charClass] / 2 + 1) + attributeMods[(int)Attribute.Con]) * (level - 1));
        }

        //determines the character's initiative which is derived simply from their dexterity modifier
        private void SetInitiative()
        {
            initiative = attributeMods[(int)Attribute.Dex];
        }

        //determines the character's walking speed. all races have 30 speed except dwarves who have 25
        private void SetSpeed()
        {
            const int NORMAL_SPEED = 30;
            const int DWARF_SPEED = 25;
            speed = (charRace == Race.Dwarf) ? DWARF_SPEED : NORMAL_SPEED;
        }

        //determines the character's perception bonus. this is equal to a base value plus their wisdom modifier plus proficiency bonus
        private void SetPerception()
        {
            const int BASE_PERCEPTION = 10;
            perception = BASE_PERCEPTION + attributeMods[(int)Attribute.Wis] + proficiency;
        }

        //determines the character's saving throw values
        private void SetSavingThrows()
        {
            savingThrows = new int[NUM_ATTRIBUTES];
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                //saving throws are based on attribute values
                savingThrows[i] = attributeMods[i];
                
                //each class has certain saving throws that also get to add their profciency bonus
                //this information is stored in a boolean data array at the top of the this file
                if(throwBonusData[(int)charClass, i])
                {
                    savingThrows[i] += proficiency;
                }
            }
        }

        //outputs the character's information to plain english
        //can be called with Console.WriteLine()
        public override String ToString()
        {
            string output = "";

            //displays the character's level, race, and class. uses the array of formatted race names declared earlier
            output += String.Format("Level {0} {1} {2}\n", level, RACE_NAMES[(int)charRace], charClass.ToString());
            //displays each attribute value with its modifier in parenthesis
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                output += String.Format("{0}: {1}({2})  ", (Attribute)i, attributes[i], attributeMods[i]);
            }
            //displays miscellaneous information; proficiency, AC, hitpoints, initiative, speed, perception
            output += String.Format("\nProficiency: {0}  AC: {1}  HP: {2}\n", proficiency, ac, maxHP);
            output += String.Format("Initiative: {0}  Speed: {1}  Perception: {2}\n", initiative, speed, perception);
            //displays each saving throw value
            output += "Saving throws, ";
            for(int i = 0; i < NUM_ATTRIBUTES; i++)
            {
                output += String.Format("{0}: {1}  ", (Attribute)i, savingThrows[i]);
            }

            return output;
        }
    }
}
