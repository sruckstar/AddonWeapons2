using System;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Native;
using LemonUI.Menus;
using AddonWeapons2.Weapons;
using AddonWeapons2.UI;
using static AddonWeapons2.UI.Badges;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.UI.WeaponMenu;
using static AddonWeapons2.UI.ComponentMenuHandler;
using static AddonWeapons2.Weapons.WeaponDataUtils;
using static AddonWeapons2.Weapons.WeaponManager;

using System.Linq;
using GTA.Math;
using static System.Windows.Forms.AxHost;

namespace AddonWeapons2.UI
{
    public static class Items
    {
        private static readonly IWeaponTintService _tintService = new WeaponTintService();
        private static readonly IWeaponManager _weaponManager = new WeaponManager();
        private static readonly IBadgeProvider _badgeProvider = new BadgeProvider();

        public static Prop WeaponObject { get; set; }

        public static void Initialize()
        {
            foreach (var category in WeaponInfo.WeaponCategories)
            {
                foreach (var weapon in category.Value)
                {
                    CreateAndAddWeaponItem(weapon);
                }
            }
        }

        private static void CreateAndAddWeaponItem(DlcWeaponDataWithComponents weapon)
        {
            string weapName = GetWeaponName(weapon);
            string weapDesc = Game.GetLocalizedString(weapon.WeaponData.GetDescLabel());
            int weapCost = GetWeaponCost(weapon);
            uint weaponHash = weapon.WeaponData.WeaponHash;

            var item = CreateWeaponItem(weapon, weapName, weapDesc, weapCost, weaponHash);
            AddToAppropriateMenu(item, weaponHash);
        }

        public static NativeItem CreateWeaponItem(DlcWeaponDataWithComponents weapon, string weaponName,
                                                string weaponDesc, int weaponCost, uint weaponHash)
        {
            var item = new NativeItem(weaponName, weaponDesc, $"${weaponCost}");
            var badgeSet = _badgeProvider.GetGunBadge();

            item.Selected += (sender, args) => OnWeaponSelected(weapon, weaponName, weaponHash, weaponCost, badgeSet, item);
            item.Activated += (sender, args) => OnWeaponActivated(weapon, weaponName, weaponHash, weaponCost, badgeSet, item);

            
            if (Game.Player.Character.Weapons.HasWeapon((WeaponHash)weaponHash) &&
                _weaponManager.HasComponentsAvailable(weaponHash))
            {
                item.AltTitle = "";
                item.RightBadgeSet = badgeSet;
            }

            return item;
        }

        private static void OnWeaponActivated(DlcWeaponDataWithComponents weapon, string weaponName,
                                           uint weaponHash, int cost, BadgeSet badgeSet, NativeItem item)
        {
            ComponentMenu.Name = weaponName;

            if (!CanAfford(cost)) return;

            if (Game.Player.Character.Weapons.HasWeapon((WeaponHash)weaponHash))
            {
                HandleExistingWeapon(weapon, weaponHash, badgeSet, item);
            }
            else
            {
                PurchaseNewWeapon(weaponHash, weapon.Components, cost, badgeSet, item);
            }
        }

        private static void OnWeaponSelected(DlcWeaponDataWithComponents weapon, string weaponName,
                                           uint weaponHash, int cost, BadgeSet badgeSet, NativeItem item)
        {
            var nearestBox = AmmoShopManager.GetNearestAmmoBox();
            if (nearestBox.HasValue)
            {
                Vector3 position = nearestBox.Value.Position;
                float heading = nearestBox.Value.Heading;
                Ped player = Game.Player.Character;
                uint playerHash = (uint)Game.Player.Character.Model.Hash;

                //DeleteWeaponObject();
                //WeaponObject = CreateWeaponObject(weaponHash, position);
                //ApplyWeaponShopComponents(player, playerHash, weaponHash, WeaponObject);
                //ApplyWeaponShopTints(player, playerHash, weaponHash, WeaponObject);
            }
        }

        public static Prop CreateWeaponObject(uint WeaponModel, Vector3 position)
        {
            return Function.Call<Prop>(Hash.CREATE_WEAPON_OBJECT, WeaponModel, 1,
                position.X, position.Y, position.Z + 1.0f, true, 1f, 0);
        }

        public static void DeleteWeaponObject()
        {
            if (Items.WeaponObject != null && Items.WeaponObject.Exists())
            {
                Items.WeaponObject.Delete();
                Items.WeaponObject = null;
            }
        }

