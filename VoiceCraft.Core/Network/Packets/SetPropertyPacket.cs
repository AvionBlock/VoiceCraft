using LiteNetLib.Utils;
using VoiceCraft.Core.Exceptions;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetPropertyPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetProperty;
        public byte Id { get; private set; }
        public string Key { get; private set; }
        public object? Value { get; private set; }

        public SetPropertyPacket(byte id = 0, string key = "", object? value = null)
        {
            Id = id;
            Key = key;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Key, Constants.MaxStringLength);
            
            switch (Value)
            {
                case byte byteValue:
                    writer.Put((byte)PropertyType.Byte);
                    writer.Put(byteValue);
                    break;
                case int intValue:
                    writer.Put((byte)PropertyType.Int);
                    writer.Put(intValue);
                    break;
                case uint uintValue:
                    writer.Put((byte)PropertyType.UInt);
                    writer.Put(uintValue);
                    break;
                case float floatValue:
                    writer.Put((byte)PropertyType.Float);
                    writer.Put(floatValue);
                    break;
                default:
                    throw new CorruptedPacketException();
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Key = reader.GetString(Constants.MaxStringLength);
            var propertyType = (PropertyType)reader.GetByte();

            switch (propertyType)
            {
                case PropertyType.Byte:
                    Value = reader.GetByte();
                    break;
                case PropertyType.Int:
                    Value = reader.GetInt();
                    break;
                case PropertyType.UInt:
                    Value = reader.GetUInt();
                    break;
                case PropertyType.Float:
                    Value = reader.GetFloat();
                    break;
                case PropertyType.Unknown:
                default:
                    throw new CorruptedPacketException();
            }
        }
    }
}