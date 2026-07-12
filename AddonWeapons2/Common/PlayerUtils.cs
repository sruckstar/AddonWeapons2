using GTA;

namespace AddonWeapons2.Common
{
    public static class PlayerUtils
    {
        private static readonly Model FreemodeMale = new Model("mp_m_freemode_01");
        private static readonly Model FreemodeFemale = new Model("mp_f_freemode_01");

        public static bool IsFreemodeCharacter()
        {
            var model = Game.Player.Character.Model;
            return model == FreemodeMale || model == FreemodeFemale;
        }

        public static bool CanAfford(int amount)
        {
            return Game.Player.Money >= amount || IsFreemodeCharacter();
        }
    }
}
