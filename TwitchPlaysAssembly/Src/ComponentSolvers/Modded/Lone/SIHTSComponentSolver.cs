using System;
using System.Collections;
using System.Reflection;

public class SIHTSComponentSolver : CommandComponentSolver
{
	public SIHTSComponentSolver(TwitchModule module) :
		base(module, "SIHTS", "!{0} underhand/flick [Set whether you will flick or underhand toss the coin] | !{0} increased/decreased/unchanged [Sets the coin acceleration]")
	{
		_underhand = (KMSelectable) UnderhandField.GetValue(_component);
		_flick = (KMSelectable) FlickField.GetValue(_component);
		_unchanged = (KMSelectable) UnchangedField.GetValue(_component);
		_increased = (KMSelectable) IncreasedField.GetValue(_component);
		_decreased = (KMSelectable) DecreasedField.GetValue(_component);
	}

	private IEnumerator Underhand(CommandParser _)
	{
		_.Literal("underhand");

		yield return null;
		yield return DoInteractionClick(_underhand);
	}

	private IEnumerator Flick(CommandParser _)
	{
		_.Literal("flick");

		yield return null;
		yield return DoInteractionClick(_flick);
	}
	private IEnumerator Unchanged(CommandParser _)
	{
		_.Literal("unchanged");

		yield return null;
		yield return DoInteractionClick(_unchanged);
	}

	private IEnumerator Increased(CommandParser _)
	{
		_.Literal("increased");

		yield return null;
		yield return DoInteractionClick(_increased);
	}

	private IEnumerator Decreased(CommandParser _)
	{
		_.Literal("decreased");

		yield return null;
		yield return DoInteractionClick(_decreased);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (TossTypeValue == 0 && !UHPressedValue)
			yield return DoInteractionClick(_underhand);
		else if (TossTypeValue == 1 && !FPressedValue)
			yield return DoInteractionClick(_flick);

		if (ComponentType.CallMethod<double>("floatToEV", _component, UHNoIntValue % 1.0) == TossValue || ComponentType.CallMethod<double>("floatToEV", _component, FNoIntValue % 1.0) == TossValue)
			yield return DoInteractionClick(_unchanged);
		else if (ComponentType.CallMethod<double>("floatToEV", _component, UHDecAccValue % 1.0) == TossValue || ComponentType.CallMethod<double>("floatToEV", _component, FDecAccValue % 1.0) == TossValue)
			yield return DoInteractionClick(VegemiteValue ? _increased : _decreased);
		else
			yield return DoInteractionClick(VegemiteValue ? _decreased : _increased);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SIHTS");
	private static readonly FieldInfo UnderhandField = ComponentType.GetField("underhandSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo FlickField = ComponentType.GetField("flickSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo UnchangedField = ComponentType.GetField("noIntSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo IncreasedField = ComponentType.GetField("incAccSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo DecreasedField = ComponentType.GetField("decAccSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo VegemiteField = ComponentType.GetField("vegemite", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo UHPressedField = ComponentType.GetField("UHPressed", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo FPressedField = ComponentType.GetField("FPressed", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo TossTypeField = ComponentType.GetField("requiredTossType", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo TossField = ComponentType.GetField("requiredToss", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo UHNoIntField = ComponentType.GetField("UH_noIntRot", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo FNoIntField = ComponentType.GetField("F_noIntRot", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo UHDecAccField = ComponentType.GetField("UH_decAccRot", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo FDecAccField = ComponentType.GetField("F_decAccRot", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly KMSelectable _underhand;
	private readonly KMSelectable _flick;
	private readonly KMSelectable _unchanged;
	private readonly KMSelectable _increased;
	private readonly KMSelectable _decreased;

	private bool VegemiteValue => (bool) VegemiteField.GetValue(_component);
	private bool UHPressedValue => (bool) UHPressedField.GetValue(_component);
	private bool FPressedValue => (bool) FPressedField.GetValue(_component);
	private int TossTypeValue => (int) TossTypeField.GetValue(_component);
	private double TossValue => (int) TossField.GetValue(_component);
	private double UHNoIntValue => (double) UHNoIntField.GetValue(_component);
	private double FNoIntValue => (double) FNoIntField.GetValue(_component);
	private double UHDecAccValue => (double) UHDecAccField.GetValue(_component);
	private double FDecAccValue => (double) FDecAccField.GetValue(_component);
}