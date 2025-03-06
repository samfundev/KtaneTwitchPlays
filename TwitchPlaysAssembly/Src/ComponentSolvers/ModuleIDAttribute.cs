[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public class ModuleIDAttribute : System.Attribute
{
	public readonly string ModuleID;

	public ModuleIDAttribute(string moduleID)
	{
		ModuleID = moduleID;
	}
}
