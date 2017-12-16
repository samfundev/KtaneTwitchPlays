using System;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class MurderComponentSolver : ComponentSolver
{
	public MurderComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
	    _display = (TextMesh[]) _displayField.GetValue(_component);
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    IEnumerable CycleThroughCategory(int index, string search = null)
    {
        int length = (index == 2) ? 9 : 4;
        //float delay = (search != null) ? 0.05f : 1.0f; // Doesn't seem to be used.
        KMSelectable button = _buttons[(index * 2) + 1];
        for (int i = 0; i < length; i++)
        {
            if ((search != null) &&
                (_display[index].text.ToLowerInvariant().EndsWith(search)))
            {
                yield return true;
                break;
            }
            yield return button;
        }
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
	    inputCommand = inputCommand.ToLowerInvariant();

	    if (inputCommand.Equals("accuse"))
	    {
	        yield return "accuse";
	        yield return DoInteractionClick(_buttons[6]);
	        yield break;
	    }
	    else if (inputCommand.StartsWith("cycle"))
	    {
	        bool cycleAll = (inputCommand.Equals("cycle"));
	        for (int i = 0; i < 3; i++)
	        {
	            if ((cycleAll) || (inputCommand.EndsWith(NameTypes[i])))
	            {
	                yield return inputCommand;
	                foreach (var item in CycleThroughCategory(i))
	                {
	                    yield return new WaitForSeconds(1.9f);
                        yield return DoInteractionClick((MonoBehaviour) item);
	                }
	            }
	        }
	        yield break;
	    }

	    string category, value;
	    int catIndex;
	    bool[] set = new bool[3] { false, false, false };
        bool[] tried = new bool[3] { false, false, false };

	    foreach (Match match in Regex.Matches(inputCommand, @"(" + string.Join("|", Commands) + ") ([a-z ]+)"))
	    {
	        category = match.Groups[1].ToString();
	        value = match.Groups[2].ToString().Trim();

	        catIndex = Array.IndexOf(Commands, category);
	        if ((catIndex == -1) || (set[catIndex]))
	        {
	            continue;
	        }
	        tried[catIndex] = true;

	        foreach (var item in CycleThroughCategory(catIndex, value))
	        {
	            if ((item is bool) && ((bool)item))
	            {
	                set[catIndex] = true;
	            }
	            else
	            {
	                yield return DoInteractionClick((MonoBehaviour) item);
	            }
	        }
	    }

	    if ((set[0]) && (set[1]) && (set[2]))
	    {
	        yield return DoInteractionClick(_buttons[6]);
	    }
		else
		{
		    for (var i = 0; i < 3; i++)
		    {
		        if (!tried[i]) continue;
		        if (set[i]) continue;
		        yield return "unsubmittablepenalty";
		        yield break;
            }
		}
    }

	static MurderComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("MurderModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	    _displayField = _componentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _displayField = null;

    private static readonly string[] Commands = new string[3] { "it was", "with the", "in the" };
    private static readonly string[] NameTypes = new string[3] { "people", "weapons", "rooms" };

    private object _component = null;
	private KMSelectable[] _buttons = null;
    private TextMesh[] _display = null;
}
