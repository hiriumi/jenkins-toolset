using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JenkinsLib
{
    public class ParameterConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var parameter = jsonObject.ToObject<ParameterDef>();

            var valueToken = jsonObject.SelectToken("defaultParameterValue.value");
            if (valueToken != null)
                parameter.Value = valueToken.ToObject<string>();
            return parameter;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ParameterDef);
        }
    }
}