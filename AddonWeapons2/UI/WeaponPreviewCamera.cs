using GTA;
using GTA.Math;

namespace AddonWeapons2.UI
{
    public static class WeaponPreviewCamera
    {
        private const float CameraDistance = 1.7f;
        private const float CameraHeightOffset = 0.15f;
        private const float CameraFov = 40f;

        private static Camera _camera;
        private static bool _playerHidden;

        public static bool IsActive => _camera != null && _camera.Exists();

        public static void EnsureActive(Vector3 target)
        {
            Vector3 toPlayer = Game.Player.Character.Position - target;
            toPlayer.Z = 0f;
            if (toPlayer.Length() < 0.01f)
                toPlayer = new Vector3(0f, 1f, 0f);
            toPlayer.Normalize();

            Vector3 camPos = target + toPlayer * CameraDistance
                             + new Vector3(0f, 0f, CameraHeightOffset);

            if (!IsActive)
            {
                _camera = World.CreateCamera(camPos, Vector3.Zero, CameraFov);
                World.RenderingCamera = _camera;
            }
            else
            {
                _camera.Position = camPos;
            }

            _camera.PointAt(target);

            if (!_playerHidden)
            {
                Game.Player.Character.IsVisible = false;
                _playerHidden = true;
            }
        }

        public static void Reset()
        {
            if (_playerHidden)
            {
                Game.Player.Character.IsVisible = true;
                _playerHidden = false;
            }

            if (_camera == null)
                return;

            World.RenderingCamera = null;
            if (_camera.Exists())
                _camera.Delete();
            _camera = null;
        }
    }
}
