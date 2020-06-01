using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class AppreciateArtComponentSolver : ComponentSolver
{
	private bool IsBeingZoomed = false;
	private float? LocalStartTime = null;
	private float LocalAppreciationReqTime = 60f;

	public AppreciateArtComponentSolver(TwitchModule module) :
		base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Zoom the module ('!{0} zoom 60') to appreciate the art.");
		module.StartCoroutine(HandleInteraction());
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		// No valid commands.
		yield break;
	}

	protected internal void ZoomStarted(string byUser)
	{
		IsBeingZoomed = true;
		ForceAwardSolveToNickName(byUser);
	}

	protected internal void ZoomEnded()
	{
		IsBeingZoomed = false;
	}

	private IEnumerator HandleInteraction()
	{
		// We set _solved to true, which stops the module's own interactions.
		SolvedField.SetValue(Module.BombComponent.GetComponent(ComponentType), true);
		LocalAppreciationReqTime = (float)AppreciationTimeRequiredField.GetValue(Module.BombComponent.GetComponent(ComponentType));

		while (true)
		{
			if (IsBeingZoomed)
			{
				// Camera is zoomed, appreciate
				if (!LocalStartTime.HasValue)
				{
					// Just started appreciating
					StartAppreciateMethod.Invoke(Module.BombComponent.GetComponent(ComponentType), null);
					LocalStartTime = Time.time;
				}
				else if (Time.time - LocalStartTime >= LocalAppreciationReqTime)
				{
					// Appreciation time sufficient
					EnlightenMethod.Invoke(Module.BombComponent.GetComponent(ComponentType), null);
					yield break;
				}
			}
			else if (LocalStartTime.HasValue)
			{
				// Not enough appreciation and zoom has ended
				StopAppreciateMethod.Invoke(Module.BombComponent.GetComponent(ComponentType), null);
				LocalStartTime = null;
			}

			yield return null;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("AppreciateArtModule");
	private static readonly FieldInfo SolvedField = ComponentType.GetField("_solved", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo AppreciationTimeRequiredField = ComponentType.GetField("_appreciationRequiredDuration", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo StartAppreciateMethod = ComponentType.GetMethod("StartAppreciatingArt", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo StopAppreciateMethod = ComponentType.GetMethod("StopAppreciatingArt", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo EnlightenMethod = ComponentType.GetMethod("Enlighten", BindingFlags.NonPublic | BindingFlags.Instance);
}
