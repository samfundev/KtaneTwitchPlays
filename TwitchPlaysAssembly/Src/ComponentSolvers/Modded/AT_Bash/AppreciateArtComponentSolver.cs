using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class AppreciateArtComponentSolver : ReflectionComponentSolver
{
	private float LocalAppreciationReqTime = 60f;
	private static bool avoidDoubleAppreciation;

	public AppreciateArtComponentSolver(TwitchModule module) :
		base(module, "AppreciateArtModule", "Use '!{0} appreciate {0}' to appreciate a module.")
	{
		// We set _solved to true, which stops the module's own interactions.
		SolvedField.SetValue(Module.BombComponent.GetComponent(ComponentType), true);
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || split[0] != "appreciate")
			yield break;

		var targetModule = TwitchGame.Instance.Modules.Find(module => module.Code.EqualsIgnoreCase(split[1]));
		if (targetModule == null)
			yield break;

		if (Art.TryGetValue(targetModule, out Component artComponent)) {
			LocalAppreciationReqTime = (float) AppreciationTimeRequiredField.GetValue(artComponent);
			StartAppreciateMethod.Invoke(artComponent, null);
		}

		// If we're going to appreciate Art but we are also Art, don't allow the Show/HideAppreciation methods to appreciate ourselves.
		// Which would lead to appreciating two things at the same time.
		avoidDoubleAppreciation = artComponent != null && Art.ContainsKey(Module);

		yield return null;
		yield return ModuleCommands.Zoom(targetModule, new SuperZoomData(1, 0.5f, 0.5f), LocalAppreciationReqTime);

		if (CoroutineCanceller.ShouldCancel || artComponent == null)
		{
			if (artComponent != null)
				StopAppreciateMethod.Invoke(artComponent, null);

			yield return $"sendtochat The appreciation of module ID {split[1]} was cancelled.";
			yield break;
		}

		EnlightenMethod.Invoke(artComponent, null);
	}

	// "Art is the Key, Art Appreciation is the Value." - some art guru or something.
	// Which is to say the key is the module an art appreciation cares about and the value is the Art Appreciation script that cares about it.
	// So we can know if a module needs to be appreciated and what art appreciation module needs to be solved if we stare about it.
	private static Dictionary<TwitchModule, Component> Art => TwitchGame.Instance.Modules
				.Where(module => !module.Solved)
				.Select(module => module.BombComponent.GetComponent(ComponentType))
				.Where(component => component != null)
				.ToDictionary(component => {
					var bombComponent = component.GetValue<Transform>("_transform").GetComponent<BombComponent>();
					return TwitchGame.Instance.Modules.First(module => module.BombComponent == bombComponent);
				}, component => component);

	public static void ShowAppreciation(TwitchModule module)
	{
		if (Art.TryGetValue(module, out Component artComponent) && !avoidDoubleAppreciation)
			StartAppreciateMethod.Invoke(artComponent, null);
	}

	public static void HideAppreciation(TwitchModule module)
	{
		if (Art.TryGetValue(module, out Component artComponent) && !avoidDoubleAppreciation)
			StopAppreciateMethod.Invoke(artComponent, null);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("AppreciateArtModule");
	private static readonly FieldInfo SolvedField = ComponentType.GetField("_solved", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo AppreciationTimeRequiredField = ComponentType.GetField("_appreciationRequiredDuration", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo StartAppreciateMethod = ComponentType.GetMethod("StartAppreciatingArt", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo StopAppreciateMethod = ComponentType.GetMethod("StopAppreciatingArt", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo EnlightenMethod = ComponentType.GetMethod("Enlighten", BindingFlags.NonPublic | BindingFlags.Instance);
}
