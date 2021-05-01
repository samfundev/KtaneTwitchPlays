using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MysteryWidgetShim : ComponentSolverShim
{
	public static readonly List<GameObject> Covers = new List<GameObject>();

	public MysteryWidgetShim(TwitchModule module)
		: base(module, "widgetModule")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());

		module.StartCoroutine(WaitForMysteryWidget());
	}

	IEnumerator WaitForMysteryWidget()
	{
		var component = Module.BombComponent.GetComponent("WidgetMagic");

		GameObject mystified;
		do
		{
			mystified = component.GetValue<GameObject>("Cover");
			yield return null;
		} while (mystified == null);
		Covers.Add(mystified);

		ModuleCameras.Instance.SetupEdgeworkCameras();
	}

	public static void ClearUnused()
	{
		if (Covers.Count == 0) return;
		GameObject[] tmp = new GameObject[Covers.Count];
		Covers.CopyTo(tmp);
		foreach (GameObject j in tmp)
		{
			if (j == null)
			{
				Covers.Remove(j);
				ModuleCameras.Instance.SetupEdgeworkCameras();
			}
		}
	}

	public static IEnumerable<Dictionary<string, T>> FilterQuery<T>(string queryKey, IEnumerable<Dictionary<string, T>> responses)
	{
		ClearUnused();

		if (queryKey == "indicatorColor")
			queryKey = "indicator";

		switch (queryKey)
		{
			case "batteries" when responses is IEnumerable<Dictionary<string, int>> batteries:
				int AAs = 0;
				int DCells = 0;

				foreach (GameObject cover in Covers)
				{
					AAs += cover.transform.parent.GetComponentsInChildren<Transform>().Count(x => x.name.EqualsIgnoreCase("AA"));
					DCells += cover.transform.parent.GetComponentsInChildren<Transform>().Count(x => x.name.EqualsIgnoreCase("DCell"));
				}

				return (IEnumerable<Dictionary<string, T>>) batteries.Where(battery =>
				{
					var numbatteries = battery["numbatteries"];
					if (AAs != 0 && numbatteries == 2)
					{
						AAs--;
						return false;
					}
					else if (DCells != 0 && numbatteries == 1)
					{
						DCells--;
						return false;
					}

					return true;
				});
			case "indicator" when responses is IEnumerable<Dictionary<string, string>> indicators:
				var hiddenLabels = Covers
					.SelectMany(cover => cover.transform.parent.GetComponentsInChildren<Component>().Where(x => x.GetType().Name == "TextMeshPro"))
					.Select(IndMesh => IndMesh.GetType().GetMethod("get_text").Invoke(IndMesh, null).ToString())
					.Where(IndLabel => IndLabel != null);

				return (IEnumerable<Dictionary<string, T>>) indicators.Where(indicator => !hiddenLabels.Contains(indicator["label"]));
			case "ports" when responses is IEnumerable<Dictionary<string, List<string>>> plates:
				var coveredPlates = Covers
					.Where(cover => cover.transform.parent.name == "PortWidget(Clone)")
					.Select(cover => cover.transform.parent.GetComponentsInChildren<Transform>().Select(transform => transform.name).ToList())
					.ToList();
				var portNames = new[] { "RJ", "PS2", "RCA", "DVI", "Serial", "Parallel" };

				return (IEnumerable<Dictionary<string, T>>) plates.Where(plate => !coveredPlates.Any(coveredPlate =>
				{
					var plateFound = plate["presentPorts"].All(port => coveredPlate.Any(coveredPort => port.ContainsIgnoreCase(coveredPort)));
					if (plateFound)
						coveredPlates.Remove(coveredPlate);

					return plateFound;
				}));
			case "serial" when responses is IEnumerable<Dictionary<string, string>> serial:
				return (IEnumerable<Dictionary<string, T>>) serial.Where(_ => !Covers.Any(cover => cover.transform.parent.name.EqualsIgnoreCase("serial")));
			default:
				return responses;
		}
	}
}