using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.Weapons.WeaponManager;
using static AddonWeapons2.Weapons.WeaponDataUtils;

namespace AddonWeapons2.Weapons
{
    public static class WeaponSave
    {
        private const string SaveDirectory = "Scripts\\AddonWeapons\\bin";
        private const string ComponentsFile = "components.bin";
        private const string TintsFile = "tints.bin";
        private const string InstallComponentsFile = "install_components.bin";
        private const string InstallAmmoFile = "install_ammo.bin";
        private const string InstallTintsFile = "install_tints.bin";

        private static readonly IWeaponManager _weaponManager = new WeaponManager();

        // Player model hashes
        private static readonly uint PlayerZeroHash = (uint)PedHash.Michael;
        private static readonly uint PlayerOneHash = (uint)PedHash.Franklin;
        private static readonly uint PlayerTwoHash = (uint)PedHash.Trevor;
        private static readonly uint FreemodeMaleHash = (uint)PedHash.FreemodeMale01;
        private static readonly uint FreemodeFemaleHash = (uint)PedHash.FreemodeFemale01;

        private static readonly Model[] MainCharacterModels =
        {
            new Model(PedHash.Michael),
            new Model(PedHash.Franklin),
            new Model(PedHash.Trevor),
            new Model(PedHash.FreemodeMale01),
            new Model(PedHash.FreemodeFemale01)
        };

        private static Dictionary<PlayerModel, bool> _loadedInventoryFlags = new Dictionary<PlayerModel, bool>
        {
            { PlayerModel.Michael, false },
            { PlayerModel.Franklin, false },
            { PlayerModel.Trevor, false },
            { PlayerModel.FreemodeMale, false },
            { PlayerModel.FreemodeFemale, false }
        };

        private static uint _lastPlayerModelHash = 0;
        private static int _saveInProgress = 0;
        private static Dictionary<uint, uint> _currentWeapons = new Dictionary<uint, uint>();

