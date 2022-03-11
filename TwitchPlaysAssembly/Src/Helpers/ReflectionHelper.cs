using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ReflectionHelper
{
	public static Type FindType(string fullName)
	{
		return AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName));
	}

	public static Type FindType(string fullName, string assemblyName)
	{
		return AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName) && t.Assembly.GetName().Name.Equals(assemblyName));
	}

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
#pragma warning disable CA1031
		catch (Exception)
		{
			return new List<Type>();
		}
#pragma warning restore CA1031
	}

	public static IEnumerable<FieldInfo> GetAllFields(this Type t, BindingFlags bindingFlags)
	{
		if (t == null)
			return Enumerable.Empty<FieldInfo>();

		BindingFlags flags = bindingFlags |
							 BindingFlags.DeclaredOnly;
		return t.GetFields(flags).Concat(GetAllFields(t.BaseType, bindingFlags));
	}

	public static IEnumerable<FieldInfo> GetAllFields(this object obj, BindingFlags bindingFlags)
	{
		return GetAllFields(obj.GetType(), bindingFlags);
	}

	public static FieldInfo GetDeepField(this Type t, string fieldName, BindingFlags bindingFlags)
	{
		return GetAllFields(t, bindingFlags).FirstOrDefault(field => field.Name == fieldName);
	}

	public static FieldInfo GetDeepField(this object obj, string fieldName, BindingFlags bindingFlags)
	{
		return GetDeepField(obj.GetType(), fieldName, bindingFlags);
	}

	static readonly Dictionary<string, MemberInfo> MemberCache = new Dictionary<string, MemberInfo>();
	public static T GetCachedMember<T>(this Type type, string member) where T : MemberInfo
	{
		// Use AssemblyQualifiedName and the member name as a unique key to prevent a collision if two types have the same member name
		var key = type.AssemblyQualifiedName + member;
#pragma warning disable IDE0038
		if (MemberCache.ContainsKey(key)) return MemberCache[key] is T ? (T) MemberCache[key] : null;

		MemberInfo memberInfo = type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
		MemberCache[key] = memberInfo;

		return memberInfo is T ? (T) memberInfo : null;
#pragma warning restore IDE0038
	}

	public static T GetValue<T>(this Type type, string member, object target = null)
	{
		var fieldMember = type.GetCachedMember<FieldInfo>(member);
		var propertyMember = type.GetCachedMember<PropertyInfo>(member);

		return (T) (fieldMember != null ? fieldMember.GetValue(target) : propertyMember != null ? propertyMember.GetValue(target, null) : default(T));
	}

	public static void SetValue(this Type type, string member, object value, object target = null)
	{
		var fieldMember = type.GetCachedMember<FieldInfo>(member);
		if (fieldMember != null)
			fieldMember.SetValue(target, value);

		var propertyMember = type.GetCachedMember<PropertyInfo>(member);
		if (propertyMember != null)
			propertyMember.SetValue(target, value, null);
	}

	public static T CallMethod<T>(this Type type, string method, object target = null, params object[] arguments)
	{
		var member = type.GetCachedMember<MethodInfo>(method);
#pragma warning disable IDE0034, IDE0034WithoutSuggestion, RCS1244
		return member != null ? (T) member.Invoke(target, arguments) : default(T);
#pragma warning restore IDE0034, IDE0034WithoutSuggestion, RCS1244
	}

	public static void CallMethod(this Type type, string method, object target = null, params object[] arguments)
	{
		var member = type.GetCachedMember<MethodInfo>(method);
		if (member != null)
			member.Invoke(target, arguments);
	}

	public static T GetValue<T>(this object @object, string member)
	{
		return @object.GetType().GetValue<T>(member, @object);
	}

	public static void SetValue(this object @object, string member, object value)
	{
		@object.GetType().SetValue(member, value, @object);
	}

	public static T CallMethod<T>(this object @object, string member, params object[] arguments)
	{
		return @object.GetType().CallMethod<T>(member, @object, arguments);
	}

	public static void CallMethod(this object @object, string member, params object[] arguments)
	{
		@object.GetType().CallMethod(member, @object, arguments);
	}
}