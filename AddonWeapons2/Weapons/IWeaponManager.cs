using System;
using System.Collections.Generic;
using static AddonWeapons2.Weapons.WeaponInfo;

namespace AddonWeapons2.Weapons
{
    /// <summary>
    /// Интерфейс для управления оружием и его компонентами
    /// </summary>
    public interface IWeaponManager
    {
        List<uint> GetComponentsList(DlcWeaponDataWithComponents weapon);
        List<int> GetComponentsCost(DlcWeaponDataWithComponents weapon);
        bool IsMaxAmmo(uint weaponHash);
        string[] ExtractParameters(string command, string commandName, int paramCount);
        bool HasComponentsAvailable(uint weaponHash);
        uint GetWeaponTypeGroup(uint weaponHash);
        bool UsesAmmo(uint weaponHash);
        bool IsMeleeOrThrown(uint weaponHash);
        void SerializeDictionary<T1, T2, T3>(string filePath, Dictionary<T1, Dictionary<T2, List<T3>>> dictionary);
        Dictionary<T1, Dictionary<T2, List<T3>>> DeserializeDictionary<T1, T2, T3>(string filePath);
    }
}