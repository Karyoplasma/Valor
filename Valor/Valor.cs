﻿using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public const string ModVersion = "0.3.3";

        // Create a ConfigEntry so we can reference our config option.
        private ConfigEntry<bool> ValorEnabled;
        private ConfigEntry<bool> ValorNewGamePlus;
        private ConfigEntry<int> ValorImprovedSwimming;
        private ConfigEntry<bool> ValorAllowSpectrals;
        private ConfigEntry<bool> ValorAllowBard;
        private ConfigEntry<bool> ValorAllowDuplicates;
        private ConfigEntry<int> ValorStartingDifficulty;
        private ConfigEntry<bool> ValorGenerateLog;
        private ConfigEntry<int> ValorChooseFamiliar;
        private ConfigEntry<bool> ValorDisableZoneRestrictions;
        private List<Monster> allMonsters;
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
        private List<Monster> possibleTrades;
        private bool[] exploreAbilities;
        private Monster[] monstersArray;
        private StringBuilder logBuilder;
        // This is the first code that runs when your mod gets loaded.
        public Valor()
        {
            // Binding a config option, making it actually be registered by BepInEx.
            ValorEnabled = Config.Bind(
                "General", // The category in the config file.
                "Enabled", // The name of the option
                true, // The default value
                "Toggle between vanilla Bravery and Valor.\nValor is true, Bravery is false." // The description
            );
            ValorNewGamePlus = Config.Bind(
                "Progression",
                "IgnoreFamilarAbility",
                false,
                "Ignore the Explore Ability of your Spectral familiar when generating the seed. This is intended for New Game+.\nLegacy setting. Probably unnecessary, but it generates different monsters, so I left it in"
            );
            ValorImprovedSwimming = Config.Bind(
                "Progression",
                "GuaranteeImprovedSwimming",
                0,
                "Guarantees the generation of an improved swimming monster.\n0 = off, 1 = anywhere in the seed, 2 = guaranteed from Caretaker."
            );
            ValorAllowBard = Config.Bind(
                "Bans",
                "AllowBard",
                false,
                "Allows Bard to be chosen as a random monster.\nReceiving one before the Forgotten World DLC will break the quest there."
            );
            ValorAllowSpectrals = Config.Bind(
                "Bans",
                "AllowSpectrals",
                false,
                "Allows Spectrals to be chosen as random monsters."
            );
            ValorAllowDuplicates = Config.Bind(
                "Bans",
                "AllowDuplicates",
                false,
                "Allows duplicate monsters."
            );
            ValorStartingDifficulty = Config.Bind(
                "Extras",
                "StartingDifficulty",
                2,
                "Set the ingame difficulty at the start.\n0 = Easy, 1 = Normal, 2 = Master."
            );
            ValorGenerateLog = Config.Bind(
                "Extras",
                "GenerateLog",
                true,
                "Generate a log detailing the intended progression and monsters found in the seed."
            );
            ValorChooseFamiliar = Config.Bind(
                "Extras",
                "ChooseFamiliar",
                -1,
                "Set to select your Familiar.\n0 = Wolf, 1 = Toad, 2 = Eagle, 3 = Lion\nanything else = random"
            );
            ValorDisableZoneRestrictions = Config.Bind(
                "Bans",
                "DisableZoneRestrictions",
                false,
                "If set to true, the logic will ignore all zone-wide monster bans like Swimmers or Improved Flyers in the starting zones.\nNote: Unbans Spectrals and Bard as well since they are essentially zone restrictions. Duplicates are still banned unless toggled."
            );
            // To modify game functions you can use monomod.
            // This routes the SetupGame and Open method into our methods.
            On.GameModeManager.SetupGame += GameModeManager_SetupGame;
            On.NewGameMenu.Open += NewGameMenu_Open;
        }

        public void NewGameMenu_Open(On.NewGameMenu.orig_Open orig, NewGameMenu self, int index, bool resetSettings)
        {
            if (ValorEnabled.Value)
            {
                self.RandomizerItem.SetDisabled(true);
                self.BraveryModeDescription = "- Valor functions the same way as Bravery, but the monster chests contain monsters from a wider pool.";
            }           
            orig(self, index, resetSettings);
        }

        public void GameModeManager_SetupGame(On.GameModeManager.orig_SetupGame orig, GameModeManager self)
        {
            orig(self);
            // If our config options is true run the code inside.
            if (ValorEnabled.Value)
            {
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
                    allMonsters = initializeAllMonstersList();
                    // 0-breakwall, 1-flying, 2-mount, 3-improvedflying, 4-secretvision, 5-grapple, 6-bigrock
                    // 7-fire, 8-levitate, 9-light, 10-crush, 11-blob, 12-improvedswimming
                    exploreAbilities = new bool[13];
                    monstersArray = new Monster[13];
                    chosenMonsters = new List<Monster>();
                    possibleTrades = new List<Monster>();
                    logBuilder = new StringBuilder();
                    buildLogStartingConfiguration(self.Seed);

                    Debug.Log("Rerandoming monsters for Valor Mode! Pool is " + allMonsters.Count + " monsters." );
                    self.BraveryMonsters.Clear();
                    self.SwimmingMonster = swimmingMonsters[UnityEngine.Random.Range((ValorImprovedSwimming.Value == 2) ? 1 : 0, swimmingMonsters.Count)];
                    chosenMonsters.Add(self.SwimmingMonster);
                    
                    Debug.Log("Swimming Monster: " + self.SwimmingMonster.name);
                    GetStartingMonsters(self);
                    GetRing0Monsters();
                    GetRing1Monsters();
                    GetRing2Monsters(self);
                    GetRing3Monsters(self);
                    GetRing4Monsters();
                    GetRing5Monsters();
                    GetKeepersArmyMonsters(self);
                    GetEndOfTimeMonsters(self);
                    GetTradeMonster(self);
                    BuildLogStarters();
                    for (int i = 0; i < 13; i++)
                    {
                        self.BraveryMonsters.Add(self.MonsterAreas[i], monstersArray[i]);
                        logBuilder.AppendLine(string.Format("{0}: {1}", getAreaByIndex(i), monstersArray[i].name));
                    }
                    BuildLogExtraMonsters(self);
                    Debug.Log("Pool is " + allMonsters.Count + " monsters.");
                    // set difficulty
                    PlayerController.Instance.Difficulty = GetStartingDifficulty();
                    // print log to file
                    if (ValorGenerateLog.Value)
                    {
                        saveLogToFile(Path.Combine(Paths.BepInExRootPath, "ValorLogs", string.Format("Seed {0}.txt", self.Seed)));
                    }
                }
            } 
        }

        private void GetTradeMonster(GameModeManager self)
        {
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            // only basic logic implemented, be careful
            if (possibleTrades.Count == 0)
            {
                Debug.LogError("The list of possible trades is empty. This should never happen.");
            } else
            {
                Debug.Log("Possible Trades:");
                foreach (Monster monster in possibleTrades)
                {
                    Debug.Log(monster.name);
                }
            }
            Monster obtainedMonster = possibleTrades[UnityEngine.Random.Range(0, possibleTrades.Count)];
            Monster randomMonster = getMonsterFromPool(allMonsters, activeBans);
            self.CryomancerMonster = randomMonster;
            self.CryomancerRequiredMonster = obtainedMonster;
            Debug.Log("Lady Stasis will trade our " + obtainedMonster + " for " + randomMonster + ". This is untested and might break the playthrough if done too early.");           
        }

        private void GetEndOfTimeMonsters(GameModeManager self)
        {
            // The monsters in Eternity's End. Anything goes here, even Spectrals!
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<Monster> endgameMonsters = new List<Monster>();

            // Generate an Improved Swimmer if the user enabled it
            if (ValorImprovedSwimming.Value == 1 && !exploreAbilities[12])
            {
                Monster improvedSwimmer = swimmingMonsters[UnityEngine.Random.Range(1, swimmingMonsters.Count)];
                endgameMonsters.Add(improvedSwimmer);
                updateExploreAbilities(improvedSwimmer);
                chosenMonsters.Add(improvedSwimmer);
                buildLogForcedProgression("Improved Swimming", improvedSwimmer);
            }

            // Fill the list to contain 3 monsters
            fillListWithMonsters(activeBans, endgameMonsters, 3);
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
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<Monster> ring6Monsters = new List<Monster>();

            if (!exploreAbilities[7])
            {
                Monster fireMonster = getMonsterFromPool(fireMonsters, activeBans);
                ring6Monsters.Add(fireMonster);
                Debug.Log("We need a fire monster: " + fireMonster.name);
                buildLogForcedProgression("Fire", fireMonster);
            }
            if (!exploreAbilities[9])
            {
                Monster lightMonster = getMonsterFromPool(lightMonsters, activeBans);
                ring6Monsters.Add(lightMonster);
                Debug.Log("We need a light monster: " + lightMonster.name);
                buildLogForcedProgression("Light", lightMonster);
            }
            if (!exploreAbilities[10])
            {
                Monster crushMonster = getMonsterFromPool(crushMonsters, activeBans);
                ring6Monsters.Add(crushMonster);
                Debug.Log("We need a crush monster: " + crushMonster.name);
                buildLogForcedProgression("Crush", crushMonster);
            }
            if (!exploreAbilities[6])
            {
                Monster bigRockMonster = getMonsterFromPool(bigRockMonsters, activeBans);
                ring6Monsters.Add(bigRockMonster);
                Debug.Log("We need a Big Rock monster: " + bigRockMonster.name);
                buildLogForcedProgression("Big Rock", bigRockMonster);
            }
            if (!exploreAbilities[8])
            {
                Monster levitateMonster = getMonsterFromPool(levitateMonsters, activeBans);
                ring6Monsters.Add(levitateMonster);
                Debug.Log("We need a levitate monster: " + levitateMonster.name);
                buildLogForcedProgression("Levitate", levitateMonster);
            }
            if (!exploreAbilities[11])
            {
                Monster blobMonster = getMonsterFromPool(blobMonsters, activeBans);
                ring6Monsters.Add(blobMonster);
                Debug.Log("We need a blob form monster: " + blobMonster.name);
                buildLogForcedProgression("Blob Form", blobMonster);
            }
            if (!exploreAbilities[5])
            {
                Monster grappleMonster = getMonsterFromPool(grappleMonsters, activeBans);
                ring6Monsters.Add(grappleMonster);
                Debug.Log("We need Grapple: " + grappleMonster.name);
                buildLogForcedProgression("Grapple", grappleMonster);
            }
            // We have everything, fill the list and set it to the army keeper.
            fillListWithMonsters(activeBans, ring6Monsters, 7);
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

        private void GetRing5Monsters()
        {
            // Ring 5 is the DLC and Blob Burg. No requirements.
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<int> ring5Areas = new List<int>() { 11, 12 };
            List<Monster> ring5Monsters = new List<Monster>();

            fillListWithMonsters(activeBans, ring5Monsters, ring5Areas.Count);
            distributeMonstersToAreas(ring5Monsters, ring5Areas);

            //debug
            logExplorationAbilityArray();
        }

        private void GetRing4Monsters()
        {
            // Ring 4 core is the Underworld and Abandoned Tower. Mystical Workshop might be in this ring as well, if we needed an Improved Flyer until last
            // ring. Once again, only Spectrals are banned, so we don't need a ban list.
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<int> ring4Areas = new List<int>() { 9, 10 };
            List<Monster> ring4Monsters = new List<Monster>();

            // check whether we have been to Mystical Workshop, if not, it's in this ring.
            if (monstersArray[8] == null)
            {
                ring4Areas.Add(8);
            }

            // We need a monster with Secret Vision for the DLC.
            if (!exploreAbilities[4])
            {
                Monster secretVisionMonster = getMonsterFromPool(secretVisionMonsters, activeBans);
                ring4Monsters.Add(secretVisionMonster);
                Debug.Log("We need Secret Vision: " + secretVisionMonster.name);
                buildLogForcedProgression("Secret Vision", secretVisionMonster);
            }

            fillListWithMonsters(activeBans, ring4Monsters, ring4Areas.Count);
            distributeMonstersToAreas(ring4Monsters, ring4Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing3Monsters(GameModeManager self)
        {
            // Ring 3 core is Horizon Beach and Bex. There is a high chance we can access Magma Chamber monster chest before that,
            // if not, it's in this ring.
            // Mystical Workshop might be accessible too if we randomed an Improved Flyer.
            // Banned are only Spectrals
            List<MonsterBanType> activeBans = new List<MonsterBanType>();
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<int> ring3Areas = new List<int>() { 6 };
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

            // we need Improved Flying
            if (!exploreAbilities[3])
            {
                Monster improvedFlyingMonster = getMonsterFromPool(improvedFlyingMonsters, activeBans);
                ring3Monsters.Add(improvedFlyingMonster);
                Debug.Log("We need Improved Flying: " + improvedFlyingMonster.name);
                buildLogForcedProgression("Improved Flying", improvedFlyingMonster);
            }
            // fill the rest with random monsters that are not banned plus an extra one for Bex
            fillListWithMonsters(activeBans, ring3Monsters, ring3Areas.Count + 1);

            // set the Bex monster and remove it from the list
            Monster bexMonster = ring3Monsters[UnityEngine.Random.Range(0, ring3Monsters.Count)];
            self.BexMonster = bexMonster;
            Debug.Log("Bex will give us: " + bexMonster.name);
            ring3Monsters.Remove(bexMonster);

            distributeMonstersToAreas(ring3Monsters, ring3Areas);

            // debug stuff
            logExplorationAbilityArray();

        }

        private void GetRing2Monsters(GameModeManager self)
        {
            // Ring 2 core is only Sun Palace. The rest depends whether we got a mount or a flying monster in the previous ring.
            // Banned are Swimming and Spectral monsters.
            List<MonsterBanType> activeBans = new List<MonsterBanType>() { MonsterBanType.SWIMMING };
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<int> ring2Areas = new List<int>() { 5 };
            List<Monster> ring2Monsters = new List<Monster>();

            // we can add the explore ability of the swimming monster here since we are supposed to beat Sun Palace in this ring
            updateExploreAbilities(self.SwimmingMonster);

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
                Monster mountMonster = getMonsterFromPool(mountMonsters, activeBans);
                ring2Monsters.Add(mountMonster);
                Debug.Log("We need a mount: " + mountMonster.name);
                buildLogForcedProgression("Mount", mountMonster);
            }

            // fill the rest with random monsters that are not banned
            fillListWithMonsters(activeBans, ring2Monsters, ring2Areas.Count);
            distributeMonstersToAreas(ring2Monsters, ring2Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing1Monsters()
        {
            // Ring 1 is complicated. The core is Stronghold Dungeon and Ancient Woods, however we might be able to access
            // Snowy Peaks and Magma Chamber. In this case we have to add them later on. Only swimming monsters and spectrals
            // are banned here.
            List<MonsterBanType> activeBans = new List<MonsterBanType>() { MonsterBanType.SWIMMING };
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<int> ring1Areas = new List<int>() { 2, 3 };
            List<Monster> ring1Monsters = new List<Monster>();

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
                Monster mountOrFlying = getMonsterFromPool(mountMonsters.Union(flyingMonsters).Union(improvedFlyingMonsters).ToList(), activeBans);
                ring1Monsters.Add(mountOrFlying);
                Debug.Log("Need a flying or mount monster: " + mountOrFlying.name);
                buildLogForcedProgression("MountOrFlying", mountOrFlying);
            }

            // fill the rest with random monsters that are not banned
            fillListWithMonsters(activeBans, ring1Monsters, ring1Areas.Count);
            distributeMonstersToAreas(ring1Monsters, ring1Areas);

            // debug stuff
            logExplorationAbilityArray();
        }

        private void GetRing0Monsters()
        {
            // ring 0 is always Mountain Path and Blue Cave. No Swimming or Improved Flying allowed.
            List<MonsterBanType> activeBans = new List<MonsterBanType>() { MonsterBanType.SWIMMING, MonsterBanType.IMPROVED_FLYING };
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            List<Monster> ring0Monsters = new List<Monster>();
            List<int> ring0Areas = new List<int>() { 0, 1 };

            // requirements is only to have a monster that can break walls
            if (!exploreAbilities[0])
            {
                Monster breakWallMonster = getMonsterFromPool(breakWallMonsters, activeBans);
                ring0Monsters.Add(breakWallMonster);
                Debug.Log("Need a BreakWall monster: " + breakWallMonster.name);
                buildLogForcedProgression("BreakWall", breakWallMonster);
            }

            // fill the rest with randoms and distribute them throughout the ring
            fillListWithMonsters(activeBans, ring0Monsters, ring0Areas.Count);
            distributeMonstersToAreas(ring0Monsters, ring0Areas);

            // debug stuff
            logExplorationAbilityArray();

        }

        private void GetStartingMonsters(GameModeManager self)
        {
            Debug.Log("Getting new Starting Monsters");

            // no Swimming or Improved Flying monsters at the start
            List<MonsterBanType> activeBans = new List<MonsterBanType>() { MonsterBanType.SWIMMING, MonsterBanType.IMPROVED_FLYING };
            if (!ValorAllowSpectrals.Value)
            {
                activeBans.Add(MonsterBanType.SPECTRAL);
            }
            if (ValorDisableZoneRestrictions.Value)
            {
                activeBans.Clear();
            }
            if (!ValorAllowDuplicates.Value)
            {
                activeBans.Add(MonsterBanType.DUPLICATE);
            }
            
            List<Monster> monsterPool = getValidMonsterList(allMonsters, activeBans);

            // remove old monsters and get new ones
            PlayerController.Instance.Monsters.Clear();
            
            if (ValorChooseFamiliar.Value < 0 || ValorChooseFamiliar.Value > 3)
            {
                self.FamiliarIndex = UnityEngine.Random.Range(0, 4);
            } else
            {
                self.FamiliarIndex = ValorChooseFamiliar.Value;
            }
            PlayerController.Instance.Monsters.AddMonsterByPrefab(GameController.Instance.MonsterJournalList[self.FamiliarIndex], EShift.Normal, false, null, false, false);
            chosenMonsters.Add(GameController.Instance.MonsterJournalList[self.FamiliarIndex].GetComponent<Monster>());
            // Ignore progression if NewGame+ compatibility option is set
            if (!ValorNewGamePlus.Value)
            {
                updateExploreAbilities(GameController.Instance.MonsterJournalList[self.FamiliarIndex].GetComponent<Monster>());
            }
            // If duplicates are disallowed, remove it from the pool
            if (!ValorAllowDuplicates.Value)
            {
                monsterPool.Remove(GameController.Instance.MonsterJournalList[self.FamiliarIndex].GetComponent<Monster>());
            }
            // set follower to your familiar else it sometimes bugs out and doesn't let you name your starters
            PlayerController.Instance.Follower.Monster = PlayerController.Instance.Monsters.Familiar;
            // random 2 different, non-banned monsters
            for (int i = 0; i < 2; i++)
            {
                GameObject gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range((ValorAllowSpectrals.Value || ValorDisableZoneRestrictions.Value) ? 0 : 4, (ValorAllowBard.Value || ValorDisableZoneRestrictions.Value) ? GameController.Instance.MonsterJournalList.Count : GameController.Instance.MonsterJournalList.Count - 1)];
                while (!monsterPool.Contains(gameObject.GetComponent<Monster>()))
                {
                    gameObject = GameController.Instance.MonsterJournalList[UnityEngine.Random.Range((ValorAllowSpectrals.Value || ValorDisableZoneRestrictions.Value) ? 0 : 4, (ValorAllowBard.Value || ValorDisableZoneRestrictions.Value) ? GameController.Instance.MonsterJournalList.Count : GameController.Instance.MonsterJournalList.Count - 1)];
                };
                PlayerController.Instance.Monsters.AddMonsterByPrefab(gameObject, EShift.Normal, false, null, false, false);
                chosenMonsters.Add(gameObject.GetComponent<Monster>());
                updateExploreAbilities(gameObject.GetComponent<Monster>());
                if (!ValorAllowDuplicates.Value)
                {
                    monsterPool.Remove(gameObject.GetComponent<Monster>());
                }
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

        private Monster getMonsterFromPool(List<Monster> monsterPool, List<MonsterBanType> activeBans)
        {
            List<Monster> validMonsters = getValidMonsterList(monsterPool, activeBans);

            Monster chosenMonster = validMonsters[UnityEngine.Random.Range(0, validMonsters.Count)];
            chosenMonsters.Add(chosenMonster);
            updateExploreAbilities(chosenMonster);
            return chosenMonster;
        }

        private void fillListWithMonsters(List<MonsterBanType> activeBans, List<Monster> monsterList, int amount)
        {
            List<Monster> validMonsters = getValidMonsterList(allMonsters, activeBans);

            for (int i = monsterList.Count; i < amount; i++)
            {
                Monster monster = validMonsters[UnityEngine.Random.Range(0, validMonsters.Count)];
                chosenMonsters.Add(monster);
                monsterList.Add(monster);
                updateExploreAbilities(monster);
                if (!ValorAllowDuplicates.Value)
                {
                    validMonsters.Remove(monster);
                }
            }
        }

        private Monster getMonsterByIndex(int index)
        {
            return GameController.Instance.MonsterJournalList[index].GetComponent<Monster>();
        }

        private List<Monster> getValidMonsterList(List<Monster> monsterPool, List<MonsterBanType> activeBans)
        {
            List<Monster> validMonsters = new List<Monster>(monsterPool);
            foreach (MonsterBanType banType in activeBans)
            {
                switch (banType)
                {
                    case MonsterBanType.SPECTRAL:
                        validMonsters = validMonsters.Except(spectralMonsters).ToList();
                        break;

                    case MonsterBanType.DUPLICATE:
                        validMonsters = validMonsters.Except(chosenMonsters).ToList();
                        break;

                    case MonsterBanType.IMPROVED_FLYING:
                        validMonsters = validMonsters.Except(improvedFlyingMonsters).ToList();
                        break;

                    case MonsterBanType.SWIMMING:
                        validMonsters = validMonsters.Except(swimmingMonsters).ToList();
                        break;
                }

            }

            return validMonsters;
        }

        private void updateExploreAbilities(Monster monster)
        {
            // keep track of monsters with required explore abilities for the cryomancer
            bool isRequired = false;

            // 0-breakwall, 1-flying, 2-mount, 3-improvedflying, 4-secretvision, 5-grapple, 6-bigrock
            // 7-fire, 8-levitate, 9-light, 10-crush, 11-blob, 12-improvedswimming
            if (!exploreAbilities[0] && breakWallMonsters.Contains(monster))
            {
                exploreAbilities[0] = true;
                isRequired = true;
            }
            if (!exploreAbilities[1] && flyingMonsters.Contains(monster))
            {
                exploreAbilities[1] = true;
                isRequired = true;
            }
            if (!exploreAbilities[2] && mountMonsters.Contains(monster))
            {
                exploreAbilities[2] = true;
                isRequired = true;
            }
            if (!exploreAbilities[3] && improvedFlyingMonsters.Contains(monster))
            {
                exploreAbilities[3] = true;
                isRequired = true;
            }
            if (!exploreAbilities[4] && secretVisionMonsters.Contains(monster))
            {
                exploreAbilities[4] = true;
                isRequired = true;
            }
            if (!exploreAbilities[5] && grappleMonsters.Contains(monster))
            {
                exploreAbilities[5] = true;
                isRequired = true;
            }
            if (!exploreAbilities[6] && bigRockMonsters.Contains(monster))
            {
                exploreAbilities[6] = true;
                isRequired = true;
            }
            if (!exploreAbilities[7] && fireMonsters.Contains(monster))
            {
                exploreAbilities[7] = true;
                isRequired = true;
            }
            if (!exploreAbilities[8] && levitateMonsters.Contains(monster))
            {
                exploreAbilities[8] = true;
                isRequired = true;
            }
            if (!exploreAbilities[9] && lightMonsters.Contains(monster))
            {
                exploreAbilities[9] = true;
                isRequired = true;
            }
            if (!exploreAbilities[10] && crushMonsters.Contains(monster))
            {
                exploreAbilities[10] = true;
                isRequired = true;
            }
            if (!exploreAbilities[11] && blobMonsters.Contains(monster))
            {
                exploreAbilities[11] = true;
                isRequired = true;
            }
            if (!exploreAbilities[12] && swimmingMonsters.Skip(1).Contains(monster))
            {
                exploreAbilities[12] = true;
                isRequired = true;
            }

            // monster is not required, so it can be traded away
            if (!isRequired)
            {
                possibleTrades.Add(monster);
            }
        }

        private void addMonsterToArea(Monster monster, int index)
        {
            if (monstersArray[index] != null)
            {
                Debug.LogWarning("Overwriting monster in area " + getAreaByIndex(index) + "!");
            }
            else
            {
                Debug.Log("Adding " + monster.name + " to " + getAreaByIndex(index) + "!");
            }
            monstersArray[index] = monster;
        }

        private string getAreaByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "Mountain Path";
                case 1:
                    return "Blue Cave";
                case 2:
                    return "Stronghold Dungeon";
                case 3:
                    return "Ancient Woods";
                case 4:
                    return "Snowy Peaks";
                case 5:
                    return "Sun Palace";
                case 6:
                    return "Horizon Beach";
                case 7:
                    return "Magma Chamber";
                case 8:
                    return "Mystical Workshop";
                case 9:
                    return "Underworld";
                case 10:
                    return "Abandoned Tower";
                case 11:
                    return "Blob Burg";
                case 12:
                    return "Forgotten World";
                default:
                    return "Area not found";
            }
        }

        private EDifficulty GetStartingDifficulty()
        {
            if (ValorStartingDifficulty == null || ValorStartingDifficulty.Value < 0 || ValorStartingDifficulty.Value > 2)
            {
                return EDifficulty.Master;
            }
            switch (ValorStartingDifficulty.Value)
            {
                case 0:
                    return EDifficulty.Easy;
                case 1:
                    return EDifficulty.Normal;
                default:
                    return EDifficulty.Master;
            }
        }

        // Seed generation log methods
        private void buildLogStartingConfiguration(int seed)
        {
            logBuilder.AppendLine("Valor version: " + ModVersion);
            logBuilder.AppendLine("Seed: " + seed);
            logBuilder.AppendLine("Configuration:");
            logBuilder.AppendLine(string.Format("AllowBard={0}; AllowSpectrals={1}; AllowDuplicates={2}; DisableZoneRestrictions={3}", ValorAllowBard.Value, ValorAllowSpectrals.Value, ValorAllowDuplicates.Value, ValorDisableZoneRestrictions.Value));
            logBuilder.AppendLine(string.Format("ImprovedSwimming={0}; NewGamePlus={1}; ChooseFamiliar={2}", ValorImprovedSwimming.Value, ValorNewGamePlus.Value, ValorChooseFamiliar.Value));
            logBuilder.AppendLine();
        }

        private void buildLogForcedProgression(string progression, Monster monster)
        {
            logBuilder.AppendLine(string.Format("[Forced {0} monster: {1}]", progression, monster.name));
        }

        private void saveLogToFile(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);

            // Create directory if it doesn't exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory); 
            }
            File.WriteAllText(filePath, logBuilder.ToString());
        }

        private void BuildLogStarters()
        {
            logBuilder.AppendLine();
            logBuilder.AppendLine("Familiar: " + chosenMonsters[1].name);
            logBuilder.AppendLine("Starter 1: " + chosenMonsters[2].name);
            logBuilder.AppendLine("Starter 2: " + chosenMonsters[3].name);
        }
        
        private void BuildLogExtraMonsters(GameModeManager self)
        {
            logBuilder.AppendLine("Caretaker: " + self.SwimmingMonster.name);
            logBuilder.AppendLine("Bex: " + self.BexMonster.name);
            for (int i = 0; i < 7; i++)
            {
                logBuilder.AppendLine(string.Format("Keeper's Army {0}: {1}", i + 1, self.MonsterArmyMonsters[i].name));
            }
            for (int i = 0; i < 3; i++)
            {
                logBuilder.AppendLine(string.Format("Eternity's End {0}: {1}", i + 1, self.EndOfTimeMonsters[i].name));
            }
            logBuilder.AppendLine(string.Format("Lady Stasis trade: {0} for {1}", self.CryomancerRequiredMonster.name, self.CryomancerMonster.name));
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

        private List<Monster> initializeAllMonstersList()
        {
            List<Monster> allMonsters = new List<Monster>();

            for (int i = 0; i < 110; i++)
            {
                allMonsters.Add(getMonsterByIndex(i));
            }

            if (ValorAllowBard.Value || ValorDisableZoneRestrictions.Value)
            {
                allMonsters.Add(getMonsterByIndex(110));
                Debug.Log("Adding Bard to monster pool!");
            }
            return allMonsters;
        }

        // debug methods
        private void logExplorationAbilityArray()
        {
            string formattedArray = string.Join(", ", exploreAbilities);

            // Log the formatted array
            Debug.Log("Exploration abilities available: [" + formattedArray + "]");
        }
    }

    public enum MonsterBanType
    {
        DUPLICATE,
        SPECTRAL,
        IMPROVED_FLYING,
        SWIMMING,
    }
}