using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;

class SettingsConverter
{
	public static string Serialize(object obj)
	{
		return JsonConvert.SerializeObject(obj, Formatting.Indented, new ColorConverter());
	}

	public static T Deserialize<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json, new ColorConverter());
	}
}

class ColorConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		Color color = (Color) value;
		if (color.a == 1) writer.WriteValue(string.Format("{0}, {1}, {2}", color.r, color.g, color.b));
		else writer.WriteValue(string.Format("{0}, {1}, {2}, {3}", color.r, color.g, color.b, color.a));
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		try
		{
			var values = ((string) reader.Value).Split(',').Select(x => float.Parse(x.Trim())).ToArray();
			switch (values.Count())
			{
				case 3:
					return new Color(values[0], values[1], values[2]);
				case 4:
					return new Color(values[0], values[1], values[2], values[3]);
				default:
					return null;
			}
		}
		catch
		{
			return null;
		}
	}

	public override bool CanConvert(Type objectType)
	{
		return typeof(Color) == objectType;
	}
}