using GTA;
using GTA.Native;
using System;
using System.Windows.Forms;
using AddonWeapons2.Common;
using AddonWeapons2.UI;
using AddonWeapons2.Weapons;

namespace AddonWeapons2
{
    public class AddonWeapons : Script
    {
        private const int HeartbeatEveryTicks = 120; // ~2 sec at 60 fps

        private ScriptSettings config_settings;
        private Keys menuOpenKey;

        private bool _weaponsLoaded = false;
        private bool _initFailureLogged = false;

        private int _tickCount = 0;
        private int _ticksSinceHeartbeat = 0;

        public AddonWeapons()
        {
            Tick += OnTick;
            KeyUp += onkeyup;
            Aborted += OnAborted;

            config_settings = ScriptSettings.Load("Scripts\\AddonWeapons\\settings.ini");
            menuOpenKey = config_settings.GetValue<Keys>("MENU", "MenuOpenKey", Keys.None);

            Log("=== AddonWeapons script constructed ===");
        }

        private void OnTick(object sender, EventArgs e)
        {
            _tickCount++;
            LogHeartbeatIfDue();

            if (!_weaponsLoaded)
            {
                if (!IsPlayerReady())
                    return;

                RunOneTimeInitialization();
                _weaponsLoaded = true;
            }

            SafeRun("Pool.Process",           () => WeaponMenu.Pool.Process());
            SafeRun("UpdateWeaponPreview",     Items.UpdateWeaponPreview);
            SafeRun("UpdatePreviewCamera",     UpdatePreviewCamera);
            SafeRun("UpdateAmmoBoxes",         AmmoShopManager.UpdateAmmoBoxes);
            SafeRun("WaitForLoadedInventory",  WeaponSave.WaitForLoadedInventory);
            SafeRun("ReOpenLastMenu",          WeaponMenu.ReOpenLastMenu);
        }

        private static bool IsPlayerReady()
        {
            var ped = Game.Player?.Character;
            if (ped == null || !ped.Exists())
                return false;

            if (!Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player))
                return false;

            var pos = ped.Position;
            if (pos.X == 0f && pos.Y == 0f && pos.Z == 0f)
                return false;

            return true;
        }

        private static void UpdatePreviewCamera()
        {
            if (!WeaponMenu.Pool.AreAnyVisible && WeaponPreviewCamera.IsActive)
                WeaponPreviewCamera.Reset();
        }

        private void LogHeartbeatIfDue()
        {
            if (++_ticksSinceHeartbeat < HeartbeatEveryTicks)
                return;
            _ticksSinceHeartbeat = 0;

            bool loading;
            bool charNull;
            bool charExists;
            try { loading = Game.IsLoading; } catch { loading = true; }
            try
            {
                var ped = Game.Player?.Character;
                charNull = ped == null;
                charExists = ped != null && ped.Exists();
            }
            catch { charNull = true; charExists = false; }

            Log($"heartbeat tick#{_tickCount} inited={_weaponsLoaded} " +
                $"IsLoading={loading} CharNull={charNull} CharExists={charExists}");
        }

        private void RunOneTimeInitialization()
        {
            Log("--- one-time init: start ---");
            SafeRun("WeaponMenu.SetLanguage",           WeaponMenu.SetLanguage);
            SafeRun("CategoriesManager.Init",           CategoriesManager.InitializeCategories);
            SafeRun("WeaponMenu.Initialize",            WeaponMenu.Initialize);
            SafeRun("WeaponInfo.LoadDlcWeaponModels",   WeaponInfo.LoadDlcWeaponModels);
            SafeRun("Items.Initialize",                 Items.Initialize);
            SafeRun("WeaponMenu.BuildMainMenu",         WeaponMenu.BuildMainMenu);
            SafeRun("AmmoShopManager.Initialize",       AmmoShopManager.Initialize);
            Log("=== One-time initialization finished ===");
        }

        private void SafeRun(string stage, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Stage '{stage}' failed: {ex}");

                if (!_initFailureLogged)
                {
                    GTA.UI.Notification.Show($"~r~AddonWeapons: error in '{stage}'. See AddonWeapons.log");
                    _initFailureLogged = true;
                }
            }
        }

        private void onkeyup(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == menuOpenKey)
                SafeRun("OpenWeaponMenu", AmmoShopManager.OpenWeaponMenu);
        }

        private void OnAborted(object sender, EventArgs e)
        {
            SafeRun("PreviewCamera.Reset", WeaponPreviewCamera.Reset);
            SafeRun("Cleanup", AmmoShopManager.Cleanup);
        }

        private void Log(string message) => FileLogger.Write("Main", message);
    }
}
