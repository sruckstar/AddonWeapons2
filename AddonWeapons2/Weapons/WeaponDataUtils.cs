using GTA.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using static AddonWeapons2.Weapons.WeaponInfo;

namespace AddonWeapons2.Weapons
{
    public static class WeaponDataUtils
    {
        public const int EmptyDict = 0;
        public const int ComponentsDict = 1;
        public const int InstalledComponentsDict = 2;
        public const int TintsDict = 3;
        public const int AmmoDict = 4;
        public const int InstalledTintsDict = 5;

        public static void AddWeaponData(int dictionaryType, uint player, uint weaponHash, uint componentHash = 0, int tint = 0)
        {
            switch (dictionaryType)
            {
                case EmptyDict:
                    InitializeWeaponDictionary(PurchasedComponents, player, weaponHash);
                    break;

                case ComponentsDict:
                    AddToWeaponDictionary(PurchasedComponents, player, weaponHash, componentHash);
                    break;

                case InstalledComponentsDict:
                    AddToWeaponDictionary(InstalledComponents, player, weaponHash, componentHash);
                    break;

                case TintsDict:
                    AddToTintDictionary(PurchasedTints, player, weaponHash, tint);
                    break;

                case AmmoDict:
                    UpdateAmmoDictionary(InstalledAmmo, player, weaponHash, tint);
                    break;

                case InstalledTintsDict:
                    UpdateTintDictionary(InstalledTints, player, weaponHash, tint);
                    break;
            }
        }

        public static void RemoveWeaponData(int dictionaryType, uint player, uint weaponHash, uint componentHash = 0, int tint = 0)
        {
            switch (dictionaryType)
            {
                case ComponentsDict:
                    RemoveFromWeaponDictionary(PurchasedComponents, player, weaponHash, componentHash);
                    break;

                case InstalledComponentsDict:
                    RemoveFromWeaponDictionary(InstalledComponents, player, weaponHash, componentHash);
                    break;

                case TintsDict:
                    RemoveFromTintDictionary(PurchasedTints, player, weaponHash, tint);
                    break;

                case AmmoDict:
                    RemoveFromTintDictionary(InstalledAmmo, player, weaponHash, tint);
                    break;

                case InstalledTintsDict:
                    RemoveFromTintDictionary(InstalledTints, player, weaponHash, tint);
                    break;
            }
        }

        public static bool ContainsWeaponData(int dictionaryType, uint player, uint weaponHash, uint componentHash = 0, int tint = 0)
        {
            switch (dictionaryType)
            {
                case ComponentsDict:
                    return ContainsInWeaponDictionary(PurchasedComponents, player, weaponHash, componentHash);

                case InstalledComponentsDict:
                    return ContainsInWeaponDictionary(InstalledComponents, player, weaponHash, componentHash);

                case TintsDict:
                    return ContainsInTintDictionary(PurchasedTints, player, weaponHash, tint);

                case AmmoDict:
                    return ContainsInTintDictionary(InstalledAmmo, player, weaponHash, tint);

                case InstalledTintsDict:
                    return ContainsInTintDictionary(InstalledTints, player, weaponHash, tint);

                default:
                    return false;
            }
        }

        public static void InitializeWeaponDictionary(Dictionary<uint, Dictionary<uint, List<uint>>> dictionary, uint player, uint weaponHash)
        {
            if (!dictionary.ContainsKey(player))
            {
                dictionary[player] = new Dictionary<uint, List<uint>>();
            }

            if (!dictionary[player].ContainsKey(weaponHash))
            {
                dictionary[player][weaponHash] = new List<uint>();
            }
        }

        public static void AddToWeaponDictionary(Dictionary<uint, Dictionary<uint, List<uint>>> dictionary, uint player, uint weaponHash, uint componentHash)
        {
            InitializeWeaponDictionary(dictionary, player, weaponHash);
            dictionary[player][weaponHash].Add(componentHash);
        }

        public static void AddToTintDictionary(Dictionary<uint, Dictionary<uint, List<int>>> dictionary, uint player, uint weaponHash, int tint)
        {
            if (!dictionary.ContainsKey(player))
            {
                dictionary[player] = new Dictionary<uint, List<int>>();
            }

            if (!dictionary[player].ContainsKey(weaponHash))
            {
                dictionary[player][weaponHash] = new List<int>();
            }

            dictionary[player][weaponHash].Add(tint);
        }

        public static void UpdateAmmoDictionary(Dictionary<uint, Dictionary<uint, List<int>>> dictionary, uint player, uint weaponHash, int ammo)
        {
            if (!dictionary.ContainsKey(player))
            {
                dictionary[player] = new Dictionary<uint, List<int>>();
            }

            if (!dictionary[player].ContainsKey(weaponHash))
            {
                dictionary[player][weaponHash] = new List<int> { ammo };
            }
            else
            {
                if (dictionary[player][weaponHash].Count == 0)
                {
                    dictionary[player][weaponHash].Add(ammo);
                }
                else
                {
                    dictionary[player][weaponHash][0] = ammo;
                }
            }
        }

        public static void UpdateTintDictionary(Dictionary<uint, Dictionary<uint, List<int>>> dictionary, uint player, uint weaponHash, int tint)
        {
            UpdateAmmoDictionary(dictionary, player, weaponHash, tint);
        }

        public static void RemoveFromWeaponDictionary(Dictionary<uint, Dictionary<uint, List<uint>>> dictionary, uint player, uint weaponHash, uint componentHash)
        {
            if (dictionary.TryGetValue(player, out var playerDict) &&
                playerDict.TryGetValue(weaponHash, out var components))
            {
                components.RemoveAll(e => e == componentHash);
            }
        }

        public static void RemoveFromTintDictionary(Dictionary<uint, Dictionary<uint, List<int>>> dictionary, uint player, uint weaponHash, int tint)
        {
            if (dictionary.TryGetValue(player, out var playerDict) &&
                playerDict.TryGetValue(weaponHash, out var tints))
            {
                tints.RemoveAll(e => e == tint);
            }
        }

        public static bool ContainsInWeaponDictionary(Dictionary<uint, Dictionary<uint, List<uint>>> dictionary, uint player, uint weaponHash, uint componentHash)
        {
            return dictionary.TryGetValue(player, out var playerDict) &&
                   playerDict.TryGetValue(weaponHash, out var components) &&
                   components.Contains(componentHash);
        }

        public static bool ContainsInTintDictionary(Dictionary<uint, Dictionary<uint, List<int>>> dictionary, uint player, uint weaponHash, int tint)
        {
            return dictionary.TryGetValue(player, out var playerDict) &&
                   playerDict.TryGetValue(weaponHash, out var tints) &&
                   tints.Contains(tint);
        }

        public static (Vector3? Position, float? Heading) ParsePositionAndHeading(string input)
        {
            try
            {
                string[] parts = input.Split(',');
                if (parts.Length != 4) return (null, null);

                var position = new Vector3(
                    float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture)
                );

                float heading = float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture);
                return (position, heading);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}