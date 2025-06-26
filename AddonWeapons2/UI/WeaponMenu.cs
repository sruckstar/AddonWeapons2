using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Elements;
using AddonWeapons2.Weapons;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.UI.Badges;
using static AddonWeapons2.UI.Items;
using static AddonWeapons2.Weapons.WeaponManager;
using System.Drawing;
using System;
using GTA.Math;

namespace AddonWeapons2.UI
{
    public static class WeaponMenu
    {
        #region Menu Fields
        public static ObjectPool Pool { get; private set; }
        public static NativeMenu MainMenu { get; private set; }
        public static NativeMenu ComponentMenu { get; private set; }
        public static NativeMenu CurrentOpenedMenu { get; private set; }

        private static readonly IWeaponManager _weaponManager = new WeaponManager();

        private static readonly Dictionary<WeaponCategory, NativeMenu> _categoryMenus = new Dictionary<WeaponCategory, NativeMenu>();
        private static readonly List<NativeMenu> _customMenus = new List<NativeMenu>();
        public static List<NativeMenu> CustomMenusList { get; } = new List<NativeMenu>();
        public static event Action<WeaponCategory> OnCategoryMenuClosedByPlayer;
        #endregion

        #region Localization Fields
        public static string TitleMain { get; private set; }
        public static string TitleHeavy { get; private set; }
        public static string TitleMelee { get; private set; }
        public static string TitleMG { get; private set; }
        public static string TitlePistols { get; private set; }
        public static string TitleRifles { get; private set; }
        public static string TitleShotguns { get; private set; }
        public static string TitleSMG { get; private set; }
        public static string TitleSR { get; private set; }
        public static string TitleSG { get; private set; }
        public static string TitleThrown { get; private set; }
        public static string RoundsText { get; private set; }
        public static string MaxRoundsText { get; private set; }
        public static string HelpMessage { get; private set; }
        public static string NoMoneyText { get; private set; }
        #endregion

        private static int _menuOpenedFlag = 0;
        private static ScriptSettings _configSettings;

        public static void Initialize()
        {
            SetLanguage();
            InitializeMenus();
        }

        public static void SetLanguage()
        {
            _configSettings = ScriptSettings.Load("Scripts\\AddonWeapons\\settings.ini");

            TitleMain = GetLocalizedString("GS_TITLE_0", "WEAPONS");
            TitleHeavy = GetLocalizedString("VAULT_WMENUI_6", "Heavy Weapons");
            TitleMelee = GetLocalizedString("VAULT_WMENUI_8", "Melee Weapons");
            TitleMG = GetLocalizedString("VAULT_WMENUI_3", "Machine Guns");
            TitlePistols = GetLocalizedString("VAULT_WMENUI_9", "Pistols");
            TitleRifles = GetLocalizedString("VAULT_WMENUI_4", "Rifles");
            TitleShotguns = GetLocalizedString("VAULT_WMENUI_2", "Shotguns");
            TitleSMG = GetLocalizedString("HUD_MG_SMG", "Submachine Guns");
            TitleSR = GetLocalizedString("VAULT_WMENUI_5", "Sniper Rifles");
            TitleSG = GetLocalizedString("VRT_B_SGUN1", "Stun Guns");
            TitleThrown = GetLocalizedString("VAULT_WMENUI_7", "Explosives");
            RoundsText = GetLocalizedString("GSA_TYPE_R", "Rounds");
            MaxRoundsText = GetLocalizedString("GS_FULL", "FULL");
            HelpMessage = "GS_BROWSE_W";
            NoMoneyText = GetLocalizedString("MPCT_SMON_04", "~z~You'll need more cash to afford that.");
        }

        private static string GetLocalizedString(string key, string defaultValue = "")
        {
            string result = Game.GetLocalizedString(key);
            return string.IsNullOrEmpty(result) || result.Length < 3 ? defaultValue : result;
        }

        private static void InitializeMenus()
        {
            Pool = new ObjectPool();

            MainMenu = CreateMenu("", TitleMain, true, false, false);
            ComponentMenu = CreateMenu("", "", false, false, true);

            InitializeCategoryMenus();
            LoadCustomMenus();

            AddMenusToPool();
        }

        private static NativeMenu CreateMenu(string subtitle, string title, bool IsMainMenu, bool IsWeaponMenu, bool IsComponentMenu)
        {
            NativeMenu WeaponMenu = new NativeMenu(subtitle, title, " ",
                new ScaledTexture(PointF.Empty, new SizeF(0, 108), "shopui_title_gunclub", "shopui_title_gunclub"));

            WeaponMenu.Closed += (sender, args) =>
            {
                Items.DeleteWeaponObject();
            };

            if (IsWeaponMenu || IsComponentMenu)
            {
                WeaponMenu.Opening += (sender, args) =>
                {
                    var nearestBox = AmmoShopManager.GetNearestAmmoBox();
                    if (nearestBox.HasValue)
                    {
                        Vector3 position = nearestBox.Value.Position;
                        float heading = nearestBox.Value.Heading;
                        uint weaponHash = WeaponInfo.CurrentWeaponHash;
                        Ped player = Game.Player.Character;
                        uint playerHash = (uint)Game.Player.Character.Model.Hash;

                        //DeleteWeaponObject();
                        //WeaponObject = CreateWeaponObject(WeaponInfo.CurrentWeaponHash, position);
                        //ApplyWeaponShopComponents(player, playerHash, weaponHash, WeaponObject);
                        //ApplyWeaponShopTints(player, playerHash, weaponHash, WeaponObject);
                    }
                };
            }

            return WeaponMenu;
        }

