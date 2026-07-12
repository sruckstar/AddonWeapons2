using AddonWeapons2.UI;
using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.IO;
using static AddonWeapons2.Weapons.WeaponDataUtils;

namespace AddonWeapons2.Weapons
{
    public static class AmmoShopManager
    {
        #region Constants
        private const string AmmoBoxesFile = "Scripts\\AddonWeapons\\AmmoBoxes.txt";

        private const float SpawnDistance = 10f;
        private const float DespawnDistance = 15f;
        private const float InteractionDistance = 1.5f;

        private const int AmmoBoxModelHash = 2107849419;
        private const int FallbackAmmoBoxModelHash = 1580014892;

        private const int ModelRequestTimeoutMs = 1000;
        private const int ModelRetryCooldownMs = 5000;
        #endregion

        #region Model state
        private enum ModelState { Unknown, Primary, Fallback, Failed }

        private static ModelState _modelState = ModelState.Unknown;
        private static Model _activeModel = new Model(AmmoBoxModelHash);

        private static int _nextModelRetryTime = 0;
        private static bool _fallbackNotified = false;
        private static bool _modelFailureLogged = false;
        #endregion

        #region Properties
        public static List<Vector3> DefaultAmmoBoxPositions { get; } = new List<Vector3>
        {
            new Vector3(19.04f, -1103.96f, 29.24f),
            new Vector3(814.0817f, -2159.347f, 29.04f),
            new Vector3(1691.051f, 3756.589f, 34.14f),
            new Vector3(253.34f, -45.93f, 69.27754f),
            new Vector3(846.4512f, -1033.26f, 27.63f),
            new Vector3(-333.0135f, 6080.67f, 30.89f),
            new Vector3(-666.3039f, -935.6205f, 21.26f),
            new Vector3(-1305.119f, -390.2967f, 36.12f),
            new Vector3(-1120.622f, 2695.518f, 17.99f),
            new Vector3(-3173.297f, 1083.793f, 20.28f),
            new Vector3(2572.105f, 294.635f, 108.17f)
        };

        public static List<float> DefaultAmmoBoxHeadings { get; } = new List<float>
        {
            -18.99999f, 0f, 46.9999f, 70.99976f, 0f,
            44.99992f, 0f, 74.99975f, 40.99995f, 64.9998f, 0f
        };

        public static List<Vector3> AmmoBoxPositions { get; private set; } = new List<Vector3>();
        public static List<float> AmmoBoxHeadings { get; private set; } = new List<float>();
        public static List<Prop> AmmoBoxProps { get; } = new List<Prop>();

        public static Model AmmoBoxModel => _activeModel;
        #endregion

        #region Initialization
        public static void Initialize()
        {
            LoadAmmoBoxLocations();
            EnsureModelReady();
            SyncHeadingsWithPositions();
        }

        private static void LoadAmmoBoxLocations()
        {
            AmmoBoxPositions = new List<Vector3>();
            AmmoBoxHeadings = new List<float>();

            if (!File.Exists(AmmoBoxesFile))
            {
                UseDefaultLocations();
                Log($"'{AmmoBoxesFile}' not found — using {AmmoBoxPositions.Count} default positions.");
                return;
            }

            int parsed = 0;
            int skipped = 0;

            foreach (var raw in File.ReadAllLines(AmmoBoxesFile))
            {
                var line = raw?.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                var (position, heading) = ParsePositionAndHeading(line);
                if (position.HasValue && heading.HasValue)
                {
                    AmmoBoxPositions.Add(position.Value);
                    AmmoBoxHeadings.Add(heading.Value);
                    parsed++;
                }
                else
                {
                    skipped++;
                    Log($"Failed to parse line in '{AmmoBoxesFile}': \"{line}\" " +
                        "(expected format: X, Y, Z, Heading separated by commas with dot as decimal separator).");
                }
            }

            if (parsed == 0)
            {
                Log($"No valid positions found in '{AmmoBoxesFile}' (skipped: {skipped}). Falling back to defaults.");
                UseDefaultLocations();
            }
            else if (skipped > 0)
            {
                Log($"Loaded {parsed} positions, skipped {skipped} invalid lines.");
            }
            else
            {
                Log($"Loaded {parsed} ammo box positions from '{AmmoBoxesFile}'.");
            }
        }

        private static void UseDefaultLocations()
        {
            AmmoBoxPositions = new List<Vector3>(DefaultAmmoBoxPositions);
            AmmoBoxHeadings = new List<float>(DefaultAmmoBoxHeadings);
        }

        private static void SyncHeadingsWithPositions()
        {
            while (AmmoBoxHeadings.Count < AmmoBoxPositions.Count)
                AmmoBoxHeadings.Add(0f);
        }
        #endregion