        private static void HandleExistingWeapon(DlcWeaponDataWithComponents weapon, uint weaponHash,
                                               BadgeSet badgeSet, NativeItem item)
        {
            if (!_weaponManager.HasComponentsAvailable(weaponHash))
            {
                Game.Player.Money -= weapon.WeaponData.AmmoCost;
                Function.Call(Hash.ADD_AMMO_TO_PED, Game.Player.Character, weaponHash, 1);
                return;
            }

            CurrentWeaponHash = weaponHash;
            CloseAllMenus();
            ComponentMenu.Clear();

            SetupWeaponComponents(weapon, weaponHash);
            ComponentMenu.Visible = true;
        }

        private static void SetupWeaponComponents(DlcWeaponDataWithComponents weapon, uint weaponHash)
        {
            if (weapon.Components.Count == 0)
            {
                AddAmmoItemIfNeeded(weapon, weaponHash);
                _tintService.AddTintOptions(weapon, weapon.WeaponData.GetNameLabel(), weaponHash, ComponentMenu);

                if ((WeaponHash)weaponHash == WeaponHash.StunGunMultiplayer)
                {
                    AddStunGunBailComponent(weapon, weaponHash);
                }
            }
            else
            {
                AddAmmoItemIfNeeded(weapon, weaponHash);
                _tintService.AddTintOptions(weapon, weapon.WeaponData.GetNameLabel(), weaponHash, ComponentMenu);

                foreach (var component in weapon.Components)
                {
                    var componentItem = CreateComponentItem(
                        weapon,
                        Game.GetLocalizedString(component.GetNameLabel()),
                        $"${component.ComponentCost}",
                        component.ComponentCost,
                        component.ComponentHash,
                        weaponHash,
                        weapon.WeaponData.DefaultClipSize,
                        $"${weapon.WeaponData.AmmoCost}"
                    );
                    ComponentMenu.Add(componentItem);
                }
            }

            RefreshComponentMenuAfterAction(weapon);
        }

        private static void AddAmmoItemIfNeeded(DlcWeaponDataWithComponents weapon, uint weaponHash)
        {
            if (!_weaponManager.UsesAmmo(weaponHash)) return;

            var ammoItem = CreateAmmoItem(
                weapon.WeaponData.DefaultClipSize,
                $"${weapon.WeaponData.AmmoCost}",
                weapon.WeaponData.AmmoCost,
                weaponHash
            );
            ComponentMenu.Add(ammoItem);
        }

        private static void AddStunGunBailComponent(DlcWeaponDataWithComponents weapon, uint weaponHash)
        {
            string compName = Game.GetLocalizedString("WCT_STNGN_BAIL");
            uint componentHash = Function.Call<uint>(Hash.GET_HASH_KEY, "COMPONENT_STUNGUN_VARMOD_BAIL");
            var compItem = CreateComponentItem(
                weapon,
                compName,
                "$1000",
                1000,
                componentHash,
                weaponHash,
                weapon.WeaponData.DefaultClipSize,
                $"${weapon.WeaponData.AmmoCost}"
            );
            ComponentMenu.Add(compItem);
        }

        private static NativeItem CreateAmmoItem(int defaultClipSize, string ammoCost, int cost, uint weaponHash)
        {
            string title = _weaponManager.IsMaxAmmo(weaponHash) ?
                "_MAX_ROUNDS" : $"_ROUNDS x {defaultClipSize}";

            var item = new NativeItem(title, "", ammoCost);
            item.Activated += (sender, args) => OnAmmoPurchased(weaponHash, cost, defaultClipSize, item);
            return item;
        }

        private static void OnAmmoPurchased(uint weaponHash, int cost, int clipSize, NativeItem item)
        {
            if (!CanAfford(cost)) return;

            if (!_weaponManager.IsMaxAmmo(weaponHash))
            {
                Game.Player.Money -= cost;
                Function.Call(Hash.ADD_PED_AMMO_BY_TYPE,
                    Game.Player.Character,
                    Function.Call<Hash>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, Game.Player.Character, weaponHash),
                    clipSize);

                uint player = (uint)Game.Player.Character.Model.Hash;
                int currentAmmo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, Game.Player.Character, weaponHash);
                AddWeaponData(AmmoDict, player, weaponHash, 0, currentAmmo);
                WeaponSave.SaveWeaponInventory(player);

