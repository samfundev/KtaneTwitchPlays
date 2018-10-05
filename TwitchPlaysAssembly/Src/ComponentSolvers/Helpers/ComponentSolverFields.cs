using System.Reflection;
using UnityEngine;

public class ComponentSolverFields
{
	public Component CommandComponent;
	public MethodInfo Method;
	public MethodInfo ForcedSolveMethod;
	public ModuleInformation ModuleInformation;

	public FieldInfo ZenModeField;
	public FieldInfo TimeModeField;
	public FieldInfo AbandonModuleField;
	public FieldInfo TwitchPlaysField;
	public FieldInfo TwitchPlaysSkipTimeField;
	public FieldInfo CancelField;
}
