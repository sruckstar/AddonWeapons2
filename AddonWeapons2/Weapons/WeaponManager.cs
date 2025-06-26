using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GTA;
using GTA.Native;
using static AddonWeapons2.Weapons.WeaponInfo;
using static AddonWeapons2.Weapons.WeaponDataUtils;
using AddonWeapons2.UI;
using System.Linq;

namespace AddonWeapons2.Weapons
{
    public class WeaponManager : IWeaponManager
    {
        public List<uint> GetComponentsList(DlcWeaponDataWithComponents weapon)
        {
            var components = new List<uint>();
            if (weapon?.Components != null)
            {
                foreach (var component in weapon.Components)
                {
                    if (!UI.ComponentMenuHandler.DisableComponentsList.Contains(component.ComponentHash))
                    {
                        components.Add(component.ComponentHash);
                    }
                }
            }
            return components;
        }

        public List<int> GetComponentsCost(DlcWeaponDataWithComponents weapon)
        {
            var costs = new List<int>();
            if (weapon?.Components != null)
            {
                foreach (var component in weapon.Components)
                {
                    if (!UI.ComponentMenuHandler.DisableComponentsList.Contains(component.ComponentHash))
                    {
                        costs.Add(component.ComponentCost);
                    }
                }
            }
            return costs;
        }

        public bool IsMaxAmmo(uint weaponHash)
        {
            unsafe
            {
                int maxAmmo = 0;
                Function.Call(Hash.GET_MAX_AMMO, Game.Player.Character, weaponHash, &maxAmmo);
                int currentAmmo = Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, Game.Player.Character, weaponHash);
                return currentAmmo >= maxAmmo;
            }
        }

        public string[] ExtractParameters(string command, string commandName, int paramCount)
        {
            if (string.IsNullOrWhiteSpace(command) || !command.StartsWith(commandName))
                return null;

            string parametersStr = command.Substring(commandName.Length).Trim();
            return parametersStr.Split(new[] { ' ' }, paramCount, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool HasComponentsAvailable(uint weaponHash)
        {
            return WeaponInfo.AreComponentsAvailable(weaponHash);
        }

        public uint GetWeaponTypeGroup(uint weaponHash)
        {
            return Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, weaponHash);
        }

        public bool UsesAmmo(uint weaponHash)
        {
            return !WeaponInfo.NoAmmoWeapons.Contains(weaponHash);
        }

        public bool IsMeleeOrThrown(uint weaponHash)
        {
            var group = GetWeaponTypeGroup(weaponHash);
            return group == (uint)AddonWeaponGroup.Melee || group == (uint)AddonWeaponGroup.Thrown;
        }

        public static void ApplyWeaponShopComponents(Ped ped, uint playerModelHash, uint weaponHash, Prop WeaponObject)
        {
            //Disable Preview Components
            if (PreviewComponent != 0)
            {
                Function.Call(Hash.REMOVE_WEAPON_COMPONENT_FROM_WEAPON_OBJECT, WeaponObject, PreviewComponent);
                PreviewComponent = 0;
            }
           
            //Install Purchased Components
            if (!InstalledComponents[playerModelHash].ContainsKey(weaponHash)) return;
            foreach (var componentHash in InstalledComponents[playerModelHash][weaponHash])
            {
                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, WeaponObject, componentHash);
            }
        }

        public static void ApplyWeaponShopTints(Ped ped, uint playerModelHash, uint weaponHash, Prop WeaponObject)
        {
            if (!PurchasedTints[playerModelHash].ContainsKey(weaponHash)) return;

            foreach (var tint in PurchasedTints[playerModelHash][weaponHash])
            {
                if (ContainsWeaponData(InstalledTintsDict, playerModelHash, weaponHash, 0, tint))
                {
                    Function.Call(Hash.SET_WEAPON_OBJECT_TINT_INDEX, WeaponObject, tint);
                }
            }
        }

        public static void SetWeaponShopTintPreview(Prop WeaponObject, int tintIndex)
        {
            Function.Call(Hash.SET_WEAPON_OBJECT_TINT_INDEX, WeaponObject, tintIndex);
        }

        public static void SetWeaponShopComponentPreview(Prop WeaponObject, uint componentHash)
        {
            int component = LoadComponentModel(componentHash);

            Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, WeaponObject, componentHash);
            PreviewComponent = componentHash;
            GTA.UI.Screen.ShowSubtitle($"Установлено превью {component}");
        }

        public void SerializeDictionary<T1, T2, T3>(string filePath, Dictionary<T1, Dictionary<T2, List<T3>>> dictionary)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, dictionary);
            }
        }

        public Dictionary<T1, Dictionary<T2, List<T3>>> DeserializeDictionary<T1, T2, T3>(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (Dictionary<T1, Dictionary<T2, List<T3>>>)formatter.Deserialize(fs);
            }
        }

        public static int LoadComponentModel(uint component)
        {
            int componentModel = Function.Call<int>(Hash.GET_WEAPON_COMPONENT_TYPE_MODEL, component);

            if (componentModel != 0)
            {
                Function.Call(Hash.REQUEST_MODEL, componentModel);

                while (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, componentModel))
                {
                    WeaponMenu.Pool.Process();
                    Script.Wait(0);
                }

                return componentModel;
            }

            return 0;
        }
    }
}