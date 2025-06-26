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
            WeaponCategories.Add(3539449195u, new List<DlcWeaponDataWithComponents>()); // GROUP_DIGISCANNER
            WeaponCategories.Add(4257178988u, new List<DlcWeaponDataWithComponents>()); // GROUP_FIREEXTINGUISHER
            WeaponCategories.Add(1175761940u, new List<DlcWeaponDataWithComponents>()); // GROUP_HACKINGDEVICE
            WeaponCategories.Add(2725924767u, new List<DlcWeaponDataWithComponents>()); // GROUP_HEAVY
            WeaponCategories.Add(3566412244u, new List<DlcWeaponDataWithComponents>()); // GROUP_MELEE
            WeaponCategories.Add(3759491383u, new List<DlcWeaponDataWithComponents>()); // GROUP_METALDETECTOR
            WeaponCategories.Add(1159398588u, new List<DlcWeaponDataWithComponents>()); // GROUP_MG
            WeaponCategories.Add(3493187224u, new List<DlcWeaponDataWithComponents>()); // GROUP_NIGHTVISION
            WeaponCategories.Add(431593103u, new List<DlcWeaponDataWithComponents>()); // GROUP_PARACHUTE
            WeaponCategories.Add(1595662460u, new List<DlcWeaponDataWithComponents>()); // GROUP_PETROLCAN
            WeaponCategories.Add(416676503u, new List<DlcWeaponDataWithComponents>()); // GROUP_PISTOL
            WeaponCategories.Add(970310034u, new List<DlcWeaponDataWithComponents>()); // GROUP_RIFLE
            WeaponCategories.Add(860033945u, new List<DlcWeaponDataWithComponents>()); // GROUP_SHOTGUN
            WeaponCategories.Add(3337201093u, new List<DlcWeaponDataWithComponents>()); // GROUP_SMG
            WeaponCategories.Add(3082541095u, new List<DlcWeaponDataWithComponents>()); // GROUP_SNIPER
            WeaponCategories.Add(690389602u, new List<DlcWeaponDataWithComponents>()); // GROUP_STUNGUN
            WeaponCategories.Add(1548507267u, new List<DlcWeaponDataWithComponents>()); // GROUP_THROWN
            WeaponCategories.Add(75159441u, new List<DlcWeaponDataWithComponents>()); // GROUP_TRANQILIZER
            WeaponCategories.Add(88899580u, new List<DlcWeaponDataWithComponents>()); // GROUP_RUBBERGUN
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