        #region Model loading
        private static bool EnsureModelReady()
        {
            if (_modelState == ModelState.Primary && _activeModel.IsLoaded)
                return true;

            bool cooldownActive = Game.GameTime < _nextModelRetryTime;

            if (!cooldownActive || _modelState == ModelState.Unknown)
            {
                bool blocking = _modelState != ModelState.Fallback;
                if (TryAcquirePrimaryModel(blocking, out var primary))
                {
                    if (_modelState == ModelState.Fallback)
                    {
                        Log("Primary ammo box model now available — switching.");
                        ReleaseActiveModel();
                    }
                    _activeModel = primary;
                    _modelState = ModelState.Primary;
                    return true;
                }
                _nextModelRetryTime = Game.GameTime + ModelRetryCooldownMs;
            }

            if (_modelState == ModelState.Fallback && _activeModel.IsLoaded)
                return true;

            if (TryPrepareModel(FallbackAmmoBoxModelHash, out var fallback))
            {
                _activeModel = fallback;
                _modelState = ModelState.Fallback;
                if (!_fallbackNotified)
                {
                    Log("Primary model unavailable, using fallback. Will switch automatically when primary loads.");
                    _fallbackNotified = true;
                }
                return true;
            }

            _modelState = ModelState.Failed;
            _nextModelRetryTime = Game.GameTime + ModelRetryCooldownMs;
            if (!_modelFailureLogged)
            {
                Log("Failed to load primary or fallback ammo box model. Will retry.");
                _modelFailureLogged = true;
            }
            return false;
        }

        private static bool TryAcquirePrimaryModel(bool blocking, out Model model)
        {
            model = new Model(AmmoBoxModelHash);

            if (!model.IsValid || !model.IsInCdImage)
                return false;

            if (model.IsLoaded)
                return true;

            if (blocking)
                return model.Request(ModelRequestTimeoutMs);

            model.Request();
            return false;
        }

        private static bool TryPrepareModel(int hash, out Model model)
        {
            model = new Model(hash);

            if (!model.IsValid || !model.IsInCdImage)
                return false;

            if (model.IsLoaded)
                return true;

            return model.Request(ModelRequestTimeoutMs);
        }

        private static void ReleaseActiveModel()
        {
            if (_activeModel.IsValid)
                _activeModel.MarkAsNoLongerNeeded();
        }
        #endregion

        #region Ammo Box Management
        public static void UpdateAmmoBoxes()
        {
            EnsureAmmoBoxListCapacity();

            var playerPos = Game.Player.Character.Position;

            for (int i = 0; i < AmmoBoxPositions.Count; i++)
            {
                var position = AmmoBoxPositions[i];
                float distanceToPlayer = playerPos.DistanceTo(position);

                if (distanceToPlayer < SpawnDistance)
                {
                    CreateAmmoBoxIfNeeded(i);
                }
                else if (distanceToPlayer > DespawnDistance)
                {
                    DeleteAmmoBoxIfExists(i);
                }

                HandleAmmoBoxInteraction(i);
            }
        }

        private static void EnsureAmmoBoxListCapacity()
        {
            while (AmmoBoxProps.Count < AmmoBoxPositions.Count)
                AmmoBoxProps.Add(null);
        }

        private static void CreateAmmoBoxIfNeeded(int index)
        {
            if (AmmoBoxProps[index] != null && AmmoBoxProps[index].Exists())
                return;

            if (!EnsureModelReady())
                return;

            var prop = World.CreateProp(
                _activeModel,
                AmmoBoxPositions[index],
                new Vector3(0f, 0f, AmmoBoxHeadings[index]),
                dynamic: false,
                placeOnGround: false
            );

            if (prop == null)
            {
                Log($"World.CreateProp returned null for ammo box #{index} at {AmmoBoxPositions[index]}.");
                return;
            }

            AmmoBoxProps[index] = prop;
            Function.Call(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, prop);
            Function.Call(Hash.FREEZE_ENTITY_POSITION, prop, true);
        }

        private static void DeleteAmmoBoxIfExists(int index)
        {
            var box = AmmoBoxProps[index];
            if (box != null && box.Exists())
            {
                box.Delete();
                AmmoBoxProps[index] = null;
            }
        }

        private static void HandleAmmoBoxInteraction(int index)
        {
            var box = AmmoBoxProps[index];
            if (box == null || !box.Exists()) return;

            float distance = Game.Player.Character.Position.DistanceTo(box.Position);
            if (distance >= InteractionDistance) return;

            ShowInteractionPrompt();

            if (Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 51)) // INPUT_CONTEXT
                OpenWeaponMenu();
        }

        private static void ShowInteractionPrompt()
        {
            if (WeaponMenu.IsMenuOpen()) return;

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, WeaponMenu.HelpMessage);
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_KEYBOARD_DISPLAY, "~INPUT_CONTEXT~");
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, 0, 1, -1);
        }

        public static void OpenWeaponMenu()
        {
            WeaponMenu.RefreshAllMenus();
            WeaponMenu.MainMenu.Visible = true;
        }
        #endregion

        #region Utility Functions
        public static (Vector3 Position, float Heading)? GetNearestAmmoBox()
        {
            Vector3 playerPos = Game.Player.Character.Position;
            (Vector3 Position, float Heading)? nearestBox = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < AmmoBoxProps.Count; i++)
            {
                var box = AmmoBoxProps[i];
                if (box == null || !box.Exists()) continue;

                float distance = playerPos.DistanceTo(box.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    float heading = i < AmmoBoxHeadings.Count ? AmmoBoxHeadings[i] : 0f;
                    nearestBox = (box.Position, heading);
                }
            }

            return nearestBox;
        }
        #endregion

        #region Logging
        private static void Log(string message) => Common.FileLogger.Write("AmmoShop", message);
        #endregion

        #region Cleanup
        public static void Cleanup()
        {
            foreach (var box in AmmoBoxProps)
            {
                if (box != null && box.Exists())
                    box.Delete();
            }
            AmmoBoxProps.Clear();

            ReleaseActiveModel();

            _modelState = ModelState.Unknown;
            _nextModelRetryTime = 0;
            _fallbackNotified = false;
            _modelFailureLogged = false;
        }
        #endregion
    }
}