        private static void InitializeCategoryMenus()
        {
            _categoryMenus.Add(WeaponCategory.Heavy, CreateMenu("", TitleHeavy, false, true, false));
            _categoryMenus.Add(WeaponCategory.Melee, CreateMenu("", TitleMelee, false, true, false));
            _categoryMenus.Add(WeaponCategory.MG, CreateMenu("", TitleMG, false, true, false));
            _categoryMenus.Add(WeaponCategory.Pistol, CreateMenu("", TitlePistols, false, true, false));
            _categoryMenus.Add(WeaponCategory.Rifle, CreateMenu("", TitleRifles, false, true, false));
            _categoryMenus.Add(WeaponCategory.Shotgun, CreateMenu("", TitleShotguns, false, true, false));
            _categoryMenus.Add(WeaponCategory.SMG, CreateMenu("", TitleSMG, false, true, false));
            _categoryMenus.Add(WeaponCategory.Sniper, CreateMenu("", TitleSR, false, true, false));
            _categoryMenus.Add(WeaponCategory.StunGun, CreateMenu("", TitleSG, false, true, false));
            _categoryMenus.Add(WeaponCategory.Thrown, CreateMenu("", TitleThrown, false, true, false));
            _categoryMenus.Add(WeaponCategory.RubberGun, CreateMenu("", "Less Lethal", false, true, false));
            _categoryMenus.Add(WeaponCategory.DigiScanner, CreateMenu("", "Digiscanners", false, true, false));
            _categoryMenus.Add(WeaponCategory.FireExtinguisher, CreateMenu("", "Fire Extinguishers", false, true, false));
            _categoryMenus.Add(WeaponCategory.HackingDevice, CreateMenu("", "Hacking Devices", false, true, false));
            _categoryMenus.Add(WeaponCategory.MetalDetector, CreateMenu("", "Metal Detectors", false, true, false));
            _categoryMenus.Add(WeaponCategory.NightVision, CreateMenu("", "Night Visions", false, true, false));
            _categoryMenus.Add(WeaponCategory.Parachute, CreateMenu("", "Parachutes", false, true, false));
            _categoryMenus.Add(WeaponCategory.PetrolCan, CreateMenu("", "Petrol Cans", false, true, false));
            _categoryMenus.Add(WeaponCategory.Tranquilizer, CreateMenu("", "Tranquilizers", false, true, false));
        }

        private static void LoadCustomMenus()
        {
            if (!File.Exists("Scripts\\AddonWeapons\\commandline.txt"))
                return;

            string[] commands = File.ReadAllLines("Scripts\\AddonWeapons\\commandline.txt");

            foreach (string command in commands)
            {
                string trimmedCommand = command.Trim();
                if (string.IsNullOrEmpty(trimmedCommand) || trimmedCommand.StartsWith("//"))
                    continue;

                if (trimmedCommand.StartsWith("CreateWeaponCategory"))
                {
                    string[] categoryName = _weaponManager.ExtractParameters(trimmedCommand, "CreateWeaponCategory", 1);
                    NativeMenu customMenu = CreateMenu("", categoryName[0], false, true, false);
                    _customMenus.Add(customMenu);
                    MainMenu.AddSubMenu(customMenu);
                }
            }
        }

        private static void AddMenusToPool()
        {
            Pool.Add(MainMenu);
            Pool.Add(ComponentMenu);

            foreach (var menu in _categoryMenus.Values)
            {
                Pool.Add(menu);
            }

            foreach (var menu in _customMenus)
            {
                Pool.Add(menu);
            }
        }

        public static NativeMenu GetMenuForCategory(WeaponCategory category)
        {
            return _categoryMenus.TryGetValue(category, out var menu) ? menu : null;
        }

        public static void CloseAllMenus()
        {
            if (MainMenu.Visible)
            {
                MainMenu.Visible = false;
                CurrentOpenedMenu = MainMenu;
            }

            if (ComponentMenu.Visible)
            {
                ComponentMenu.Visible = false;
                CurrentOpenedMenu = ComponentMenu;
            }

            foreach (var menu in _categoryMenus.Values)
            {
                if (menu.Visible)
                {
                    menu.Visible = false;
                    CurrentOpenedMenu = menu;
                }
            }

            foreach (var menu in _customMenus)
            {
                if (menu.Visible)
                {
                    menu.Visible = false;
                    CurrentOpenedMenu = menu;
                }
            }
        }

        public static void BuildMainMenu()
        {
            foreach (var menu in _categoryMenus.Values)
            {
                if (menu.Items.Count > 0)
                {
                    MainMenu.AddSubMenu(menu);
                }
            }

            foreach (var menu in _customMenus)
            {
                if (menu.Items.Count > 0)
                {
                    MainMenu.AddSubMenu(menu);
                }
            }
        }

        public static bool IsMenuOpen() => Pool.AreAnyVisible && _menuOpenedFlag == 1;

        public static void RefreshAllMenus()
        {
            foreach (var menu in _categoryMenus.Values)
            {
                menu.Clear();
            }

            foreach (var menu in _customMenus)
            {
                menu.Clear();
            }

            Items.Initialize();
        }

        public static void ReOpenLastMenu()
        {
            if (CurrentOpenedMenu != null && !ComponentMenu.Visible)
            {
                CurrentOpenedMenu.Visible = true;
                CurrentOpenedMenu = null;
            }
        }
    }

    public enum WeaponCategory
    {
        Heavy,
        Melee,
        MG,
        Pistol,
        Rifle,
        Shotgun,
        SMG,
        Sniper,
        StunGun,
        Thrown,
        RubberGun,
        DigiScanner,
        FireExtinguisher,
        HackingDevice,
        MetalDetector,
        NightVision,
        Parachute,
        PetrolCan,
        Tranquilizer
    }
}