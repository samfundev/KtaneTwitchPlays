using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static class SettingsConverter
{
	public static string Serialize(object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented, new ColorConverter());

	public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, new ColorConverter());
}

internal class ColorConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		Color color = (Color) value;
		string format = $"{(int) (color.r * 255)}, {(int) (color.g * 255)}, {(int) (color.b * 255)}";
		if (color.a != 1) format += ", " + (int) (color.a * 255);

		writer.WriteValue(format);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		IEnumerable<int?> parts = ((string) reader.Value).Split(',').Select(str => str.Trim().TryParseInt());
		if (parts.Any(x => x == null)) return existingValue;

		float[] values = parts.Select(i => (int) i / 255f).ToArray();
		switch (values.Length)
		{
			case 3:
				return new Color(values[0], values[1], values[2]);
			case 4:
				return new Color(values[0], values[1], values[2], values[3]);
			default:
				return existingValue;
		}
	}

	public override bool CanConvert(Type objectType) => typeof(Color) == objectType;
}