        public static void SaveWeaponInventory(uint playerModelHash)
        {
            var player = Game.Player.Character;
            var weaponsHashes = new List<uint>(PurchasedComponents[playerModelHash].Keys);

            foreach (var weaponHash in weaponsHashes)
            {
                if (!PurchasedComponents[playerModelHash].ContainsKey(weaponHash))
                    continue;

                int currentAmmo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, player, weaponHash);
                AddWeaponData(AmmoDict, playerModelHash, weaponHash, 0, currentAmmo);

                foreach (var componentHash in PurchasedComponents[playerModelHash][weaponHash])
                {
                    if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, player, weaponHash, componentHash))
                    {
                        AddWeaponData(InstalledComponentsDict, playerModelHash, weaponHash, componentHash, 0);
                    }
                }
            }

            SaveDictionary(PurchasedComponents, ComponentsFile);
            SaveDictionary(PurchasedTints, TintsFile);
            SaveDictionary(InstalledComponents, InstallComponentsFile);
            SaveDictionary(InstalledAmmo, InstallAmmoFile);
            SaveDictionary(InstalledTints, InstallTintsFile);
        }

        public static void WaitForLoadedInventory()
        {
            uint currentModelHash = (uint)Game.Player.Character.Model.Hash;

            if (_lastPlayerModelHash == 0)
            {
                LoadInventory(currentModelHash);
                RefreshPedInventory();
                SetCurrentLoadInventoryFlags();
                _lastPlayerModelHash = currentModelHash;
                return;
            }

            if (currentModelHash != _lastPlayerModelHash)
            {
                SaveWeaponInventory(_lastPlayerModelHash);
                RefreshPedInventory();
                LoadInventory(currentModelHash);
                SetCurrentLoadInventoryFlags();
                _lastPlayerModelHash = currentModelHash;
            }
        }

        private static void SetCurrentLoadInventoryFlags()
        {
            var playerModel = GetPlayerModelFromHash((uint)Game.Player.Character.Model.Hash);

            // Reset all flags
            foreach (var key in _loadedInventoryFlags.Keys.ToList())
            {
                _loadedInventoryFlags[key] = false;
            }

            // Set current player flag
            if (playerModel != PlayerModel.Unknown)
            {
                _loadedInventoryFlags[playerModel] = true;
            }
        }

        public static void LoadInventory(uint playerModelHash)
        {
            var player = Game.Player.Character;

            if (IsFreemodeCharacter(player))
            {
                RemoveAllWeapons(player);
            }

            ClearWeaponData();
            EnsureSaveDirectoryExists();
            LoadWeaponDataFromFiles();

            InitializePlayerData(playerModelHash);

            var weaponsHashes = new List<uint>(PurchasedComponents[playerModelHash].Keys);
            if (weaponsHashes.Count == 0) return;

            foreach (var weaponHash in weaponsHashes)
            {
                InitializeWeaponData(playerModelHash, weaponHash);

                if (!player.Weapons.HasWeapon((WeaponHash)weaponHash))
                {
                    player.Weapons.Give((WeaponHash)weaponHash, 0, true, true);
                }

                ApplyWeaponComponents(player, playerModelHash, weaponHash);
                ApplyWeaponTints(player, playerModelHash, weaponHash);

                if (InstalledAmmo[playerModelHash][weaponHash].Count > 0)
                {
                    Function.Call(Hash.ADD_AMMO_TO_PED, player, weaponHash, InstalledAmmo[playerModelHash][weaponHash][0]);
                }

                if (_currentWeapons.ContainsKey(playerModelHash))
                {
                    WaitForPlayerSwitch();
                    Function.Call(Hash.SET_CURRENT_PED_WEAPON, player, _currentWeapons[playerModelHash], false);
                }
            }

            LoadInventoryForNearbyPeds();
        }

        public static void LoadInventoryForPed(Ped ped)
        {
            uint playerModelHash = (uint)ped.Model.Hash;
            var weaponsHashes = new List<uint>(PurchasedComponents[playerModelHash].Keys);

            if (weaponsHashes.Count == 0) return;

            foreach (var weaponHash in weaponsHashes)
            {
                if (!ped.Weapons.HasWeapon((WeaponHash)weaponHash))
                {
                    ped.Weapons.Give((WeaponHash)weaponHash, 0, true, true);
                }

                ApplyWeaponComponentsToPed(ped, playerModelHash, weaponHash);
                ApplyWeaponTintsToPed(ped, playerModelHash, weaponHash);

                Function.Call(Hash.ADD_AMMO_TO_PED, ped, weaponHash, InstalledAmmo[playerModelHash][weaponHash][0]);

                if (_currentWeapons.ContainsKey(playerModelHash))
                {
                    Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, _currentWeapons[playerModelHash], true);
                }
            }
        }

        public static void RefreshPedInventory()
        {
            foreach (var ped in World.GetNearbyPeds(Game.Player.Character.Position, 50f, MainCharacterModels))
            {
                if (ped != null && ped != Game.Player.Character)
                {
                    LoadInventoryForPed(ped);
                }
            }
        }

        #region Helper Methods

        private static void SaveDictionary<T1, T2, T3>(Dictionary<T1, Dictionary<T2, List<T3>>> dict, string fileName)
        {
            _weaponManager.SerializeDictionary<T1, T2, T3>($"{SaveDirectory}\\{fileName}", dict);
        }

        private static bool IsFreemodeCharacter(Ped ped)
        {
            return ped.Model.Hash == FreemodeMaleHash || ped.Model.Hash == FreemodeFemaleHash;
        }

        private static void RemoveAllWeapons(Ped ped)
        {
            foreach (WeaponHash weapon in ped.Weapons.GetAllWeaponHashes())
            {
                if (ped.Weapons.HasWeapon(weapon) && weapon != WeaponHash.Unarmed)
                {
                    ped.Weapons.Remove(weapon);
                }
            }
        }

        private static void ClearWeaponData()
        {
            PurchasedComponents.Clear();
            PurchasedTints.Clear();
            InstalledComponents.Clear();
            InstalledAmmo.Clear();
            InstalledTints.Clear();
        }

        private static void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        private static void LoadWeaponDataFromFiles()
        {
            PurchasedComponents = LoadDictionary<uint, uint, uint>(ComponentsFile);
            PurchasedTints = LoadDictionary<uint, uint, int>(TintsFile);
            InstalledComponents = LoadDictionary<uint, uint, uint>(InstallComponentsFile);
            InstalledAmmo = LoadDictionary<uint, uint, int>(InstallAmmoFile);
            InstalledTints = LoadDictionary<uint, uint, int>(InstallTintsFile);
        }

        private static Dictionary<T1, Dictionary<T2, List<T3>>> LoadDictionary<T1, T2, T3>(string fileName)
        {
            return File.Exists($"{SaveDirectory}\\{fileName}")
                ? _weaponManager.DeserializeDictionary<T1, T2, T3>($"{SaveDirectory}\\{fileName}")
                : new Dictionary<T1, Dictionary<T2, List<T3>>>();
        }

        private static void InitializePlayerData(uint playerModelHash)
        {
            var playerHashes = new List<uint>
            {
                PlayerZeroHash,
                PlayerOneHash,
                PlayerTwoHash,
                FreemodeMaleHash,
                FreemodeFemaleHash
            };

            foreach (uint hash in playerHashes)
            {
                if (!PurchasedComponents.ContainsKey(hash)) PurchasedComponents[hash] = new Dictionary<uint, List<uint>>();
                if (!PurchasedTints.ContainsKey(hash)) PurchasedTints[hash] = new Dictionary<uint, List<int>>();
                if (!InstalledComponents.ContainsKey(hash)) InstalledComponents[hash] = new Dictionary<uint, List<uint>>();
                if (!InstalledAmmo.ContainsKey(hash)) InstalledAmmo[hash] = new Dictionary<uint, List<int>>();
                if (!InstalledTints.ContainsKey(hash)) InstalledTints[hash] = new Dictionary<uint, List<int>>();
            }
        }

        private static void InitializeWeaponData(uint playerModelHash, uint weaponHash)
        {
            if (!PurchasedComponents[playerModelHash].ContainsKey(weaponHash))
                PurchasedComponents[playerModelHash][weaponHash] = new List<uint>();

            if (!PurchasedTints[playerModelHash].ContainsKey(weaponHash))
                PurchasedTints[playerModelHash][weaponHash] = new List<int>();

            if (!InstalledComponents[playerModelHash].ContainsKey(weaponHash))
                InstalledComponents[playerModelHash][weaponHash] = new List<uint>();

            if (!InstalledAmmo[playerModelHash].ContainsKey(weaponHash))
                InstalledAmmo[playerModelHash][weaponHash] = new List<int>();

            if (!InstalledTints[playerModelHash].ContainsKey(weaponHash))
                InstalledTints[playerModelHash][weaponHash] = new List<int>();
        }

        private static void ApplyWeaponComponents(Ped ped, uint playerModelHash, uint weaponHash)
        {
            if (!InstalledComponents[playerModelHash].ContainsKey(weaponHash)) return;

            foreach (var componentHash in InstalledComponents[playerModelHash][weaponHash])
            {
                if (ContainsWeaponData(InstalledComponentsDict, playerModelHash, weaponHash, componentHash, 0))
                {
                    Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped.Handle, weaponHash, componentHash);
                }
            }
        }

        private static void ApplyWeaponTints(Ped ped, uint playerModelHash, uint weaponHash)
        {
            if (!PurchasedTints[playerModelHash].ContainsKey(weaponHash)) return;

            foreach (var tint in PurchasedTints[playerModelHash][weaponHash])
            {
                if (ContainsWeaponData(InstalledTintsDict, playerModelHash, weaponHash, 0, tint))
                {
                    Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, ped, weaponHash, tint);

                    foreach (var componentHash in PurchasedComponents[playerModelHash][weaponHash])
                    {
                        Function.Call(Hash.SET_PED_WEAPON_COMPONENT_TINT_INDEX, ped.Handle, weaponHash, componentHash, tint);
                    }
                }
            }
        }

        private static void ApplyWeaponComponentsToPed(Ped ped, uint playerModelHash, uint weaponHash)
        {
            if (!InstalledComponents[playerModelHash].ContainsKey(weaponHash)) return;

            foreach (var componentHash in InstalledComponents[playerModelHash][weaponHash])
            {
                if (ContainsWeaponData(InstalledComponentsDict, playerModelHash, weaponHash, componentHash, 0))
                {
                    if (!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, ped, weaponHash, componentHash))
                    {
                        Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, ped.Handle, weaponHash, componentHash);
                    }
                }
            }
        }

        private static void ApplyWeaponTintsToPed(Ped ped, uint playerModelHash, uint weaponHash)
        {
            if (!PurchasedTints[playerModelHash].ContainsKey(weaponHash)) return;

            foreach (var tint in PurchasedTints[playerModelHash][weaponHash])
            {
                if (ContainsWeaponData(InstalledTintsDict, playerModelHash, weaponHash, 0, tint))
                {
                    Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, ped, weaponHash, tint);
                }
            }
        }

        private static void LoadInventoryForNearbyPeds()
        {
            foreach (var ped in World.GetAllPeds())
            {
                if (IsMainCharacterModel(ped.Model) && ped != Game.Player.Character)
                {
                    LoadInventoryForPed(ped);
                }
            }
        }

        private static bool IsMainCharacterModel(Model model)
        {
            return model.Hash == PlayerZeroHash ||
                   model.Hash == PlayerOneHash ||
                   model.Hash == PlayerTwoHash ||
                   model.Hash == FreemodeMaleHash ||
                   model.Hash == FreemodeFemaleHash;
        }

        private static void WaitForPlayerSwitch()
        {
            while (Function.Call<int>(Hash.GET_PLAYER_SWITCH_STATE) < 10)
            {
                Script.Wait(0);
            }
        }

        private static PlayerModel GetPlayerModelFromHash(uint modelHash)
        {
            switch (modelHash)
            {
                case var _ when modelHash == PlayerZeroHash: return PlayerModel.Michael;
                case var _ when modelHash == PlayerOneHash: return PlayerModel.Franklin;
                case var _ when modelHash == PlayerTwoHash: return PlayerModel.Trevor;
                case var _ when modelHash == FreemodeMaleHash: return PlayerModel.FreemodeMale;
                case var _ when modelHash == FreemodeFemaleHash: return PlayerModel.FreemodeFemale;
                default: return PlayerModel.Unknown;
            }
        }

        private enum PlayerModel
        {
            Unknown,
            Michael,
            Franklin,
            Trevor,
            FreemodeMale,
            FreemodeFemale
        }

        #endregion
    }
}