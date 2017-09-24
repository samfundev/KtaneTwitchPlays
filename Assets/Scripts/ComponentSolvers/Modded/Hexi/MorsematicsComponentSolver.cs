using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class MorsematicsComponentSolver : ComponentSolver
{
    public MorsematicsComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _component = bombComponent.GetComponent(_componentType);
        _transmit = (KMSelectable)_transmitField.GetValue(_component);
        _switch = (KMSelectable)_switchField.GetValue(_component);
        helpMessage = "Turn the lights off with !{0} lights off. Turn the lights on with !{0} lights on. Tranmit the answer with !{0} transmit -..-";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if ((_lightsOn && inputCommand.Equals("lights off", StringComparison.InvariantCultureIgnoreCase)) || 
            (!_lightsOn && inputCommand.Equals("lights on", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return inputCommand;
            _lightsOn = !_lightsOn;
            DoInteractionStart(_switch);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_switch);
            yield break;
        }
        var letter = Regex.Match(inputCommand, "^((transmit|xmit|trans|tx) )([.-]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (letter.Success)
        {
            yield return inputCommand;
            yield return "strike";
            yield return "solve";
            foreach (var tx in letter.Groups[3].ToString())
            {
                DoInteractionStart(_transmit);
                yield return tx == '.'
                    ? new WaitForSeconds(0.08f)
                    : new WaitForSeconds(0.32f);
                DoInteractionEnd(_transmit);
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    static MorsematicsComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedMorse");
        _transmitField = _componentType.GetField("ButtonTransmit", BindingFlags.Public | BindingFlags.Instance);
        _switchField = _componentType.GetField("ButtonSwitch", BindingFlags.Public | BindingFlags.Instance);
    }

    private Component _component = null;
    private static Type _componentType = null;
    private static FieldInfo _switchField = null;
    private static FieldInfo _transmitField = null;
   


    private bool _lightsOn = true;
    private KMSelectable _switch = null;
    private KMSelectable _transmit = null;
}
