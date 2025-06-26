using LemonUI.Menus;

namespace AddonWeapons2.UI
{
    public interface IBadgeProvider
    {
        BadgeSet GetGunBadge();
        BadgeSet GetTickBadge();
    }
}