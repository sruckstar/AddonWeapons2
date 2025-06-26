using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using LemonUI.Menus;
using AddonWeapons2.Weapons;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.UI.Badges;
using static AddonWeapons2.Weapons.WeaponDataUtils;
using static AddonWeapons2.Weapons.WeaponManager;
using static AddonWeapons2.UI.Items;

namespace AddonWeapons2.UI
{
    /// <summary>
    /// Интерфейс для сервиса работы с меню компонентов
    /// </summary>
    public interface IComponentMenuService
    {
        /// <summary>
        /// Обновляет меню компонентов
        /// </summary>
        void RefreshComponentMenu(NativeMenu menu, List<uint> componentHashes, List<int> componentCosts);

        /// <summary>
        /// Создает элемент меню для компонента
        /// </summary>
        NativeItem CreateComponentItem(ComponentItemData data);
    }

    public static class ComponentConstants
    {
        public const int DefaultAmmoAmount = 200;
        public const int DefaultDictIndex = 0;
        public const string NoMoneyText = "_NO_MONEY";
        public const string MaxRoundsText = "_MAX_ROUNDS";
        public const string RoundsText = "_ROUNDS";
    }

    public interface IComponentStateManager
    {
        ComponentState GetComponentState(uint player, uint weaponHash, uint componentHash);
        void UnlockComponent(uint player, uint weaponHash, uint componentHash);
        void ToggleComponentInstallation(uint player, uint weaponHash, uint componentHash);
    }

    public struct ComponentState
    {
        public bool IsUnlocked { get; set; }
        public bool IsInstalled { get; set; }
    }

    public struct ComponentItemData
    {
        public DlcWeaponDataWithComponents Weapon { get; set; }
        public string Name { get; set; }
        public string CostDisplay { get; set; }
        public int Cost { get; set; }
        public uint ComponentHash { get; set; }
        public uint WeaponHash { get; set; }
        public int DefaultClipSize { get; set; }
        public string AmmoCostDisplay { get; set; }
    }

    public class ComponentMenuService : IComponentMenuService
    {
        private readonly IComponentStateManager _stateManager;
        private readonly IBadgeProvider _badgeProvider;
        private readonly IWeaponManager _weaponManager;

        public ComponentMenuService(IComponentStateManager stateManager,
                                 IBadgeProvider badgeProvider,
                                 IWeaponManager weaponManager)
        {
            _stateManager = stateManager;
            _badgeProvider = badgeProvider;
            _weaponManager = new WeaponManager();
        }

        public void RefreshComponentMenu(NativeMenu menu, List<uint> componentHashes, List<int> componentCosts)
        {
            var player = GetPlayerModelHash();

            for (int i = 1; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i];
                if (IsTintItem(i))
                    RefreshTintItem(item, i - 1, player);
                else
                    RefreshComponentItem(item, i - 1 - WeaponInfo.TintPrices.Count, componentHashes, componentCosts, player);
            }
        }

        public NativeItem CreateComponentItem(ComponentItemData data)
        {
            var item = new NativeItem(data.Name, string.Empty, data.CostDisplay);
            item.Selected += (s, e) => OnComponentSelected(data);
            item.Activated += (s, e) => OnComponentActivated(data);
            UpdateComponentItemVisuals(item, data);
            return item;
        }

        private void OnComponentSelected(ComponentItemData data)
        {
            Ped player = Game.Player.Character;
            uint playerModelHash = (uint)player.Model.Hash;
            ApplyWeaponShopTints(player, playerModelHash, data.WeaponHash, WeaponObject);
            ApplyWeaponShopComponents(player, playerModelHash, data.WeaponHash, WeaponObject);
            //SetWeaponShopComponentPreview(WeaponObject, data.ComponentHash);
        }

