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
using static AddonWeapons2.Common.PlayerUtils;

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

        private const float PreviewHoverHeight = 0.9f;
        private const float PreviewForwardOffset = 0.5f;
        private const float PreviewSpinSpeed = 45f;

        private static Vector3 _previewBasePos;
        private static float _previewYaw;

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
            PreviewWeapon(weaponHash);
        }

        public static void PreviewWeapon(uint weaponHash)
        {
            var nearestBox = AmmoShopManager.GetNearestAmmoBox();
            if (!nearestBox.HasValue) return;

            Ped player = Game.Player.Character;
            uint playerHash = (uint)player.Model.Hash;

            CurrentWeaponHash = weaponHash;

            DeleteWeaponObject();
            WeaponObject = CreateWeaponObject(weaponHash, nearestBox.Value.Position);
            if (WeaponObject == null || !WeaponObject.Exists()) return;

            SetupPreviewObject(nearestBox.Value.Position);
            ApplyWeaponShopComponents(player, playerHash, weaponHash, WeaponObject);
            ApplyWeaponShopTints(player, playerHash, weaponHash, WeaponObject);

            WeaponPreviewCamera.EnsureActive(_previewBasePos);
        }

        public static Prop CreateWeaponObject(uint WeaponModel, Vector3 position)
        {
            return Function.Call<Prop>(Hash.CREATE_WEAPON_OBJECT, WeaponModel, 1,
                position.X, position.Y, position.Z + PreviewHoverHeight, true, 1f, 0);
        }

        private static void SetupPreviewObject(Vector3 boxPosition)
        {
            _previewBasePos = ComputePreviewBasePos(boxPosition);
            _previewYaw = 0f;
            FreezePreviewObject();
        }

        private static Vector3 ComputePreviewBasePos(Vector3 boxPosition)
        {
            Vector3 toPlayer = Game.Player.Character.Position - boxPosition;
            toPlayer.Z = 0f;
            if (toPlayer.Length() < 0.01f)
                toPlayer = new Vector3(0f, 1f, 0f);
            toPlayer.Normalize();

            return new Vector3(boxPosition.X, boxPosition.Y, boxPosition.Z + PreviewHoverHeight)
                   + toPlayer * PreviewForwardOffset;
        }

        private static void FreezePreviewObject()
        {
            Function.Call(Hash.SET_ENTITY_COLLISION, WeaponObject, false, false);
            Function.Call(Hash.FREEZE_ENTITY_POSITION, WeaponObject, true);
            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, WeaponObject,
                _previewBasePos.X, _previewBasePos.Y, _previewBasePos.Z, false, false, false);
            Function.Call(Hash.SET_ENTITY_ROTATION, WeaponObject, 0f, 0f, _previewYaw, 2, true);
        }

        public static void RebuildPreviewObject(uint weaponHash)
        {
            if (_previewBasePos == Vector3.Zero) return;

            DeleteWeaponObject();
            WeaponObject = Function.Call<Prop>(Hash.CREATE_WEAPON_OBJECT, weaponHash, 1,
                _previewBasePos.X, _previewBasePos.Y, _previewBasePos.Z, true, 1f, 0);
            if (WeaponObject == null || !WeaponObject.Exists()) return;

            FreezePreviewObject();
            PreviewComponent = 0;
        }

        public static void UpdateWeaponPreview()
        {
            if (WeaponObject == null || !WeaponObject.Exists()) return;

            _previewYaw += PreviewSpinSpeed * Game.LastFrameTime;
            if (_previewYaw >= 360f) _previewYaw -= 360f;

            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, WeaponObject,
                _previewBasePos.X, _previewBasePos.Y, _previewBasePos.Z, false, false, false);
            Function.Call(Hash.SET_ENTITY_ROTATION, WeaponObject, 0f, 0f, _previewYaw, 2, true);
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
            AddAmmoItemIfNeeded(weapon, weaponHash);
            _tintService.AddTintOptions(weapon, weapon.WeaponData.GetNameLabel(), weaponHash, ComponentMenu);

            if (weapon.Components.Count == 0)
            {
                if ((WeaponHash)weaponHash == WeaponHash.StunGunMultiplayer)
                {
                    AddStunGunBailComponent(weapon, weaponHash);
                }
            }
            else
            {
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
            return weapon.WeaponData.WeaponCost;
        }

        private static readonly Dictionary<uint, WeaponCategory> _groupToCategory =
            new Dictionary<uint, WeaponCategory>
            {
                { (uint)AddonWeaponGroup.Heavy,    WeaponCategory.Heavy },
                { (uint)AddonWeaponGroup.Melee,    WeaponCategory.Melee },
                { (uint)AddonWeaponGroup.MG,       WeaponCategory.MG },
                { (uint)AddonWeaponGroup.Pistol,   WeaponCategory.Pistol },
                { (uint)AddonWeaponGroup.Rifle,    WeaponCategory.Rifle },
                { (uint)AddonWeaponGroup.Shotgun,  WeaponCategory.Shotgun },
                { (uint)AddonWeaponGroup.SMG,      WeaponCategory.SMG },
                { (uint)AddonWeaponGroup.Sniper,   WeaponCategory.Sniper },
                { (uint)AddonWeaponGroup.StunGun,  WeaponCategory.StunGun },
                { (uint)AddonWeaponGroup.Thrown,   WeaponCategory.Thrown },
                { (uint)AddonWeaponGroup.RubberGun, WeaponCategory.RubberGun },
            };

        private static readonly HashSet<uint> _noAmmoGroups = new HashSet<uint>
        {
            (uint)AddonWeaponGroup.Melee,
            (uint)AddonWeaponGroup.StunGun,
        };

        private static void AddToAppropriateMenu(NativeItem weaponItem, uint weaponHash)
        {
            if (TryAddToCustomMenu(weaponItem, weaponHash))
                return;

            var weaponGroup = Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weaponHash);

            if (_groupToCategory.TryGetValue(weaponGroup, out var category))
                WeaponMenu.GetMenuForCategory(category)?.Add(weaponItem);

            if (_noAmmoGroups.Contains(weaponGroup))
                WeaponInfo.NoAmmoWeapons.Add(weaponHash);
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