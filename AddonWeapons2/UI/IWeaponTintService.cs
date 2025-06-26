using System.Collections.Generic;
using LemonUI.Menus;
using AddonWeapons2.Weapons;
using static AddonWeapons2.Weapons.WeaponInfo;

namespace AddonWeapons2.UI
{
    public interface IWeaponTintService
    {
        void AddTintOptions(DlcWeaponDataWithComponents weapon, string weaponLabel, uint weaponHash, NativeMenu menu);
        List<string> GetTintsForWeapon(string weaponLabel, uint weaponHash);
    }
}