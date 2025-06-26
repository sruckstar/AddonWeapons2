using LemonUI.Menus;

namespace AddonWeapons2.UI
{
    /// <summary>
    /// Provides static methods for creating badge sets used in the UI.
    /// </summary>
    public static class Badges
    {
        /// <summary>
        /// Creates a new BadgeSet using the specified texture library and texture names.
        /// </summary>
        /// <param name="library">The texture dictionary name where the badge textures are stored.</param>
        /// <param name="normal">The texture name for the normal state of the badge.</param>
        /// <param name="selected">The texture name for the selected state of the badge.</param>
        /// <param name="hovered">The texture name for the hovered state of the badge.</param>
        /// <returns>A new BadgeSet configured with the specified textures.</returns>
        public static BadgeSet CreateBafgeFromItem(string library, string normal, string selected, string hovered)
        {
            return new BadgeSet(library, normal, selected, hovered);
        }
    }
}