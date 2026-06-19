using LiteNetLib.Utils;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnEntityPropertyUpdatedPacket(int id, string key, object? value) : IMcApiEventPacket
{
    public McApiOnEntityPropertyUpdatedPacket() : this(0, string.Empty, null)
    {
    }

    public EventType EventType => EventType.OnEntityPropertyUpdated;
    public int Id { get; private set; } = id;
    public string Key { get; private set; } = string.Empty;
    public object? Value { get; private set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Key);
        switch (Value)
        {
            case bool v:
                writer.Put((byte)PropertyType.Boolean);
                writer.Put(v);
                break;
            case sbyte v:
                writer.Put((byte)PropertyType.SByte);
                writer.Put(v);
                break;
            case byte v:
                writer.Put((byte)PropertyType.Byte);
                writer.Put(v);
                break;
            case short v:
                writer.Put((byte)PropertyType.Short);
                writer.Put(v);
                break;
            case ushort v:
                writer.Put((byte)PropertyType.UShort);
                writer.Put(v);
                break;
            case int v:
                writer.Put((byte)PropertyType.Int);
                writer.Put(v);
                break;
            case uint v:
                writer.Put((byte)PropertyType.UInt);
                writer.Put(v);
                break;
            case long v:
                writer.Put((byte)PropertyType.Long);
                writer.Put(v);
                break;
            case ulong v:
                writer.Put((byte)PropertyType.ULong);
                writer.Put(v);
                break;
            case float v:
                writer.Put((byte)PropertyType.Float);
                writer.Put(v);
                break;
            case double v:
                writer.Put((byte)PropertyType.Double);
                writer.Put(v);
                break;
            default:
                writer.Put((byte)PropertyType.Null);
                break;
        }
    }
    
    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Key = reader.GetString();
        var propertyType = (PropertyType)reader.GetByte();
        Value = propertyType switch
        {
            PropertyType.Boolean => reader.GetBool(),
            PropertyType.SByte => reader.GetSByte(),
            PropertyType.Byte => reader.GetByte(),
            PropertyType.Short => reader.GetShort(),
            PropertyType.UShort => reader.GetUShort(),
            PropertyType.Int => reader.GetInt(),
            PropertyType.UInt => reader.GetUInt(),
            PropertyType.Long => reader.GetLong(),
            PropertyType.ULong => reader.GetULong(),
            PropertyType.Float => reader.GetFloat(),
            PropertyType.Double => reader.GetDouble(),
            _ => null
        };
    }

    public McApiOnEntityPropertyUpdatedPacket Set(int id = 0, string key = "", object? value = null)
    {
        Id = id;
        Key = key;
        Value = value;
        return this;
    }
}