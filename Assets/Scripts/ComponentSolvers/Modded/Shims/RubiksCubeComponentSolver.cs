using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class RubiksCubeComponentSolver : ComponentSolver
{
	public RubiksCubeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_component = bombComponent.GetComponent(_componentType);
	    _cube = (Transform) _transformField.GetValue(_component);
	    _cube = _cube.parent;
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}
    private float getRotateRate(float targetTime, float rate)
    {
        return rate * (Time.deltaTime / targetTime);
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    if (inputCommand.Equals("rotate", StringComparison.InvariantCultureIgnoreCase))
	    {
	        yield return null;
	        const int angle = 75;

	        for (float i = 0; i < angle; i += getRotateRate(2, 300))
	        {
	            _cube.localEulerAngles = new Vector3(30 - i, 65 + ((i / angle) * 55), 55 - ((i / angle) * 10));
	            yield return null;
	        }
	        _cube.localEulerAngles = new Vector3(Mathf.Round(-45), Mathf.Round(120), Mathf.Round(45));
	        yield return new WaitForSeconds(2f);
	        for (float i = 0; i < angle; i += getRotateRate(2, 300))
	        {
	            _cube.localEulerAngles = new Vector3(-45 + i, 120 - ((i / angle) * 55), 45 + ((i / angle) * 100));
	            yield return null;
	        }
	        _cube.localEulerAngles = new Vector3(Mathf.Round(30), Mathf.Round(65), Mathf.Round(145));
	        yield return new WaitForSeconds(2f);
	        for (float i = 0; i < angle; i += getRotateRate(2, 300))
	        {
	            _cube.localEulerAngles = new Vector3(Mathf.Round(30), Mathf.Round(65), 145 - ((i / angle) * 90));
	            yield return null;
	        }
	        _cube.localEulerAngles = new Vector3(Mathf.Round(30), Mathf.Round(65), Mathf.Round(55));
        }
	    else
	    {
	        IEnumerator command = (IEnumerator) _ProcessCommandMethod.Invoke(_component, new object[] {inputCommand});
	        bool valid = false;
	        while (command.MoveNext())
	        {
	            valid = true;
	            yield return command.Current;
	        }
	        if (valid) yield return null;
	    }
	}

	static RubiksCubeComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("RubiksCubeModule");
		_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance);
	    _transformField = _componentType.GetField("OnAxis", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;
    private static FieldInfo _transformField = null;

    private Transform _cube = null;
	private object _component = null;
}
