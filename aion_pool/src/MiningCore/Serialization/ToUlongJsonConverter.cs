using System;
using System.Globalization;
using Newtonsoft.Json;

namespace MiningCore.Serialization
{
  public class ToUlongJsonConverter : JsonConverter
  {
    public override bool CanConvert(Type objectType)
    {
      return typeof(ulong) == objectType || typeof(string) == objectType;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (value == null) {
        writer.WriteValue("null");
      } else {
        writer.WriteValue($"{value}");
      }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      var str = (string) reader.Value;
      if (string.IsNullOrEmpty(str)) {
        return default(ulong);
      }
      ulong val;
      if (str.StartsWith("0x")) {
        val = ulong.Parse("0" + str, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
      } else {
        val = ulong.Parse("0" + str, CultureInfo.InvariantCulture);
      }
      return Convert.ChangeType(val, typeof(ulong));
    }
  }
}