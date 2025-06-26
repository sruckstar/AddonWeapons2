using GTA;
using GTA.Math;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using AddonWeapons2.UI;
using AddonWeapons2.Weapons;
using System;

namespace AddonWeapons2
{
    public class AddonWeapons : Script
    {
        
        private ScriptSettings config_settings;
        private Keys menuOpenKey;
        private bool _weaponsLoaded = false;


        public AddonWeapons()
        {
            Tick += OnTick;
            KeyUp += onkeyup;
            Aborted += OnAborted;

            config_settings = ScriptSettings.Load($"Scripts\\AddonWeapons\\settings.ini");
            menuOpenKey = config_settings.GetValue<Keys>("MENU", "MenuOpenKey", Keys.None);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_weaponsLoaded)
            {
                WeaponMenu.SetLanguage();
                CategoriesManager.InitializeCategories();
                WeaponMenu.Initialize();
                WeaponInfo.LoadDlcWeaponModels();
                Items.Initialize();
                WeaponMenu.BuildMainMenu();
                AmmoShopManager.Initialize();
                _weaponsLoaded = true;
            }

            WeaponMenu.Pool.Process();
            AmmoShopManager.UpdateAmmoBoxes();
            WeaponSave.WaitForLoadedInventory();
            //WeaponManager.SetCurrentWeapon();
            WeaponMenu.ReOpenLastMenu();
            //WeaponManager.UpdatePlayerWeapon();
        }

        private void onkeyup(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == menuOpenKey)
            {
                AmmoShopManager.OpenWeaponMenu();
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            AmmoShopManager.Cleanup();
        }
    }
}