                if (_weaponManager.IsMaxAmmo(weaponHash))
                {
                    item.AltTitle = "_MAX_ROUNDS";
                }
            }
        }

        private static void PurchaseNewWeapon(uint weaponHash, List<DlcComponentData> components, int cost, BadgeSet badgeSet, NativeItem item)
        {
            Game.Player.Money -= cost;
            uint player = (uint)Game.Player.Character.Model.Hash;

            if (_weaponManager.IsMeleeOrThrown(weaponHash))
            {
                Game.Player.Character.Weapons.Give((WeaponHash)weaponHash, 1, true, true);
            }
            else
            {
                item.AltTitle = "";
                item.RightBadgeSet = badgeSet;
                Game.Player.Character.Weapons.Give((WeaponHash)weaponHash, 1000, true, true);

                AddWeaponData(EmptyDict, player, weaponHash, 0, 0);
                WeaponSave.SaveWeaponInventory(player);
            }
        }

        private static void RefreshComponentMenuAfterAction(DlcWeaponDataWithComponents weapon)
        {
            var components = _weaponManager.GetComponentsList(weapon);
            var costs = _weaponManager.GetComponentsCost(weapon);
            RefreshComponentMenu(ComponentMenu, components, costs);
        }

        private static bool CanAfford(int amount)
        {
            return Game.Player.Money >= amount || IsFreemodeCharacter();
        }

        private static bool IsFreemodeCharacter()
        {
            var model = Game.Player.Character.Model;
            return model == new Model("mp_m_freemode_01") || model == new Model("mp_f_freemode_01");
        }

        private static string GetWeaponName(DlcWeaponDataWithComponents weapon)
        {
            string name = Game.GetLocalizedString(weapon.WeaponData.GetNameLabel());
            if (string.IsNullOrEmpty(name) || name.Length < 3)
            {
                name = weapon.WeaponData.GetNameLabel();
                if (name == "WT_SNOWLAUNCHER")
                    name = Game.GetLocalizedString("WT_SNOWLNCHR");
            }
            return name;
        }

        private static int GetWeaponCost(DlcWeaponDataWithComponents weapon)
        {
            // Реализация получения стоимости оружия
            // (может включать чтение из конфигурации)
            return weapon.WeaponData.WeaponCost;
        }

        private static void AddToAppropriateMenu(NativeItem weaponItem, uint weaponHash)
        {
            // 1. Проверка пользовательских категорий из commandline.txt
            if (TryAddToCustomMenu(weaponItem, weaponHash))
                return;

            // 2. Добавление в стандартные категории по типу оружия
            var weaponGroup = Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weaponHash);

            switch (weaponGroup)
            {
                case (uint)AddonWeaponGroup.Heavy:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Heavy)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.Melee:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Melee)?.Add(weaponItem);
                    WeaponInfo.NoAmmoWeapons.Add(weaponHash);
                    break;
                case (uint)AddonWeaponGroup.MG:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.MG)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.Pistol:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Pistol)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.Rifle:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Rifle)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.Shotgun:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Shotgun)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.SMG:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.SMG)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.Sniper:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Sniper)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.StunGun:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.StunGun)?.Add(weaponItem);
                    WeaponInfo.NoAmmoWeapons.Add(weaponHash);
                    break;
                case (uint)AddonWeaponGroup.Thrown:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.Thrown)?.Add(weaponItem);
                    break;
                case (uint)AddonWeaponGroup.RubberGun:
                    WeaponMenu.GetMenuForCategory(WeaponCategory.RubberGun)?.Add(weaponItem);
                    break;
            }
        }

        private static bool TryAddToCustomMenu(NativeItem weaponItem, uint weaponHash)
        {
            if (!File.Exists("Scripts\\AddonWeapons\\commandline.txt"))
                return false;

            foreach (string command in File.ReadLines("Scripts\\AddonWeapons\\commandline.txt"))
            {
                string trimmedCmd = command.Trim();
                if (!trimmedCmd.StartsWith("PutWeaponToCategory"))
                    continue;

                string[] parameters = _weaponManager.ExtractParameters(trimmedCmd, "PutWeaponToCategory", 2);
                if (parameters == null || new Model(parameters[0]).Hash != (int)weaponHash)
                    continue;

                foreach (var customMenu in WeaponMenu.CustomMenusList)
                {
                    if (customMenu.Name.Equals(parameters[1]))
                    {
                        customMenu.Add(weaponItem);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}