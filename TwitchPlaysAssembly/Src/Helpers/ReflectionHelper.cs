using System;
using System.Linq;

public static class ReflectionHelper
{
    public static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName.Equals(fullName));
    }

    public static Type FindType(string fullName, string assemblyName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName.Equals(fullName) && t.Assembly.GetName().Name.Equals(assemblyName));
    }
}
