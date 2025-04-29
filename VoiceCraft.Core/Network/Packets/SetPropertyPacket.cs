using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetPropertyPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetProperty;
        
        public int Id { get; private set; }
        public PropertyKey Key { get; private set; }
        public object? Value { get; private set; }

        public SetPropertyPacket(int id = 0, PropertyKey key = PropertyKey.Unknown, object? value = null)
        {
            Id = id;
            Key = key;
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((ushort)Key);
            
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
                case null:
                    writer.Put((byte)PropertyType.Null);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            var propertyKeyValue = reader.GetUShort();
            Key = Enum.IsDefined(typeof(PropertyKey), propertyKeyValue) ? (PropertyKey)propertyKeyValue : PropertyKey.Unknown;
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
                case PropertyType.Null:
                default:
                    Value = null;
                    break;
            }
        }
    }
}