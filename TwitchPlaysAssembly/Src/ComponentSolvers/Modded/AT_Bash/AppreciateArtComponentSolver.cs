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
		_component.SetValue("_solved", true);

		_componentType = _component.GetType();
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || split[0] != "appreciate")
			yield break;

		var targetModule = TwitchGame.Instance.Modules.Find(module => module.Code.EqualsIgnoreCase(split[1]));
		if (targetModule == null)
			yield break;

		if (Art.TryGetValue(targetModule, out Component artComponent)) {
			LocalAppreciationReqTime = artComponent.GetValue<float>("_appreciationRequiredDuration");
			artComponent.CallMethod("StartAppreciatingArt");
		}

		// If we're going to appreciate Art but we are also Art, don't allow the Show/HideAppreciation methods to appreciate ourselves.
		// Which would lead to appreciating two things at the same time.
		avoidDoubleAppreciation = artComponent != null && Art.ContainsKey(Module);

		yield return null;
		yield return ModuleCommands.Zoom(targetModule, new SuperZoomData(1, 0.5f, 0.5f), LocalAppreciationReqTime);

		if (CoroutineCanceller.ShouldCancel || artComponent == null)
		{
			if (artComponent != null)
				artComponent.CallMethod("StopAppreciatingArt");

			yield return $"sendtochat The appreciation of module ID {split[1]} was cancelled.";
			yield break;
		}

		artComponent.CallMethod("Enlighten");
	}

	// "Art is the Key, Art Appreciation is the Value." - some art guru or something.
	// Which is to say the key is the module an art appreciation cares about and the value is the Art Appreciation script that cares about it.
	// So we can know if a module needs to be appreciated and what art appreciation module needs to be solved if we stare about it.
	private static Dictionary<TwitchModule, Component> Art => TwitchGame.Instance.Modules
				.Where(module => !module.Solved && _componentType != null)
				.Select(module => module.BombComponent.GetComponent(_componentType))
				.Where(component => component != null)
				.ToDictionary(component => {
					var bombComponent = component.GetValue<Transform>("_transform").GetComponent<BombComponent>();
					return TwitchGame.Instance.Modules.First(module => module.BombComponent == bombComponent);
				}, component => component);

	public static void ShowAppreciation(TwitchModule module)
	{
		if (Art.TryGetValue(module, out Component artComponent) && !avoidDoubleAppreciation)
			artComponent.CallMethod("StartAppreciatingArt");
	}

	public static void HideAppreciation(TwitchModule module)
	{
		if (Art.TryGetValue(module, out Component artComponent) && !avoidDoubleAppreciation)
			artComponent.CallMethod("StopAppreciatingArt");
	}

	private static Type _componentType;
}
