using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using MonoMod;
using UnityEngine;

namespace Valor
{
    // BepInPlugin is required to make BepInEx properly load your mod, this tells BepInEx the ID, Name and Version of your mod.
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Valor : BaseUnityPlugin
    {
        // Some constants holding the stuff we put in BepInPlugin, we just made these seperate variables so that we can more easily read them.
        public const string ModGUID = "karyoplasma.valor";
        public const string ModName = "Valor";
        public const string ModVersion = "0.1.1";

        // Create a ConfigEntry so we can reference our config option.
        private ConfigEntry<bool> ValorEnabled;
        private ConfigEntry<bool> ValorNewGamePlus;
        private ConfigEntry<int> ValorImprovedSwimming;
        private List<Monster> swimmingMonsters;
        private List<Monster> breakWallMonsters;
        private List<Monster> mountMonsters;
        private List<Monster> flyingMonsters;
        private List<Monster> improvedFlyingMonsters;
        private List<Monster> secretVisionMonsters;
        private List<Monster> grappleMonsters;
        private List<Monster> bigRockMonsters;
        private List<Monster> fireMonsters;
        private List<Monster> levitateMonsters;
        private List<Monster> lightMonsters;
        private List<Monster> crushMonsters;
        private List<Monster> blobMonsters;
        private List<Monster> spectralMonsters;
        private List<Monster> chosenMonsters;
        private bool[] exploreAbilities;
        private Monster[] monstersArray;
        // This is the first code that runs when your mod gets loaded.
        public Valor()
        {
            // Binding a config option, making it actually be registered by BepInEx.
            ValorEnabled = Config.Bind(
                "General", // The category in the config file.
                "Enabled", // The name of the option
                true, // The default value
                "Toggle between vanilla Bravery and Valor. Valor is true, Bravery is false." // The description
            );
            ValorNewGamePlus = Config.Bind(
                "Progression",
                "Ignore Spectral ability",
                false,
                "Ignore the Explore Ability of your Spectral starter when generating the seed. This is intended for New Game+."
            );
            ValorImprovedSwimming = Config.Bind(
                "Progression",
                "Guarantee Improved Swimming",
                0,
                "Guarantees the generation of an improved swimming monster. 0 = off, 1 = anywhere in the seed, 2 = guaranteed from Caretaker."
            );

            // To modify game functions you can use monomod.
            // This routes the OpenChest function into our function.
            On.GameModeManager.SetupGame += GameModeManager_SetupGame;
        }

        public void GameModeManager_SetupGame(On.GameModeManager.orig_SetupGame orig, GameModeManager self)
        {
            // If our config options is true run the code inside.
            if (ValorEnabled.Value)
            {
                orig(self);
                if (self.BraveryMode && !self.RandomizerMode)
                {
                    // initialize lists
                    swimmingMonsters = initializeSwimmingMonstersList();
                    breakWallMonsters = initializeBreakWallMonstersList();
                    mountMonsters = initializeMountMonstersList();
                    flyingMonsters = initializeFlyingMonstersList();
                    improvedFlyingMonsters = initializeImprovedFlyingMonstersList();
                    secretVisionMonsters = initializeSecretVisionMonstersList();
                    grappleMonsters = initializeGrappleMonstersList();
                    bigRockMonsters = initializeBigRockMonstersList();
                    fireMonsters = initializeFireMonstersList();
                    levitateMonsters = initializeLevitateMonstersList();
                    lightMonsters = initializeLightMonstersList();
                    crushMonsters = initializeCrushMonstersList();
                    blobMonsters = initializeBlobMonstersList();
                    spectralMonsters = initializeSpectralMonstersList();
                    // 0-breakwall, 1-flying, 2-mount, 3-improvedflying, 4-secretvision, 5-grapple, 6-bigrock
                    // 7-fire, 8-levitate, 9-light, 10-crush, 11-blob, 12-improvedswimming
                    exploreAbilities = new bool[13];
                    monstersArray = new Monster[13];
                    chosenMonsters = new List<Monster>();

                    Debug.Log("Rerandoming monsters for Valor Mode!");
                    self.BraveryMonsters.Clear();
                    self.SwimmingMonster = swimmingMonsters[UnityEngine.Random.Range((ValorImprovedSwimming.Value == 2) ? 1 : 0, swimmingMonsters.Count)];

                    Debug.Log("Swimming Monster: " + self.SwimmingMonster.name);
                    GetStartingMonsters(self);
                    GetRing0Monsters(self);
                    GetRing1Monsters(self);
                    GetRing2Monsters(self);
                    GetRing3Monsters(self);
                    GetRing4Monsters(self);
                    GetRing5Monsters(self);
                    GetKeepersArmyMonsters(self);
                    GetEndOfTimeMonsters(self);
                    GetTradeMonster(self);
                    for (int i = 0; i < 13; i++)
                    {
                        self.BraveryMonsters.Add(self.MonsterAreas[i], monstersArray[i]);
                    }
                }

            }
        }

        private void GetTradeMonster(GameModeManager self)
        {   
            // beta might break
            Monster obtainedMonster = chosenMonsters[UnityEngine.Random.Range(0, chosenMonsters.Count)];
            Monster randomMonster = getAnyNonSpectralMonster();
            self.CryomancerMonster = randomMonster;
            self.CryomancerRequiredMonster = obtainedMonster;
            Debug.Log("Lady Stasis will trade our " + obtainedMonster + " for " + randomMonster + ". This is untested and might break the playthrough if done too early.");
        }

        private void GetEndOfTimeMonsters(GameModeManager self)
        {
            // The monsters in Eternity's End. Anything goes here, even Spectrals!
            List<Monster> endgameMonsters = new List<Monster>();
            
            // Generate an Improved Swimmer if the user enabled it
            if (ValorImprovedSwimming.Value == 1 && !exploreAbilities[12])
            {
                Monster improvedSwimmer = swimmingMonsters[UnityEngine.Random.Range(1, swimmingMonsters.Count)];
                endgameMonsters.Add(improvedSwimmer);
                chosenMonsters.Add(improvedSwimmer);
            }

            // Fill the list to contain 3 monsters
            for (int i = endgameMonsters.Count; i < 3; i++)
            {
                Monster randomMonster = getAnyMonster();
                endgameMonsters.Add(randomMonster);
            }
            Debug.Log("Eternity's End Monsters:");
            self.EndOfTimeMonsters.Clear();
            for (int i = 0; i < 3; i++)
            {
                self.EndOfTimeMonsters.Add(endgameMonsters[i]);
                Debug.Log(endgameMonsters[i].name);
            }
        }

        private void GetKeepersArmyMonsters(GameModeManager self)
        {
            // The goal here is to fulfill all exploration ability needs to be able to beat all champions.
            List<Monster> ring6Monsters = new List<Monster>();

            if (!exploreAbilities[7])
            {
                Monster fireMonster = getValidMonsterFromList(fireMonsters, spectralMonsters);
                ring6Monsters.Add(fireMonster);
                Debug.Log("We need a fire monster: " + fireMonster.name);
            }
            if (!exploreAbilities[9])
            {
                Monster lightMonster = getValidMonsterFromList(lightMonsters, spectralMonsters);
                ring6Monsters.Add(lightMonster);
                Debug.Log("We need a light monster: " + lightMonster.name);
            }
            if (!exploreAbilities[10])
            {
                Monster crushMonster = getValidMonsterFromList(crushMonsters, spectralMonsters);
                ring6Monsters.Add(crushMonster);
                Debug.Log("We need a crush monster: " + crushMonster.name);
            }
            if (!exploreAbilities[6])
            {
                Monster bigRockMonster = getValidMonsterFromList(bigRockMonsters, spectralMonsters);
                ring6Monsters.Add(bigRockMonster);
                Debug.Log("We need a Big Rock monster: " + bigRockMonster.name);
            }
            if (!exploreAbilities[8])
            {
                Monster levitateMonster = getValidMonsterFromList(levitateMonsters, spectralMonsters);
                ring6Monsters.Add(levitateMonster);
                Debug.Log("We need a levitate monster: " + levitateMonster.name);
            }
            if (!exploreAbilities[11])
            {
                Monster blobMonster = getValidMonsterFromList(blobMonsters, spectralMonsters);
                ring6Monsters.Add(blobMonster);
                Debug.Log("We need a blob form monster: " + blobMonster.name);
            }

            // We have everything, fill the list and set it to the army keeper.
            for (int i = ring6Monsters.Count; i < 7; i++)
            {
                Monster randomMonster = getAnyNonSpectralMonster();
                ring6Monsters.Add(randomMonster);
            }
            Debug.Log("Keeper Army Monsters:");
            if (ring6Monsters.Count > 7)
            {
                Debug.LogWarning("We generated too many Keeper's army monsters. Should never happen.");
            }

            self.MonsterArmyMonsters.Clear();
            for (int i = 0; i < 7; i++)
            {
                Monster addMonster = ring6Monsters[UnityEngine.Random.Range(0, ring6Monsters.Count)];
                self.MonsterArmyMonsters.Add(addMonster);
                Debug.Log(addMonster.name);
                ring6Monsters.Remove(addMonster);
            }
        }

        private void GetRing5Monsters(GameModeManager self)
        {
            // Ring 5 is the DLC and Blob Burg. No spectrals, but else anything goes. No requirements.
            List<int> ring5Areas = new List<int>() { 11, 12 };
            List<Monster> ring5Monsters = new List<Monster>();

            for (int i = 0; i < 2; i++)
            {
                ring5Monsters.Add(getAnyNonSpectralMonster());
            }

            distributeMonstersToAreas(ring5Monsters, ring5Areas);

            //debug
            logExplorationAbilityArray();
        }

        private void GetRing4Monsters(GameModeManager self)
        {
            // Ring 4 core is the Abandoned Tower and the Bex monster. Mystical Workshop might be in this ring as well, if we needed an Improved Flyer until last
            // ring. Once again, only Spectrals are banned, so we don't need a ban list.
            List<int> ring4Areas = new List<int>() { 10 };
            List<Monster> ring4Monsters = new List<Monster>();

            // check whether we have been to Mystical Workshop, if not, it's in this ring.
            if (monstersArray[8] == null)
            {
                ring4Areas.Add(8);
            }

            // We need a monster with Secret Vision for the DLC.
            if (!exploreAbilities[4])
            {
                Monster secretVisionMonster = getValidMonsterFromList(secretVisionMonsters, spectralMonsters);
                ring4Monsters.Add(secretVisionMonster);
                Debug.Log("We need Secret Vision: " + secretVisionMonster.name);
            }
            // fill the rest with random non-spectrals and generate an extra one for Bex
            for (int i = ring4Monsters.Count; i < ring4Areas.Count + 1; i++)
            {
                ring4Monsters.Add(getAnyNonSpectralMonster());
            }
            // set the Bex monster and remove it from the list
            Monster bexMonster = ring4Monsters[UnityEngine.Random.Range(0, ring4Monsters.Count)];
            self.BexMonster = bexMonster;
            Debug.Log("Bex will give us: " + bexMonster.name);
            ring4Monsters.Remove(bexMonster);
            distributeMonstersToAreas(ring4Monsters, ring4Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing3Monsters(GameModeManager self)
        {
            // Ring 3 core is Horizon Beach and Underworld. There is a high chance we can access Magma Chamber before that, if not, it's in this ring.
            // Mystical Workshop might be accessible too if we randomed an Improved Flyer.
            // Banned are only Spectrals, so we don't need a specific ban list anymore
            List<int> ring3Areas = new List<int>() { 6, 9 };
            List<Monster> ring3Monsters = new List<Monster>();
            
            // check for improved flying
            if (exploreAbilities[3])
            {
                // if we haven't already been to Mystical Workshop, we can access it now.
                if (monstersArray[8] == null)
                {
                    ring3Areas.Add(8);
                }
            }
            // check whether we have been to Magma Chamber, if not, we have a mount at this point.
            if (monstersArray[7] == null)
            {
                ring3Areas.Add(7);
            }

            // we need Grapple and Improved Flying
            if (!exploreAbilities[5])
            {
                Monster grappleMonster = getValidMonsterFromList(grappleMonsters, spectralMonsters);
                ring3Monsters.Add(grappleMonster);
                Debug.Log("We need Grapple: " + grappleMonster.name);
            }
            if (!exploreAbilities[3])
            {
                Monster improvedFlyingMonster = getValidMonsterFromList(improvedFlyingMonsters, spectralMonsters);
                ring3Monsters.Add(improvedFlyingMonster);
                Debug.Log("We need Improved Flying: " + improvedFlyingMonster.name);
            }
            // fill the rest with random non-spectrals that are not banned
            for (int i = ring3Monsters.Count; i < ring3Areas.Count; i++)
            {
                Monster monster = getAnyNonSpectralMonster();
                ring3Monsters.Add(monster);
            }

            distributeMonstersToAreas(ring3Monsters, ring3Areas);

            // debug stuff
            logExplorationAbilityArray();

        }

        private void GetRing2Monsters(GameModeManager self)
        {
            // Ring 2 core is only Sun Palace. The rest depends whether we got a mount or a flying monster in the previous ring.
            // Banned are Swimming and Spectral monsters.
            List<int> ring2Areas = new List<int>() { 5 };
            List<Monster> ring2Monsters = new List<Monster>();
            List<Monster> banList = swimmingMonsters.Union(spectralMonsters).ToList();
            // if we got a mount we have to check whether we got it before last ring or not
            if (exploreAbilities[2])
            {
                if (monstersArray[4] == null)
                {
                    ring2Areas.Add(4);
                }
                if (monstersArray[7] == null)
                {
                    ring2Areas.Add(7);
                }
            }
            // we have at least flying at this point, but if we have the improved version, we can dip into Mystical Workshop!
            if (exploreAbilities[3])
            {
                ring2Areas.Add(8);
            }
            // if Snowy Peaks was not set yet and it was not visited before, it's in this ring
            if (!ring2Areas.Contains(4) && monstersArray[4] == null)
            {
                ring2Areas.Add(4);
            }

            // the only requirement for this ring is a mount, else we are free.
            if (!exploreAbilities[2])
            {
                Monster mountMonster = getValidMonsterFromList(mountMonsters, banList);
                ring2Monsters.Add(mountMonster);
                Debug.Log("We need a mount: " + mountMonster.name);
            }

            // fill the rest with random non-spectrals that are not banned
            for (int i = ring2Monsters.Count; i < ring2Areas.Count; i++)
            {
                Monster monster = getValidMonsterNoSpectral(banList);
                ring2Monsters.Add(monster);
            }

            distributeMonstersToAreas(ring2Monsters, ring2Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing1Monsters(GameModeManager self)
        {
            // Ring 1 is complicated. The core is Stronghold Dungeon and Ancient Woods, however we might be able to access
            // Snowy Peaks and Magma Chamber. In this case we have to add them later on. Only swimming monsters and spectrals
            // are banned here.
            List<int> ring1Areas = new List<int>() { 2, 3 };
            List<Monster> ring1Monsters = new List<Monster>();
            List<Monster> banList = swimmingMonsters.Union(spectralMonsters).ToList();

            // check for area availability
            if (exploreAbilities[1])
            {
                // we have flying, so we can go to Snowy Peaks
                ring1Areas.Add(4);
                if (exploreAbilities[2])
                {
                    // we also have a mount so we can access Magma Chamber
                    ring1Areas.Add(7);
                }
            }
            else
            {
                // we do not have flying
                if (exploreAbilities[2])
                {
                    // mount alone allows access to both Peak and Chamber
                    ring1Areas.Add(4);
                    ring1Areas.Add(7);
                }
            }

            // requirements are that we get a flying or a mount monster so we are not stuck
            if (!(exploreAbilities[1] || exploreAbilities[2]))
            {
                Monster mountOrFlying = getValidMonsterFromList(mountMonsters.Union(flyingMonsters).Union(improvedFlyingMonsters).ToList(), banList);
                ring1Monsters.Add(mountOrFlying);
                Debug.Log("Need a flying or mount monster: " + mountOrFlying.name);
            }

            // fill the rest with random non-spectrals that are not banned
            for (int i = ring1Monsters.Count; i < ring1Areas.Count; i++)
            {
                Monster monster = getValidMonsterNoSpectral(banList);
                ring1Monsters.Add(monster);
            }

            distributeMonstersToAreas(ring1Monsters, ring1Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing0Monsters(GameModeManager self)
        {
            // ring 0 is always Mountain Path and Blue Cave. No Spectrals, Swimming or Improved Flying allowed.
            List<Monster> ring0Monsters = new List<Monster>();
            List<int> ring0Areas = new List<int>() { 0, 1 };
            List<Monster> banList = improvedFlyingMonsters.Union(swimmingMonsters).Union(spectralMonsters).ToList();

            // requirements is only to have a monster that can break walls
            if (!exploreAbilities[0])
            {
                Monster breakWallMonster = getValidMonsterFromList(breakWallMonsters, banList);
                ring0Monsters.Add(breakWallMonster);
                Debug.Log("Need a BreakWall monster: " + breakWallMonster.name);
            }

            // fill the rest with random non-spectrals that are not banned
            for (int i = ring0Monsters.Count; i < ring0Areas.Count; i++)
            {
                Monster monster = getValidMonsterNoSpectral(banList);
                ring0Monsters.Add(monster);
            }

            distributeMonstersToAreas(ring0Monsters, ring0Areas);

            // debug stuff
            logExplorationAbilityArray();

        }

        private void GetStartingMonsters(GameModeManager self)
        {
            Debug.Log("Getting new Starting Monsters");

            // no Swimming or Improved Flying monsters at the start
            List<Monster> banList = improvedFlyingMonsters.Union(swimmingMonsters).ToList();

            // remove old monsters and get new ones
            PlayerController.Instance.Monsters.Clear();
            self.FamiliarIndex = UnityEngine.Random.Range(0, 4);
            PlayerController.Instance.Monsters.AddMonsterByPrefab(GameController.Instance.MonsterJournalList[self.FamiliarIndex], EShift.Normal, false, null, false, false);
            chosenMonsters.Add(GameController.Instance.MonsterJournalList[self.FamiliarIndex].GetComponent<Monster>());
            // Ignore progression if NewGame+ compatibility option is set
            if (!ValorNewGamePlus.Value)
            {
                updateExploreAbilities(GameController.Instance.MonsterJournalList[self.FamiliarIndex].GetComponent<Monster>());
            }
            // set follower to your familiar else it sometimes bugs out and doesn't let you name your starters
            PlayerController.Instance.Follower.Monster = PlayerController.Instance.Monsters.Familiar;
            // random 2 different, non-banned monsters
            for (int i = 0; i < 2; i++)
            {
                GameObject gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
                while (banList.Contains(gameObject.GetComponent<Monster>()))
                {
                    gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
                };
                PlayerController.Instance.Monsters.AddMonsterByPrefab(gameObject, EShift.Normal, false, null, false, false);
                chosenMonsters.Add(gameObject.GetComponent<Monster>());
                updateExploreAbilities(gameObject.GetComponent<Monster>());
            }

            // debug section
            Debug.Log("Starting Monsters: ");
            foreach (Monster m in chosenMonsters)
            {
                Debug.Log(m.name);
            }
            logExplorationAbilityArray();
        }

        // helper methods
        private void distributeMonstersToAreas(List<Monster> monsters, List<int> areas)
        {
            if (monsters.Count != areas.Count)
            {
                Debug.LogWarning("The area and monster lists are not of equal size!");
            }

            foreach (int area in areas)
            {
                Monster addMonster = monsters[UnityEngine.Random.Range(0, monsters.Count)];
                addMonsterToArea(addMonster, area);
                monsters.Remove(addMonster);
            }
        }

        private Monster getValidMonsterFromList(List<Monster> monsterList, List<Monster> banList)
        {
            Monster monster = monsterList[UnityEngine.Random.Range(0, monsterList.Count)];
            while (banList.Contains(monster) || chosenMonsters.Contains(monster))
            {
                monster = monsterList[UnityEngine.Random.Range(0, monsterList.Count)];
            }
            chosenMonsters.Add(monster);

            return monster;
        }

        private Monster getValidMonsterNoSpectral(List<Monster> banList)
        {
            GameObject gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
            while (banList.Contains(gameObject.GetComponent<Monster>()) || chosenMonsters.Contains(gameObject.GetComponent<Monster>()))
            {
                gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
            };
            chosenMonsters.Add(gameObject.GetComponent<Monster>());

            return gameObject.GetComponent<Monster>() ;
        }

        private Monster getAnyNonSpectralMonster()
        {
            GameObject gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
            while (chosenMonsters.Contains(gameObject.GetComponent<Monster>()))
            {
                gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(4, GameController.Instance.MonsterJournalList.Count - 1)];
            };
            chosenMonsters.Add(gameObject.GetComponent<Monster>());

            return gameObject.GetComponent<Monster>();
        }

        private Monster getAnyMonster()
        {
            GameObject gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(0, GameController.Instance.MonsterJournalList.Count - 1)];
            while (chosenMonsters.Contains(gameObject.GetComponent<Monster>()))
            {
                gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range(0, GameController.Instance.MonsterJournalList.Count - 1)];
            };
            chosenMonsters.Add(gameObject.GetComponent<Monster>());

            return gameObject.GetComponent<Monster>();
        }

        private Monster getMonsterByIndex(int index)
        {
            return GameController.Instance.MonsterJournalList[index].GetComponent<Monster>();
        }

        private void updateExploreAbilities(Monster monster)
        {
            // 0-breakwall, 1-flying, 2-mount, 3-improvedflying, 4-secretvision, 5-grapple, 6-bigrock
            // 7-fire, 8-levitate, 9-light, 10-crush, 11-blob
            if (breakWallMonsters.Contains(monster))
            {
                exploreAbilities[0] = true;
            }
            if (flyingMonsters.Contains(monster))
            {
                exploreAbilities[1] = true;
            }
            if (mountMonsters.Contains(monster))
            {
                exploreAbilities[2] = true;
            }
            if (improvedFlyingMonsters.Contains(monster))
            {
                exploreAbilities[3] = true;
            }
            if (secretVisionMonsters.Contains(monster))
            {
                exploreAbilities[4] = true;
            }
            if (grappleMonsters.Contains(monster))
            {
                exploreAbilities[5] = true;
            }
            if (bigRockMonsters.Contains(monster))
            {
                exploreAbilities[6] = true;
            }
            if (fireMonsters.Contains(monster))
            {
                exploreAbilities[7] = true;
            }
            if (levitateMonsters.Contains(monster))
            {
                exploreAbilities[8] = true;
            }
            if (lightMonsters.Contains(monster))
            {
                exploreAbilities[9] = true;
            }
            if (crushMonsters.Contains(monster))
            {
                exploreAbilities[10] = true;
            }
            if (blobMonsters.Contains(monster))
            {
                exploreAbilities[11] = true;
            }
        }

        private void addMonsterToArea(Monster monster, int index)
        {
            if (monstersArray[index] != null)
            {
                Debug.LogWarning("Overwriting monster in area " + getAreaByIndex(index));
            }
            else
            {
                Debug.Log("Adding " + monster.name + " to " + getAreaByIndex(index));
            }
          
            monstersArray[index] = monster;
            updateExploreAbilities(monster);
        }

        private String getAreaByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "Mountain Path!";
                case 1:
                    return "Blue Cave!";
                case 2:
                    return "Stronghold Dungeon!";
                case 3:
                    return "Ancient Woods!";
                case 4:
                    return "Snowy Peaks!";
                case 5:
                    return "Sun Palace!";
                case 6:
                    return "Horizon Beach!";
                case 7:
                    return "Magma Chamber!";
                case 8:
                    return "Mystical Workshop!";
                case 9:
                    return "Underworld!";
                case 10:
                    return "Abandoned Tower!";
                case 11:
                    return "Blob Burg!";
                case 12:
                    return "Forgotten World!";
                default:
                    return "Area not found!";
            }
        }

        // initialize list methods
        private List<Monster> initializeSpectralMonstersList()
        {
            List<Monster> spectralMonsters = new List<Monster>();

            spectralMonsters.Add(getMonsterByIndex(0));
            spectralMonsters.Add(getMonsterByIndex(1));
            spectralMonsters.Add(getMonsterByIndex(2));
            spectralMonsters.Add(getMonsterByIndex(3));

            return spectralMonsters;
        }

        private List<Monster> initializeSwimmingMonstersList()
        {
            List<Monster> swimmingMonsters = new List<Monster>();

            swimmingMonsters.Add(getMonsterByIndex(49));
            swimmingMonsters.Add(getMonsterByIndex(56));
            swimmingMonsters.Add(getMonsterByIndex(57));
            swimmingMonsters.Add(getMonsterByIndex(59));
            swimmingMonsters.Add(getMonsterByIndex(105));
            swimmingMonsters.Add(getMonsterByIndex(108));

            return swimmingMonsters;
        }

        private List<Monster> initializeBreakWallMonstersList()
        {
            List<Monster> breakWallMonsters = new List<Monster>();

            breakWallMonsters.Add(getMonsterByIndex(0));
            breakWallMonsters.Add(getMonsterByIndex(1));
            breakWallMonsters.Add(getMonsterByIndex(3));
            breakWallMonsters.Add(getMonsterByIndex(8));
            breakWallMonsters.Add(getMonsterByIndex(9));
            breakWallMonsters.Add(getMonsterByIndex(10));
            breakWallMonsters.Add(getMonsterByIndex(11));
            breakWallMonsters.Add(getMonsterByIndex(16));
            breakWallMonsters.Add(getMonsterByIndex(26));
            breakWallMonsters.Add(getMonsterByIndex(28));
            breakWallMonsters.Add(getMonsterByIndex(40));
            breakWallMonsters.Add(getMonsterByIndex(43));
            breakWallMonsters.Add(getMonsterByIndex(54));
            breakWallMonsters.Add(getMonsterByIndex(55));
            breakWallMonsters.Add(getMonsterByIndex(61));
            breakWallMonsters.Add(getMonsterByIndex(62));
            breakWallMonsters.Add(getMonsterByIndex(67));
            breakWallMonsters.Add(getMonsterByIndex(74));
            breakWallMonsters.Add(getMonsterByIndex(76));
            breakWallMonsters.Add(getMonsterByIndex(78));
            breakWallMonsters.Add(getMonsterByIndex(89));
            breakWallMonsters.Add(getMonsterByIndex(103));
            breakWallMonsters.Add(getMonsterByIndex(104));

            return breakWallMonsters;
        }

        private List<Monster> initializeFlyingMonstersList()
        {
            List<Monster> flyingMonsters = new List<Monster>();

            flyingMonsters.Add(getMonsterByIndex(2));
            flyingMonsters.Add(getMonsterByIndex(7));
            flyingMonsters.Add(getMonsterByIndex(15));
            flyingMonsters.Add(getMonsterByIndex(20));
            flyingMonsters.Add(getMonsterByIndex(32));
            flyingMonsters.Add(getMonsterByIndex(65));

            return flyingMonsters;
        }

        private List<Monster> initializeMountMonstersList()
        {
            List<Monster> mountMonsters = new List<Monster>();

            mountMonsters.Add(getMonsterByIndex(35));
            mountMonsters.Add(getMonsterByIndex(39));
            mountMonsters.Add(getMonsterByIndex(47));
            mountMonsters.Add(getMonsterByIndex(52));
            mountMonsters.Add(getMonsterByIndex(83));
            mountMonsters.Add(getMonsterByIndex(98));
            mountMonsters.Add(getMonsterByIndex(103));
            mountMonsters.Add(getMonsterByIndex(104));
            mountMonsters.Add(getMonsterByIndex(106));

            return mountMonsters;
        }

        private List<Monster> initializeImprovedFlyingMonstersList()
        {
            List<Monster> improvedFlyingMonsters = new List<Monster>();

            improvedFlyingMonsters.Add(getMonsterByIndex(53));
            improvedFlyingMonsters.Add(getMonsterByIndex(58));
            improvedFlyingMonsters.Add(getMonsterByIndex(66));
            improvedFlyingMonsters.Add(getMonsterByIndex(70));
            improvedFlyingMonsters.Add(getMonsterByIndex(77));
            improvedFlyingMonsters.Add(getMonsterByIndex(85));
            improvedFlyingMonsters.Add(getMonsterByIndex(98));
            improvedFlyingMonsters.Add(getMonsterByIndex(105));

            return improvedFlyingMonsters;
        }

        private List<Monster> initializeSecretVisionMonstersList()
        {
            List<Monster> secretVisionMonsters = new List<Monster>();

            secretVisionMonsters.Add(getMonsterByIndex(88));
            secretVisionMonsters.Add(getMonsterByIndex(90));
            secretVisionMonsters.Add(getMonsterByIndex(96));
            secretVisionMonsters.Add(getMonsterByIndex(100));
            secretVisionMonsters.Add(getMonsterByIndex(101));

            return secretVisionMonsters;
        }

        private List<Monster> initializeGrappleMonstersList()
        {
            List<Monster> grappleMonsters = new List<Monster>();

            grappleMonsters.Add(getMonsterByIndex(72));
            grappleMonsters.Add(getMonsterByIndex(81));
            grappleMonsters.Add(getMonsterByIndex(82));
            grappleMonsters.Add(getMonsterByIndex(94));

            return grappleMonsters;
        }

        private List<Monster> initializeBigRockMonstersList()
        {
            List<Monster> bigRockMonsters = new List<Monster>();

            bigRockMonsters.Add(getMonsterByIndex(79));
            bigRockMonsters.Add(getMonsterByIndex(80));
            bigRockMonsters.Add(getMonsterByIndex(84));

            return bigRockMonsters;
        }

        private List<Monster> initializeBlobMonstersList()
        {
            List<Monster> blobMonsters = new List<Monster>();

            blobMonsters.Add(getMonsterByIndex(91));
            blobMonsters.Add(getMonsterByIndex(92));
            blobMonsters.Add(getMonsterByIndex(93));

            return blobMonsters;
        }

        private List<Monster> initializeFireMonstersList()
        {
            List<Monster> fireMonsters = new List<Monster>();

            fireMonsters.Add(getMonsterByIndex(5));
            fireMonsters.Add(getMonsterByIndex(13));
            fireMonsters.Add(getMonsterByIndex(17));
            fireMonsters.Add(getMonsterByIndex(25));
            fireMonsters.Add(getMonsterByIndex(29));
            fireMonsters.Add(getMonsterByIndex(46));
            fireMonsters.Add(getMonsterByIndex(63));
            fireMonsters.Add(getMonsterByIndex(68));
            fireMonsters.Add(getMonsterByIndex(71));
            fireMonsters.Add(getMonsterByIndex(73));
            fireMonsters.Add(getMonsterByIndex(87));

            return fireMonsters;
        }
        private List<Monster> initializeLightMonstersList()
        {
            List<Monster> lightMonsters = new List<Monster>();

            lightMonsters.Add(getMonsterByIndex(21));
            lightMonsters.Add(getMonsterByIndex(27));
            lightMonsters.Add(getMonsterByIndex(34));
            lightMonsters.Add(getMonsterByIndex(39));
            lightMonsters.Add(getMonsterByIndex(60));
            lightMonsters.Add(getMonsterByIndex(61));
            lightMonsters.Add(getMonsterByIndex(64));


            return lightMonsters;
        }

        private List<Monster> initializeCrushMonstersList()
        {
            List<Monster> crushMonsters = new List<Monster>();

            crushMonsters.Add(getMonsterByIndex(61));
            crushMonsters.Add(getMonsterByIndex(62));
            crushMonsters.Add(getMonsterByIndex(67));
            crushMonsters.Add(getMonsterByIndex(74));
            crushMonsters.Add(getMonsterByIndex(89));
            crushMonsters.Add(getMonsterByIndex(103));
            crushMonsters.Add(getMonsterByIndex(104));

            return crushMonsters;
        }

        private List<Monster> initializeLevitateMonstersList()
        {
            List<Monster> levitateMonsters = new List<Monster>();

            levitateMonsters.Add(getMonsterByIndex(95));
            levitateMonsters.Add(getMonsterByIndex(97));
            levitateMonsters.Add(getMonsterByIndex(99));
            levitateMonsters.Add(getMonsterByIndex(109));

            return levitateMonsters;
        }

        // debug methods
        private void logExplorationAbilityArray()
        {
            string formattedArray = string.Join(", ", exploreAbilities);

            // Log the formatted array
            Debug.Log("Exploration abilities available: [" + formattedArray + "]");
        }
    }
}



