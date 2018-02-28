using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ExtendedPasswordComponentSolver : ComponentSolver
{
    public ExtendedPasswordComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _component = bombComponent.GetComponent(_componentType);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
		if (inputCommand.StartsWith("cycle ", StringComparison.InvariantCultureIgnoreCase))
	    {
		    HashSet<int> alreadyCycled = new HashSet<int>();
		    string[] commandParts = inputCommand.Split(' ');
		    
		    foreach (string cycle in commandParts.Skip(1))
		    {
			    if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > 6)
				    continue;

			    IEnumerator spinnerCoroutine = (IEnumerator)_ProcessCommandMethod.Invoke(_component, new object[] { $"cycle {cycle}" });
				while (spinnerCoroutine.MoveNext())
			    {
				    yield return spinnerCoroutine.Current;
				    yield return "trycancel";
			    }
		    }
		    yield break;
	    }

        IEnumerator command = (IEnumerator)_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
        while (command.MoveNext())
        {
			yield return command.Current;
	        yield return "trycancel";
        }
        if (inputCommand.Trim().Length == 6)
        {
            yield return null;
            yield return "unsubmittablepenalty";
        }
    }

    static ExtendedPasswordComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("ExtendedPassword", "ExtendedPassword");
        _ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static MethodInfo _ProcessCommandMethod = null;

    private object _component = null;
}
