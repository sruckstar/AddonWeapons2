using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AddonWeapons2.Weapons.WeaponInfo;

namespace AddonWeapons2.Weapons
{
    public static class CategoriesManager
    {
        public static void InitializeCategories()
        {
            foreach (AddonWeaponGroup group in Enum.GetValues(typeof(AddonWeaponGroup)))
            {
                if (!WeaponCategories.ContainsKey((uint)group))
                    WeaponCategories.Add((uint)group, new List<DlcWeaponDataWithComponents>());
            }
        }

        public static void CategorizeWeapon(DlcWeaponDataWithComponents weaponDataWithComponents, uint weaponTypeGroup)
        {
            if (!WeaponCategories.ContainsKey(weaponTypeGroup))
            {
                WeaponCategories.Add(weaponTypeGroup, new List<DlcWeaponDataWithComponents>());
            }

            WeaponCategories[weaponTypeGroup].Add(weaponDataWithComponents);
        }
    }
}
