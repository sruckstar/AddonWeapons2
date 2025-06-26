using AddonWeapons2.UI;
using GTA;
using GTA.Math;
using GTA.Native;
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
        public static Model AmmoBoxModel { get; } = new Model(AmmoBoxModelHash);
        #endregion

        #region Initialization
        public static void Initialize()
        {
            LoadAmmoBoxLocations();
        }

        private static void LoadAmmoBoxLocations()
        {
            if (File.Exists(AmmoBoxesFile))
            {
                var lines = File.ReadAllLines(AmmoBoxesFile);
                foreach (var line in lines)
                {
                    var (position, heading) = ParsePositionAndHeading(line);
                    if (position.HasValue && heading.HasValue)
                    {
                        AmmoBoxPositions.Add(position.Value);
                        AmmoBoxHeadings.Add(heading.Value);
                    }
                }
            }
            else
            {
                AmmoBoxPositions = new List<Vector3>(DefaultAmmoBoxPositions);
                AmmoBoxHeadings = new List<float>(DefaultAmmoBoxHeadings);
            }
        }
        #endregion

        #region Ammo Box Management
        public static void UpdateAmmoBoxes()
        {
            EnsureAmmoBoxListCapacity();

            for (int i = 0; i < AmmoBoxPositions.Count; i++)
            {
                var position = AmmoBoxPositions[i];
                var distanceToPlayer = Game.Player.Character.Position.DistanceTo(position);

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
            {
                AmmoBoxProps.Add(null);
            }
        }

        private static void CreateAmmoBoxIfNeeded(int index)
        {
            if (AmmoBoxProps[index] == null || !AmmoBoxProps[index].Exists())
            {
                AmmoBoxProps[index] = World.CreateProp(
                    AmmoBoxModel,
                    AmmoBoxPositions[index],
                    new Vector3(0, 0, AmmoBoxHeadings[index]),
                    false,
                    false
                );

                if (AmmoBoxProps[index] != null)
                {
                    Function.Call(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, AmmoBoxProps[index]);
                    Function.Call(Hash.FREEZE_ENTITY_POSITION, AmmoBoxProps[index], true);
                }
            }
        }

        private static void DeleteAmmoBoxIfExists(int index)
        {
            if (AmmoBoxProps[index] != null && AmmoBoxProps[index].Exists())
            {
                AmmoBoxProps[index].Delete();
                AmmoBoxProps[index] = null;
            }
        }

        private static void HandleAmmoBoxInteraction(int index)
        {
            var box = AmmoBoxProps[index];
            if (box == null || !box.Exists()) return;

            float distance = Game.Player.Character.Position.DistanceTo(box.Position);
            if (distance < InteractionDistance)
            {
                ShowInteractionPrompt();

                if (Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, 51)) // INPUT_CONTEXT
                {
                    OpenWeaponMenu();
                }
            }
        }

        private static void ShowInteractionPrompt()
        {
            if (!WeaponMenu.IsMenuOpen())
            {
                Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, WeaponMenu.HelpMessage);
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_KEYBOARD_DISPLAY, "~INPUT_CONTEXT~");
                Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, 0, 1, -1);
            }
        }

        public static void OpenWeaponMenu()
        {
            WeaponMenu.RefreshAllMenus();
            WeaponMenu.MainMenu.Visible = true;
        }
        #endregion

        #region Utility Functions
        /// <summary>
        /// Возвращает позицию и угол поворота ближайшей к игроку созданной коробки с оружием.
        /// Если коробок нет или они не созданы, возвращает null.
        /// </summary>
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
                    nearestBox = (box.Position, AmmoBoxHeadings[i]);
                }
            }

            return nearestBox;
        }
        #endregion

        #region Cleanup
        public static void Cleanup()
        {
            foreach (var box in AmmoBoxProps)
            {
                if (box != null && box.Exists())
                {
                    box.Delete();
                }
            }
            AmmoBoxProps.Clear();
        }
        #endregion
    }
}