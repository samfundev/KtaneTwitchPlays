using System.Collections;
using System.Collections.Generic;

public class TheRuleComponentSolver : ComponentSolver
{
    public TheRuleComponentSolver(TwitchModule module) :
        base(module)
    {
        Buttons = Module.BombComponent.GetComponent<KMSelectable>().Children;
        ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Toggle squares with '!{0} toggle # # #': squares are numbered in reading order (only the clickable ones). Submit the module with '!{0} submit!'");
    }

    protected internal override IEnumerator RespondToCommandInternal(string inputCommand){
        string command = inputCommand.ToLowerInvariant();
        if(command=="submit"){
            yield return null;
            yield return DoInteractionClick(Buttons[18]);
            yield break;
        }
        string[] btns=command.Replace("toggle ","").Replace("press ","").SplitFull(' ', ';', ',');
        List<KMSelectable> buttonsList = new List<KMSelectable>();
        foreach(string btn in btns){
            if(!int.TryParse(btn, out int num)){
                yield return null;
                yield return "sendtochaterror Number not valid!";
                yield break;
            }
            if(num<1 || num>18){
                yield return null;
                yield return "sendtochaterror Number out of range!";
                yield break;
            }
            buttonsList.Add(Buttons[num-1]);
        }
        yield return null;
        foreach(KMSelectable btntopress in buttonsList){
            yield return DoInteractionClick(btntopress);
        }
    }

    private readonly KMSelectable[] Buttons;
}