        private void OnComponentActivated(ComponentItemData data)
        {
            var player = GetPlayerModelHash();
            var state = _stateManager.GetComponentState(player, data.WeaponHash, data.ComponentHash);

            if (state.IsUnlocked)
            {
                _stateManager.ToggleComponentInstallation(player, data.WeaponHash, data.ComponentHash);
            }
            else if (CanAfford(data.Cost))
            {
                PurchaseComponent(player, data);
            }
            else
            {
                ShowNoMoneyMessage();
            }

            UpdateMenuAfterAction(data.Weapon);
        }

        private bool IsTintItem(int index) => index - 1 < WeaponInfo.TintPrices.Count;

        private void RefreshTintItem(NativeItem item, int index, uint player)
        {
            var currentTint = Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, Game.Player.Character, CurrentWeaponHash);
            var isPurchased = ContainsWeaponData(TintsDict, player, CurrentWeaponHash, 0, index);

            if (currentTint == index)
            {
                UpdateAsEquippedTint(item, player, index);
            }
            else
            {
                UpdateAsAvailableTint(item, isPurchased, index);
            }
        }

        private void RefreshComponentItem(NativeItem item, int index, List<uint> componentHashes, List<int> componentCosts, uint player)
        {
            if (index < 0 || index >= componentHashes.Count) return;

            var componentHash = componentHashes[index];
            var cost = componentCosts[index];
            var isInstalled = Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, Game.Player.Character, CurrentWeaponHash, componentHash);
            var isPurchased = ContainsWeaponData(ComponentsDict, player, CurrentWeaponHash, componentHash, 0);

            if (isInstalled)
            {
                UpdateAsEquippedComponent(item);
            }
            else
            {
                UpdateAsAvailableComponent(item, isPurchased, cost);
            }
        }

        private void UpdateAsEquippedTint(NativeItem item, uint player, int index)
        {
            if (!ContainsWeaponData(TintsDict, player, CurrentWeaponHash, 0, index))
            {
                AddToTintDictionary(PurchasedTints, player, CurrentWeaponHash, index);
            }
            item.AltTitle = "";
            item.RightBadgeSet = _badgeProvider.GetGunBadge(); // Используем свойство вместо метода
        }

        private void UpdateAsAvailableTint(NativeItem item, bool isPurchased, int index)
        {
            if (isPurchased)
            {
                item.AltTitle = "";
                item.RightBadgeSet = _badgeProvider.GetTickBadge(); // Используем свойство вместо метода
            }
            else
            {
                item.AltTitle = $"${WeaponInfo.TintPrices[index]}";
                item.RightBadgeSet = null;
            }
        }

        private void UpdateAsEquippedComponent(NativeItem item)
        {
            item.AltTitle = "";
            item.RightBadgeSet = _badgeProvider.GetGunBadge();
        }

        private void UpdateAsAvailableComponent(NativeItem item, bool isPurchased, int cost)
        {
            if (isPurchased)
            {
                item.AltTitle = "";
                item.RightBadgeSet = _badgeProvider.GetTickBadge();
            }
            else
            {
                item.AltTitle = $"${cost}";
                item.RightBadgeSet = null;
            }
        }

        private void UpdateComponentItemVisuals(NativeItem item, ComponentItemData data)
        {
            var player = GetPlayerModelHash();
            var state = _stateManager.GetComponentState(player, data.WeaponHash, data.ComponentHash);

            if (state.IsInstalled)
            {
                item.AltTitle = "";
                item.RightBadgeSet = _badgeProvider.GetGunBadge();
            }
            else if (state.IsUnlocked)
            {
                item.AltTitle = "";
                item.RightBadgeSet = _badgeProvider.GetTickBadge();
            }
        }

        private uint GetPlayerModelHash() => (uint)Game.Player.Character.Model.Hash;

        private bool CanAfford(int cost) => Game.Player.Money >= cost || IsMultiplayerCharacter();

        private bool IsMultiplayerCharacter()
        {
            int hash = Game.Player.Character.Model.Hash;
            return hash == new Model("mp_m_freemode_01").Hash || hash == new Model("mp_f_freemode_01").Hash;
        }

        private void PurchaseComponent(uint player, ComponentItemData data)
        {
            Game.Player.Money -= data.Cost;
            _stateManager.UnlockComponent(player, data.WeaponHash, data.ComponentHash);
        }

        private void ShowNoMoneyMessage() => GTA.UI.Screen.ShowSubtitle(ComponentConstants.NoMoneyText);

        private void UpdateMenuAfterAction(DlcWeaponDataWithComponents weapon)
        {
            var hashes = _weaponManager.GetComponentsList(weapon);
            var costs = _weaponManager.GetComponentsCost(weapon);
            RefreshComponentMenu(WeaponMenu.ComponentMenu, hashes, costs);
            WeaponSave.SaveWeaponInventory(GetPlayerModelHash());
        }
    }

    public class ComponentStateManager : IComponentStateManager
    {
        public ComponentState GetComponentState(uint player, uint weaponHash, uint componentHash)
        {
            return new ComponentState
            {
                IsUnlocked = ContainsWeaponData(ComponentsDict, player, weaponHash, componentHash, 0),
                IsInstalled = Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, Game.Player.Character, weaponHash, componentHash)
            };
        }

        public void UnlockComponent(uint player, uint weaponHash, uint componentHash)
        {
            AddToWeaponDictionary(PurchasedComponents, player, weaponHash, componentHash);
        }

        public void ToggleComponentInstallation(uint player, uint weaponHash, uint componentHash)
        {
            if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT, Game.Player.Character, weaponHash, componentHash))
            {
                RemoveComponent(player, weaponHash, componentHash);
            }
            else
            {
                InstallComponent(player, weaponHash, componentHash);
            }
        }

        private void InstallComponent(uint player, uint weaponHash, uint componentHash)
        {
            Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, Game.Player.Character.Handle, weaponHash, componentHash);

            var currentAmmo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, Game.Player.Character, weaponHash);
            if (currentAmmo == 0)
            {
                Function.Call(Hash.ADD_PED_AMMO_BY_TYPE, Game.Player.Character,
                    Function.Call<Hash>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, Game.Player.Character, weaponHash),
                    ComponentConstants.DefaultAmmoAmount);

                AddWeaponData(AmmoDict, player, weaponHash, 0, ComponentConstants.DefaultAmmoAmount);
            }
        }

        private void RemoveComponent(uint player, uint weaponHash, uint componentHash)
        {
            Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_PED, Game.Player.Character.Handle, weaponHash, componentHash);
            RemoveFromWeaponDictionary(InstalledComponents, player, weaponHash, componentHash);
        }
    }

    public static class ComponentMenuHandler
    {
        private static readonly IComponentMenuService _service;
        private static readonly IComponentStateManager _stateManager;

        public static List<uint> DisableComponentsList { get; } = new List<uint>();

        static ComponentMenuHandler()
        {
            var badgeProvider = new BadgeProvider();
            var weaponManager = new WeaponManager();
            _stateManager = new ComponentStateManager();
            _service = new ComponentMenuService(_stateManager, badgeProvider, weaponManager);
        }

        public static void RefreshComponentMenu(NativeMenu componentMenu, List<uint> componentHashes, List<int> componentCosts)
        {
            _service.RefreshComponentMenu(componentMenu, componentHashes, componentCosts);
        }

        public static NativeItem CreateComponentItem(DlcWeaponDataWithComponents weapon, string componentName,
            string componentCost, int cost, uint componentHash, uint weaponHash, int defaultClipSize, string ammoCost)
        {
            var data = new ComponentItemData
            {
                Weapon = weapon,
                Name = componentName,
                CostDisplay = componentCost,
                Cost = cost,
                ComponentHash = componentHash,
                WeaponHash = weaponHash,
                DefaultClipSize = defaultClipSize,
                AmmoCostDisplay = ammoCost
            };

            return _service.CreateComponentItem(data); ;
        }
    }
}