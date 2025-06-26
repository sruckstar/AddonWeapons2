using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace AddonWeapons2.Weapons
{
    /// <summary>
    /// Provides information about weapons, their components and categories
    /// </summary>
    public static class WeaponInfo
    {
        #region Constants and Enums

        public enum AddonWeaponGroup : uint
        {
            Digiscanner = 3539449195u,
            FireExtinguisher = 4257178988u,
            HackingDevice = 1175761940u,
            Heavy = 2725924767u,
            Melee = 3566412244u,
            MetalDetector = 3759491383u,
            MG = 1159398588u,
            NightVision = 3493187224u,
            Parachute = 431593103u,
            PetrolCan = 1595662460u,
            Pistol = 416676503u,
            Rifle = 970310034u,
            Shotgun = 860033945u,
            SMG = 3337201093u,
            Sniper = 3082541095u,
            StunGun = 690389602u,
            Thrown = 1548507267u,
            Tranquilizer = 75159441u,
            RubberGun = 88899580u
        }

        public enum DictionaryType
        {
            Empty = -1,
            Components,
            InstallComponents,
            Tints,
            Ammo,
            InstallTints
        }

        public const int MaxDlcWeapons = 69;

        #endregion

        #region Weapon Data Storage

        public static Dictionary<uint, List<DlcWeaponDataWithComponents>> WeaponCategories { get; } = new Dictionary<uint, List<DlcWeaponDataWithComponents>>();
        public static Dictionary<uint, string> GroupNames { get; } = new Dictionary<uint, string>();

        // Player weapon customization data
        public static Dictionary<uint, Dictionary<uint, List<uint>>> PurchasedComponents { get; set; } = new Dictionary<uint, Dictionary<uint, List<uint>>>();
        public static Dictionary<uint, Dictionary<uint, List<int>>> PurchasedTints { get; set; } = new Dictionary<uint, Dictionary<uint, List<int>>>();
        public static Dictionary<uint, Dictionary<uint, List<uint>>> InstalledComponents { get; set; } = new Dictionary<uint, Dictionary<uint, List<uint>>>();
        public static Dictionary<uint, Dictionary<uint, List<int>>> InstalledAmmo { get; set; } = new Dictionary<uint, Dictionary<uint, List<int>>>();
        public static Dictionary<uint, Dictionary<uint, List<int>>> InstalledTints { get; set; } = new Dictionary<uint, Dictionary<uint, List<int>>>();



        public static List<uint> NoAmmoWeapons { get; } = new List<uint>();
        public static List<uint> DisabledComponentsWeapons { get; } = new List<uint>();
        public static uint PreviewComponent = 0;

        public static uint CurrentWeaponHash { get; set; } = 0;

        #endregion

        #region Pricing

        public static readonly List<int> StandardPrices = new List<int>
        {
            0, 100, 200, 400, 600, 800, 1000, 1500
        };

        public static readonly List<int> Mk2Prices = new List<int>
        {
            20000, 20000, 30000, 30000, 30000, 30000, 30000, 35000,
            35000, 40000, 40000, 40000, 40000, 75000, 60000, 60000,
            60000, 50000, 50000, 50000, 50000, 45000, 45000, 100000,
            100000, 80000, 80000, 75000, 75000, 75000, 90000, 90000
        };

        public static List<int> TintPrices = new List<int>();

        #endregion

        #region Weapon Data Structures

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct DlcWeaponData
        {
            public int EmptyCheck;
            public int Padding1;
            public uint WeaponHash;
            public int Padding2;
            public int Unknown1;
            public int Padding3;
            public int WeaponCost;
            public int Padding4;
            public int AmmoCost;
            public int Padding5;
            public int AmmoType;
            public int Padding6;
            public int DefaultClipSize;
            public int Padding7;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] NameLabel;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] DescLabel;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Desc2Label;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] UpperCaseNameLabel;

            public string GetNameLabel() => GetString(NameLabel);
            public string GetDescLabel() => GetString(DescLabel);
            public string GetDesc2Label() => GetString(Desc2Label);
            public string GetUpperCaseNameLabel() => GetString(UpperCaseNameLabel);

            private string GetString(byte[] byteArray)
            {
                int count = Array.IndexOf(byteArray, (byte)0);
                if (count == -1) count = byteArray.Length;
                return System.Text.Encoding.ASCII.GetString(byteArray, 0, count);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct DlcComponentData
        {
            public int AttachBone;
            public int Padding1;
            public int IsActiveByDefault;
            public int Padding2;
            public int Unknown1;
            public int Padding3;
            public uint ComponentHash;
            public int Padding4;
            public int Unknown2;
            public int Padding5;
            public int ComponentCost;
            public int Padding6;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] NameLabel;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] DescLabel;

            public string GetNameLabel() => GetString(NameLabel);
            public string GetDescLabel() => GetString(DescLabel);

            private string GetString(byte[] byteArray)
            {
                int count = Array.IndexOf(byteArray, (byte)0);
                if (count == -1) count = byteArray.Length;
                return System.Text.Encoding.ASCII.GetString(byteArray, 0, count);
            }
        }

        public class DlcWeaponDataWithComponents
        {
            public DlcWeaponData WeaponData { get; }
            public List<DlcComponentData> Components { get; }

            public DlcWeaponDataWithComponents(DlcWeaponData weaponData, List<DlcComponentData> components)
            {
                WeaponData = weaponData;
                Components = components;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves all DLC weapon models and their components
        /// </summary>
        /// <summary>
        /// Retrieves all DLC weapon models and their components
        /// </summary>
        public static void LoadDlcWeaponModels()
        {
            int numDlcWeapons = Function.Call<int>(Hash.GET_NUM_DLC_WEAPONS);
            var processedWeaponHashes = new HashSet<uint>(); // Для отслеживания уже обработанных оружий

            for (int i = 0; i < numDlcWeapons; i++)
            {
                IntPtr outData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DlcWeaponData)));
                try
                {
                    if (Function.Call<bool>(Hash.GET_DLC_WEAPON_DATA, i, outData))
                    {
                        var weaponData = Marshal.PtrToStructure<DlcWeaponData>(outData);

                        // Пропускаем дубликаты
                        if (processedWeaponHashes.Contains(weaponData.WeaponHash))
                        {
                            continue;
                        }

                        int WeaponHash = Function.Call<int>(Hash.GET_WEAPONTYPE_MODEL, weaponData.WeaponHash);
                        Model weap = new Model(WeaponHash);
                        while (!weap.IsLoaded)
                        {
                            Script.Wait(0);
                            weap.Request(1000);
                        }

                        processedWeaponHashes.Add(weaponData.WeaponHash); // Добавляем хеш в обработанные

                        var components = GetDlcWeaponComponents(i);
                        var weaponDataWithComponents = new DlcWeaponDataWithComponents(weaponData, components);

                        uint weaponGroup = Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weaponData.WeaponHash);
                        CategoriesManager.CategorizeWeapon(weaponDataWithComponents, weaponGroup);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(outData);
                }
            }
        }

        /// <summary>
        /// Checks if components are available for the specified weapon
        /// </summary>
        /// <param name="weaponHash">Hash of the weapon to check</param>
        /// <returns>True if components are available, false otherwise</returns>
        public static bool AreComponentsAvailable(uint weaponHash)
        {
            var weaponGroup = Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weaponHash);

            return weaponGroup != (uint)AddonWeaponGroup.Melee &&
                   weaponGroup != (uint)AddonWeaponGroup.Thrown &&
                   !DisabledComponentsWeapons.Contains(weaponHash);
        }

        #endregion

        #region Private Methods

        private static List<DlcComponentData> GetDlcWeaponComponents(int dlcWeaponIndex)
        {
            var components = new List<DlcComponentData>();
            int numComponents = Function.Call<int>(Hash.GET_NUM_DLC_WEAPON_COMPONENTS, dlcWeaponIndex);

            for (int j = 0; j < numComponents; j++)
            {
                IntPtr outData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DlcComponentData)));
                try
                {
                    if (Function.Call<bool>(Hash.GET_DLC_WEAPON_COMPONENT_DATA, dlcWeaponIndex, j, outData))
                    {
                        var componentData = Marshal.PtrToStructure<DlcComponentData>(outData);
                        components.Add(componentData);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(outData);
                }
            }

            return components;
        }

        #endregion
    }
}