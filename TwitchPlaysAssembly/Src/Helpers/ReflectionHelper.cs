using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionHelper
{
	public static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName));

	public static Type FindType(string fullName, string assemblyName) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName) && t.Assembly.GetName().Name.Equals(assemblyName));

	public static IEnumerable<Type> GetSafeTypes(this Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.Where(x => x != null);
		}
		catch (Exception)
		{
			return new List<Type>();
		}
	}

	static readonly Dictionary<string, MemberInfo> MemberCache = new Dictionary<string, MemberInfo>();
	public static T GetCachedMember<T>(this Type type, string member) where T : MemberInfo
	{
		if (MemberCache.ContainsKey(member)) return (T) MemberCache[member];

		MemberInfo memberInfo = type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
		MemberCache[member] = memberInfo;

		return (T) memberInfo;
	}

	public static T GetValue<T>(this Type type, string member, object target = null) =>
		(T) (type.GetCachedMember<FieldInfo>(member)?.GetValue(target) ?? type.GetCachedMember<PropertyInfo>(member)?.GetValue(target, null));

	public static void SetValue<T>(this Type type, string member, object value, object target = null)
	{
		type.GetCachedMember<FieldInfo>(member)?.SetValue(target, value);
		type.GetCachedMember<PropertyInfo>(member)?.SetValue(target, value, null);
	}

	public static T CallMethod<T>(this Type type, string method, object target = null, params object[] arguments) =>
		(T) type.GetCachedMember<MethodInfo>(method)?.Invoke(target, arguments);

	public static T GetValue<T>(this object @object, string member) => @object.GetType().GetValue<T>(member, @object);
	public static void SetValue<T>(this object @object, string member, object value) => @object.GetType().SetValue<T>(member, @object, value);
	public static T CallMethod<T>(this object @object, string member, params object[] arguments) => @object.GetType().CallMethod<T>(member, @object, arguments);
}
