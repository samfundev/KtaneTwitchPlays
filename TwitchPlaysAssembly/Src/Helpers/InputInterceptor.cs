using System;
using UnityEngine;

public static class InputInterceptor
{
	static InputInterceptor()
	{
		_inputSystems = Resources.FindObjectsOfTypeAll<AbstractControls>();
	}

	public static void EnableInput()
	{
		foreach (AbstractControls inputSystem in _inputSystems)
		{
			try
			{
				inputSystem.gameObject.SetActive(true);
				Cursor.visible = true;
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex);
			}
		}
	}

	public static void DisableInput()
	{
		foreach (AbstractControls inputSystem in _inputSystems)
		{
			try
			{
				inputSystem.gameObject.SetActive(false);
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex);
			}
		}
	}

	private static readonly AbstractControls[] _inputSystems = null;
}
