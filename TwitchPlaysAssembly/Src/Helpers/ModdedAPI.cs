using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Linq;

static class ModdedAPI
{
	private static IDictionary<string, object> sharedAPI;
	public static IDictionary<string, object> API
	{
		get
		{
			EnsureSharedAPI();

			return sharedAPI;
		}
	}

	private static Type propertyType;

	public static object AddProperty(string name, Func<object> get, Action<object> set)
	{
		var sharedAPIType = API.GetType();
		var addPropertyMethod = sharedAPIType.GetMethod("AddProperty", BindingFlags.Public | BindingFlags.Instance);
		return addPropertyMethod.Invoke(sharedAPI, new object[] { name, get, set });
	}

	public static void SetEnabled(object property, bool enabled)
	{
		propertyType.GetField("Enabled").SetValue(property, enabled);
	}

	public static bool TryGetAs<T>(string name, out T value)
	{
		if (API.TryGetValue(name, out object objectValue) && objectValue is T)
		{
			value = (T) objectValue;
			return true;
		}

		value = default(T);
		return false;
	}

	private static void EnsureSharedAPI()
	{
		if (sharedAPI != null)
			return;

		var apiObject = GameObject.Find("ModdedAPI_Info");
		if (apiObject == null)
		{
			apiObject = new GameObject("ModdedAPI_Info", typeof(ModdedAPIBehaviour));
		}

		sharedAPI = apiObject.GetComponent<IDictionary<string, object>>();
		propertyType = sharedAPI.GetType().GetNestedType("Property");
	}
}

// This class originally came from Multiple Bombs, written by Lupo511. Modified a bit from the original.
public class ModdedAPIBehaviour : MonoBehaviour, IDictionary<string, object>
{
	public class Property
	{
		private readonly Func<object> _getDelegate;

		private readonly Action<object> _setDelegate;

		public bool Enabled = true;

		public Property(Func<object> get, Action<object> set)
		{
			_getDelegate = get;
			_setDelegate = set;
		}

		public object Get()
		{
			return _getDelegate();
		}

		public bool CanSet()
		{
			return _setDelegate != null;
		}

		public void Set(object value)
		{
			_setDelegate(value);
		}
	}

	private readonly Dictionary<string, List<Property>> _properties;

	public ModdedAPIBehaviour()
	{
		_properties = new Dictionary<string, List<Property>>();
	}

	public Property AddProperty(string name, Func<object> get, Action<object> set)
	{
		if (!_properties.TryGetValue(name, out List<Property> subproperties))
		{
			subproperties = new List<Property>();
			_properties.Add(name, subproperties);
		}

		var property = new Property(get, set);
		subproperties.Add(property);
		return property;
	}

	public object this[string key]
	{
		get { return GetEnabledProperty(key).Get(); }
		set
		{
			if (!_properties.ContainsKey(key))
			{
				throw new Exception("You can't add items to this Dictionary.");
			}
			Property property = GetEnabledProperty(key);
			if (!property.CanSet())
			{
				throw new Exception("The key \"" + key + "\" cannot be set (it is read-only).");
			}
			property.Set(value);
		}
	}

	public int Count => _properties.Count;

	public bool IsReadOnly => false;

	public ICollection<string> Keys => _properties.Keys.ToList();

	public ICollection<object> Values
	{
		get { throw new NotSupportedException("The Values property is not supported in this Dictionary."); }
	}

	public void Add(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("You can't add items to this Dictionary.");
	}

	public void Add(string key, object value)
	{
		throw new NotSupportedException("You can't add items to this Dictionary.");
	}

	public void Clear()
	{
		throw new NotSupportedException("You can't clear this Dictionary.");
	}

	public bool Contains(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("The Contains method is not supported in this Dictionary.");
	}

	public bool ContainsKey(string key)
	{
		return _properties.ContainsKey(key);
	}

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new NotSupportedException("The CopyTo method is not supported in this Dictionary.");
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		throw new NotSupportedException("The GetEnumerator method is not supported in this Dictionary.");
	}

	public bool Remove(KeyValuePair<string, object> item)
	{
		throw new NotSupportedException("The Remove method is not supported in this Dictionary.");
	}

	public bool Remove(string key)
	{
		throw new NotSupportedException("The Remove method is not supported in this Dictionary.");
	}

	public bool TryGetValue(string key, out object value)
	{
		bool result;
		try
		{
			value = GetEnabledProperty(key).Get();
			result = true;
		}
		catch
		{
			value = null;
			result = false;
		}
		return result;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotSupportedException("The GetEnumerator method is not supported in this Dictionary.");
	}

	private Property GetEnabledProperty(string name)
	{
		return _properties[name].Find(property => property.Enabled);
	}
}
