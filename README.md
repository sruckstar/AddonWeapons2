![#AddonWeapons](https://gtaforums.com/uploads/monthly_2024_05/AddonWeaponsForums.png.1f4e155eeceb30cbd5f8cc529a5fd7ee.png)

Modders create a lot of cool weapons for GTA 5, but they have to replace the standard models. AddonWeapons allows you to add weapons to the game without replacement and buy them in Ammu-nation in a separate menu. There are also weapons available exclusively in GTA Online.

The box of weapons is located in the two Ammu-nations with a shooting range in Los Santos.

The script finds new weapons automatically, just install them like any addon mod (eg. cars).

**IMPORTANT: This mod by itself doesn't bring any new weapons to the game.**
# Installation
1. Install [ScriptHookV](http://dev-c.com/gtav/scripthookv/)
2. Install [ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet/releases/latest)
3. Install [LemonUI (SHVDN3)](https://github.com/LemonUIbyLemon/LemonUI/releases/latest)
4. Move the `scripts` folder into your main GTAV folder (press _Replace the files in the destination_ if Windows asks you to).

# Trainer Mode (BETA)
You can customize AddonWeapons into trainer mode. In this case, the weapon purchase menu can be opened anywhere.
Set the menu activation key in the AddonWeapons\settings.ini file.

# Weapon components (for mod authors)
To display weapon components in the menu, fill in the `weapon_shop.meta` file correctly.

# Weapon tints (for mod authors)
For the tint names to be displayed correctly in the purchase menu, you need to adhere to Rockstar standards: either use a set of 8 tints from standard weapons or a set of 32 tints from MKII weapons.
If you are not adding all the tints in your mod, or combining them from different types of weapons, you will need to create a text file in the AddonWeapons/tints folder with the name of the weapon from `weapon_shop.meta`, and write all the tints names line by line. Otherwise, the menu will display tints without names (Tint 1, Tint 2, etc.), or the names will not correspond to the actual weapon tint.

# Commandline.txt

Commandline.txt allows you to add new categories to the menu, move weapons from one category to another, and set your own weapon prices. The file can be found in the AddonWeapons folder.

**Available commands at this time:**
- CreateWeaponCategory(Name)
- PutWeaponToCategory(ModelName, CategoryName)
- SetWeaponCost(ModelName, IntCost)

**Example:**  
CreateWeaponCategory(New Pistols)  
PutWeaponToCategory(WEAPON_FLAREGUN, New Pistols)  
SetWeaponCost(WEAPON_FLAREGUN, 666666)  

You can move weapons to one of the categories already existing in the mod, using one of the following parameters as the name:

- GROUP_HEAVY
- GROUP_MELEE
- GROUP_MG
- GROUP_PISTOL
- GROUP_RIFLE
- GROUP_SHOTGUN
- GROUP_SMG
- GROUP_SNIPER
- GROUP_STUNGUN
- GROUP_THROWN
- GROUP_DIGISCANNER
- GROUP_FIREEXTINGUISHER
- GROUP_HACKINGDEVICE
- GROUP_NIGHTVISION
- GROUP_PARACHUTE
- GROUP_PETROLCAN
- GROUP_TRANQILIZER
- GROUP_RUBBERGUN

**Example:**
PutWeaponToCategory(WEAPON_FLAREGUN, GROUP_MELEE)

This command will move the flare gun into the melee weapon category
