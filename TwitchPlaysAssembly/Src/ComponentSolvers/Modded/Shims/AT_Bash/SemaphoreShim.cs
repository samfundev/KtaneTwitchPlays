using System.Collections;

[ModuleID("Semaphore")]
public class SemaphoreShim : ReflectionComponentSolverShim
{
	public SemaphoreShim(TwitchModule module)
		: base(module, "SemaphoreModule")
	{
		_prev = _component.GetValue<KMSelectable>("PreviousButton");
		_next = _component.GetValue<KMSelectable>("NextButton");
		_ok = _component.GetValue<KMSelectable>("OKButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<string>("_semaphoreSequence") == null) yield return true;
		int endIndex = _component.GetValue<object>("Sequencer").GetValue<IList>("characterSequence").IndexOf(_component.GetValue<object>("_correctSemaphoreCharacter"));
		int curIndex = _component.GetValue<object>("Sequencer").GetValue<int>("currentCharacterIndex");
		if (curIndex < endIndex)
		{
			for (int i = 0; i < endIndex - curIndex; i++)
				yield return DoInteractionClick(_next);
		}
		else if (curIndex > endIndex)
		{
			for (int i = 0; i < curIndex - endIndex; i++)
				yield return DoInteractionClick(_prev);
		}
		yield return DoInteractionClick(_ok, 0);
	}

	private readonly KMSelectable _prev;
	private readonly KMSelectable _next;
	private readonly KMSelectable _ok;
}