using LemonUI.Menus;
using static AddonWeapons2.UI.Badges;

namespace AddonWeapons2.UI
{
    /// <summary>
    /// Provides badge sets for the UI, specifically for gun and tick icons.
    /// Implements the IBadgeProvider interface to offer standardized badge access.
    /// </summary>
    public class BadgeProvider : IBadgeProvider
    {
        private readonly BadgeSet _gunBadge;
        private readonly BadgeSet _tickBadge;

        /// <summary>
        /// Initializes a new instance of the BadgeProvider class.
        /// Creates the gun and tick badge sets using predefined icons from the commonmenu.
        /// </summary>
        public BadgeProvider()
        {
            _gunBadge = CreateBafgeFromItem("commonmenu", "shop_gunclub_icon_a", "commonmenu", "shop_gunclub_icon_b");
            _tickBadge = CreateBafgeFromItem("commonmenu", "shop_tick_icon", "commonmenu", "shop_tick_icon");
        }

        /// <summary>
        /// Gets the gun badge set.
        /// </summary>
        /// <returns>The BadgeSet containing gun-related icons.</returns>
        public BadgeSet GetGunBadge() => _gunBadge;

        /// <summary>
        /// Gets the tick badge set.
        /// </summary>
        /// <returns>The BadgeSet containing tick icons.</returns>
        public BadgeSet GetTickBadge() => _tickBadge;
    }
}