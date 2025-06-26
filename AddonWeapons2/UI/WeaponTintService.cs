using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using GTA.Native;
using LemonUI.Menus;
using AddonWeapons2.Weapons;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.Weapons.WeaponDataUtils;
using static AddonWeapons2.Weapons.WeaponManager;
using static AddonWeapons2.UI.Items; 


namespace AddonWeapons2.UI
{
    public class WeaponTintService : IWeaponTintService
    {
        private static readonly IWeaponManager _weaponManager = new WeaponManager();
        private readonly Dictionary<uint, List<string>> _tintCache = new Dictionary<uint, List<string>>();

        public void AddTintOptions(DlcWeaponDataWithComponents weapon, string weaponLabel, uint weaponHash, NativeMenu menu)
        {
            var tints = GetTintsForWeapon(weaponLabel, weaponHash);
            var prices = GetTintPrices(weaponHash, tints.Count);

            for (int i = 0; i < tints.Count; i++)
            {
                var tintItem = CreateTintMenuItem(weapon, tints[i], i, weaponHash, prices[i]);
                menu.Add(tintItem);
            }
        }

        public List<string> GetTintsForWeapon(string weaponLabel, uint weaponHash)
        {
            if (_tintCache.TryGetValue(weaponHash, out var cachedTints))
                return cachedTints;

            var tints = LoadTintsFromFile(weaponLabel) ?? GetDefaultTints(weaponHash);
            _tintCache[weaponHash] = tints;
            return tints;
        }

        private List<string> LoadTintsFromFile(string weaponLabel)
        {
            string filePath = Path.Combine("Scripts", "AddonWeapons", "tints", $"{weaponLabel}.txt");
            if (!File.Exists(filePath)) return null;

            WeaponInfo.TintPrices.Clear();
            var tints = File.ReadAllLines(filePath).ToList();
            WeaponInfo.TintPrices.AddRange(Enumerable.Repeat(1000, tints.Count));
            return tints;
        }

        private List<string> GetDefaultTints(uint weaponHash)
        {
            int tintCount = Function.Call<int>(Hash.GET_WEAPON_TINT_COUNT, weaponHash);
            tintCount = tintCount == 33 ? 32 : tintCount; //Fix for MK2 weapons. It contains 32 real tints
            string tintPrefix = tintCount == 8 ? "WM_TINT" : "WCT_TINT_";
            WeaponInfo.TintPrices = tintCount == 8 ? WeaponInfo.StandardPrices : WeaponInfo.Mk2Prices;

            return Enumerable.Range(0, tintCount)
                .Select(i => GetLocalizedTintName(tintPrefix, i))
                .ToList();
        }

        private string GetLocalizedTintName(string prefix, int index)
        {
            string name = Game.GetLocalizedString($"{prefix}{index}");
            return string.IsNullOrEmpty(name) ? $"Livery {index}" : name;
        }

        private List<int> GetTintPrices(uint weaponHash, int tintCount)
        {
            return tintCount == 8 ? WeaponInfo.StandardPrices : WeaponInfo.Mk2Prices;
        }

        private NativeItem CreateTintMenuItem(DlcWeaponDataWithComponents weapon, string tintName, int tintIndex, uint weaponHash, int price)
        {
            var item = new NativeItem(tintName, "", $"${price}");
            item.Selected += (sender, args) => OnTintSelected(weapon, tintIndex, weaponHash, price);
            item.Activated += (sender, args) => OnTintActivated(weapon, tintIndex, weaponHash, price);
            return item;
        }

        private void OnTintSelected(DlcWeaponDataWithComponents weapon, int tintIndex, uint weaponHash, int price)
        {
            Ped player = Game.Player.Character;
            uint playerModelHash = (uint)player.Model.Hash;
            ApplyWeaponShopComponents(player, playerModelHash, weaponHash, WeaponObject);
            SetWeaponShopTintPreview(WeaponObject, tintIndex);
        }

        private void OnTintActivated(DlcWeaponDataWithComponents weapon, int tintIndex, uint weaponHash, int price)
        {
            uint player = (uint)Game.Player.Character.Model.Hash;

            if (ContainsInTintDictionary(PurchasedTints, player, weaponHash, tintIndex))
            {
                Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, Game.Player.Character, weaponHash, tintIndex);
                AddToTintDictionary(InstalledTints, player, weaponHash, tintIndex);
            }
            else if (CanAfford(price))
            {
                Game.Player.Money -= price;
                AddWeaponData(TintsDict, player, weaponHash, 0, tintIndex);
            }

            WeaponSave.SaveWeaponInventory(player);
            RefreshComponentMenuAfterAction(weapon);
        }

        private bool CanAfford(int amount)
        {
            return Game.Player.Money >= amount || IsFreemodeCharacter();
        }

        private bool IsFreemodeCharacter()
        {
            var model = Game.Player.Character.Model;
            return model == new Model("mp_m_freemode_01") || model == new Model("mp_f_freemode_01");
        }

        private void RefreshComponentMenuAfterAction(DlcWeaponDataWithComponents weapon)
        {
            var components = _weaponManager.GetComponentsList(weapon);
            var costs = _weaponManager.GetComponentsCost(weapon);
            ComponentMenuHandler.RefreshComponentMenu(WeaponMenu.ComponentMenu, components, costs);
        }
    }
}