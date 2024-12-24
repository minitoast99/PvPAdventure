using System.IO;

namespace PvPAdventure;

public interface IPacket<out TSelf>
{
    static abstract TSelf Deserialize(BinaryReader reader);
    void Serialize(BinaryWriter writer);
}