using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionHelper
{
	public static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => t.FullName?.Equals(fullName) == true);

	public static Type FindType(string fullName, string assemblyName) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => t.FullName?.Equals(fullName) == true && t.Assembly.GetName().Name.Equals(assemblyName));

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
		if (MemberCache.ContainsKey(member)) return MemberCache[member] is T cachedCastedInfo ? cachedCastedInfo : null;

		MemberInfo memberInfo = type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
		MemberCache[member] = memberInfo;

		return memberInfo is T castedInfo ? castedInfo : null;
	}

	public static T GetValue<T>(this Type type, string member, object target = null) =>
		(T) ((type.GetCachedMember<FieldInfo>(member)?.GetValue(target)) ?? (type.GetCachedMember<PropertyInfo>(member)?.GetValue(target, null)));

	public static void SetValue(this Type type, string member, object value, object target = null)
	{
		type.GetCachedMember<FieldInfo>(member)?.SetValue(target, value);
		type.GetCachedMember<PropertyInfo>(member)?.SetValue(target, value, null);
	}

	public static T CallMethod<T>(this Type type, string method, object target = null, params object[] arguments) => (T) (type.GetCachedMember<MethodInfo>(method)?.Invoke(target, arguments));

	public static void CallMethod(this Type type, string method, object target = null, params object[] arguments) => type.GetCachedMember<MethodInfo>(method)?.Invoke(target, arguments);

	public static T GetValue<T>(this object @object, string member) => @object.GetType().GetValue<T>(member, @object);
	public static void SetValue(this object @object, string member, object value) => @object.GetType().SetValue(member, value, @object);
	public static T CallMethod<T>(this object @object, string member, params object[] arguments) => @object.GetType().CallMethod<T>(member, @object, arguments);
	public static void CallMethod(this object @object, string member, params object[] arguments) => @object.GetType().CallMethod(member, @object, arguments);
}
