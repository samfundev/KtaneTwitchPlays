using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class StockMarketComponentSolver : ComponentSolver
{
	public StockMarketComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base (bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_rightButton = (KMSelectable)_rightButtonField.GetValue(_component);
		_leftButton = (KMSelectable)_leftButtonField.GetValue(_component);
		_submitButton = (KMSelectable)_submitButtonField.GetValue(_component);
		_companyOptions = (string[])_companyOptionsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit HSBC with !{0} submit HSBC. You can also use the first letter of the company in the submit command.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split[0] == "left")
		{
			yield return null;
			yield return DoInteractionClick(_leftButton);
		}
		else if (split[0] == "right")
		{
			yield return null;
			yield return DoInteractionClick(_rightButton);
		}
		else if (split[0].EqualsAny("submit", "invest") && split.Length == 1)
		{
			yield return null;
			yield return DoInteractionClick(_submitButton);
		}
		else if (split[0].EqualsAny("submit", "invest"))
		{
			bool valid = false;
			foreach (string company in _companyOptions)
			{
				if (company.ToLowerInvariant().StartsWith(split[1][0].ToString()))
					valid = true;
			}
			if (!valid) yield break;

			yield return null;
			while (!((TextMesh)_displayedCompanyField.GetValue(_component)).text.ToLowerInvariant().StartsWith(split[1][0].ToString()))
			{
				yield return null;
				yield return DoInteractionClick(_rightButton);
			}
			yield return DoInteractionClick(_submitButton);
		}
		else yield break;
	}

	static StockMarketComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("stockMarketScript");
		_rightButtonField = _componentType.GetField("cycleRightButton", BindingFlags.Instance | BindingFlags.Public);
		_leftButtonField = _componentType.GetField("cycleLeftButton", BindingFlags.Instance | BindingFlags.Public);
		_submitButtonField = _componentType.GetField("investButton", BindingFlags.Instance | BindingFlags.Public);
		_companyOptionsField = _componentType.GetField("companyOptions", BindingFlags.Instance | BindingFlags.Public);
		_displayedCompanyField = _componentType.GetField("displayedCompany", BindingFlags.Instance | BindingFlags.Public);
	}

	private static Type _componentType = null;
	private static FieldInfo _rightButtonField = null;
	private static FieldInfo _leftButtonField = null;
	private static FieldInfo _submitButtonField = null;
	private static FieldInfo _displayedCompanyField = null;

	private static FieldInfo _companyOptionsField = null;

	private readonly Component _component = null;
	private readonly KMSelectable _rightButton = null;
	private readonly KMSelectable _leftButton = null;
	private readonly KMSelectable _submitButton = null;

	private readonly string[] _companyOptions = null;
}
