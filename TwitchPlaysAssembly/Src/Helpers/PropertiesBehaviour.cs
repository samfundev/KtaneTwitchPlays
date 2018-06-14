using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* 
 * This file originally came from Multiple Bombs, Written by Lupo511
 */

public class PropertiesBehaviour : MonoBehaviour, IDictionary<string, object>
{
	public class Property
	{
		public delegate object PropertyGetDelegate();

		private readonly PropertyGetDelegate _getDelegate;

		public delegate void PropertySetDelegate(object value);

		private readonly PropertySetDelegate _setDelegate;

		public Property(PropertyGetDelegate get, PropertySetDelegate set)
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

	private readonly Dictionary<string, Property> _properties;

	public PropertiesBehaviour()
	{
		_properties = new Dictionary<string, Property>();
	}

	// ReSharper disable once ParameterHidesMember
	public void AddProperty(string name, Property property)
	{
		_properties.Add(name, property);
	}

	public object this[string key]
	{
		get { return _properties[key].Get(); }
		set
		{
			if (!_properties.ContainsKey(key))
			{
				throw new NotImplementedException("You can't add items to this Dictionary.");
			}
			Property property = _properties[key];
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
			value = _properties[key].Get();
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
}
