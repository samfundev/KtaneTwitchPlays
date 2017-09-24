using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ComponentSolverFactory
{
    private delegate ComponentSolver ModComponentSolverDelegate(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller);
    private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreators;
    private static readonly Dictionary<string, ModuleInformation> ModComponetSolverInformation;

    private enum ModCommandType
    {
        Simple,
        Coroutine
    }

    static ComponentSolverFactory()
    {
        ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
        ModComponetSolverInformation = new Dictionary<string, ModuleInformation>();

        //AT_Bash Modules
        ModComponentSolverCreators["MotionSense"] = (bombCommander, bombComponent, ircConnection, canceller) => new MotionSenseComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

       //Hexi Modules
        ModComponentSolverCreators["MemoryV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new ForgetMeNotComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["KeypadV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new RoundKeypadComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["ButtonV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new SquareButtonComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["SimonV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new SimonStatesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["PasswordV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new SafetySafeComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["MazeV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new PlumbingComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["MorseV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new MorsematicsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["NeedyVentV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new NeedyQuizComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		ModComponentSolverCreators["NeedyKnobV2"] = (bombCommander, bombComponent, ircConnection, canceller) => new NeedyRotaryPhoneComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Perky Modules  (Silly Slots is maintained by Timwi, and as such its handler lives there.)
        ModComponentSolverCreators["CrazyTalk"] = (bombCommander, bombComponent, ircConnection, canceller) => new CrazyTalkComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["CryptModule"] = (bombCommander, bombComponent, ircConnection, canceller) => new CryptographyComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["ForeignExchangeRates"] = (bombCommander, bombComponent, ircConnection, canceller) => new ForeignExchangeRatesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["Listening"] = (bombCommander, bombComponent, ircConnection, canceller) => new ListeningComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["OrientationCube"] = (bombCommander, bombComponent, ircConnection, canceller) => new OrientationCubeComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["Probing"] = (bombCommander, bombComponent, ircConnection, canceller) => new ProbingComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["TurnTheKey"] = (bombCommander, bombComponent, ircConnection, canceller) => new TurnTheKeyComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["TurnTheKeyAdvanced"] = (bombCommander, bombComponent, ircConnection, canceller) => new TurnTheKeyAdvancedComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Kaneb Modules
        ModComponentSolverCreators["TwoBits"] = (bombCommander, bombComponent, ircConnection, canceller) => new TwoBitsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Asimir Modules
        ModComponentSolverCreators["shapeshift"] = (bombCommander, bombComponent, ircConnection, canceller) => new ShapeShiftComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["SeaShells"] = (bombCommander, bombComponent, ircConnection, canceller) => new SeaShellsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Spare Wizard Modules
        ModComponentSolverCreators["spwiz3DMaze"] = (bombCommander, bombComponent, ircConnection, canceller) => new ThreeDMazeComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Misc Modules
        ModComponentSolverCreators["alphabet"] = (bombCommander, bombComponent, ircConnection, canceller) => new AlphabetComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["NumberPad"] = (bombCommander, bombComponent, ircConnection, canceller) => new NumberPadComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["switchModule"] = (bombCommander, bombComponent, ircConnection, canceller) => new SwitchesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["resistors"] = (bombCommander, bombComponent, ircConnection, canceller) => new ResistorsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["Microcontroller"] = (bombCommander, bombComponent, ircConnection, canceller) => new MicrocontrollerComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
        ModComponentSolverCreators["ChordQualities"] = (bombCommander, bombComponent, ircConnection, canceller) => new ChordQualitiesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

        //Module Information
        //Information decleared here will be used to generate ModuleInformation.json if it doesn't already exist,  and will be overwritten by ModuleInformation.json if it does exist.
        /*
         * 
            Typical ModuleInformation json entry
            {
                "moduleDisplayName": "Double-Oh",
                "moduleID": "DoubleOhModule",
                "helpText": "Cycle the buttons with !{0} cycle. (Cycle presses each button 3 times, in the order of vert1, horiz1, horiz2, vert2, submit.)  Submit your answer with !{0} press vert1 horiz1 horiz2 vert2 submit.",
                "manualCode": null,
                "statusLightLeft": false,
                "statusLightDown": false,
                "chatRotation": 0.0,
                "validCommands": null,
                "DoesTheRightThing": false,
                "helpTextOverride": false,
                "manualCodeOverride": false,
                "statusLightOverride": true,
                "validCommandsOverride": false
            },
         * 
         * moduleDisplayName - The name of the module as displayed in Mod Selector or the chat box.
         * moduleID - The unique identifier of the module.
         * 
         * helpTextOverride - Specifies whether the help text should not be overwritten by what is present in the module.
         * helpText - Instructions on how to interact with the module in twitch plays.
         * 
         * manualCodeOverride - Specifies whether the manual code should not be overwritten by what is present in the module.
         * manualCode - If defined, is used instead of moduleDisplayName to look up the html/pdf manual.
         * 
         * statusLightOverride - Specifies an override of the ID# position / rotation. (This must be set if you wish to have the ID be anywhere other than
         *      Above the status light, or if you wish to rotate the ID / chat box.)
         * statusLightLeft - Specifies whether the ID should be on the left side of the module.
         * statusLightDown - Specifies whether the ID should be on the bottom side of the module.
         * chatRotation - Specifies whether the chat box / ID should be rotated.  (not currently implemented yet.)
         * 
         * Finally, validCommands, DoesTheRightThing and all of the override flags will only show up in modules not built into Twitch plays.
         * validCommandsOverride - Specifies whether the valid regular expression list should not be updated from the module.
         * validCommands - A list of valid regular expression commands that define if the command should be passed onto the modules Twitch plays handler.
         *      If null, the command will always be passed on.
         *      
         * DoesTheRightThing - Specifies whether the module properly yields return something BEFORE interacting with any buttons.
         * 
         * 
         */

        //All of these modules are built into Twitch plays.

        //Asimir
        ModComponetSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "SeaShells", moduleDisplayName = "Sea Shells", helpText = "Press buttons by typing !{0} press alar llama. You can submit partial text as long it only matches one button. NOTE: Each button press is separated by a space so typing \"burglar alarm\" will press a button twice." , moduleScore = 7};
        ModComponetSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "shapeshift", moduleDisplayName = "Shape Shift", helpText = "Submit your anwser with !{0} submit point round. Reset to initial state with !{0} reset. Valid shapes: flat, point, round and ticket.", moduleScore = 8 };

        //AT_Bash / Bashly
        ModComponetSolverInformation["MotionSense"] = new ModuleInformation {builtIntoTwitchPlays = true, moduleID = "MotionSense", moduleDisplayName = "Motion Sense", helpText = "I am a passive module that awards strikes for motion while I am active. Use !{0} status to find out if I am active, and for how long."};

        //Hexicube
        ModComponetSolverInformation["MemoryV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MemoryV2", moduleDisplayName = "Forget Me Not", helpText = "Enter forget me not sequence with !{0} press 5 3 1 8 2 0... The Sequence length depends on how many modules were on the bomb.", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true };
        ModComponetSolverInformation["KeypadV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "KeypadV2", moduleDisplayName = "Round Keypad", helpText = "Solve the module with !{0} press 2 4 6 7 8. Button 1 is the top most botton, and are numbered in clockwise order.", moduleScore = 6 };
        ModComponetSolverInformation["ButtonV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "ButtonV2", moduleDisplayName = "Square Button", helpText = "Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.", moduleScore = 6 };
        ModComponetSolverInformation["SimonV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "SimonV2", moduleDisplayName = "Simon States", helpText = "Enter the response with !{0} press B Y R G.", moduleScore = 8 };
        ModComponetSolverInformation["PasswordV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "PasswordV2", moduleDisplayName = "Safety Safe", helpText = "Listen to the dials with !{0} cycle. Listen to a single dial with !{0} cycle BR. Make a correction to a single dial with !{0} BM 3. Enter the solution with !{0} 6 0 6 8 2 5. Submit the answer with !{0} submit. Dial positions are TL, TM, TR, BL, BM, BR.", moduleScore = 15 };
        ModComponetSolverInformation["MazeV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MazeV2", moduleDisplayName = "Plumbing", helpText = "Rotate the pipes with !{0} rotate A1 A1 B2 B3 C2 C3 C3. Check your work for leaks Kappa with !{0} submit. (Pipes rotate clockwise. Top left is A1, Bottom right is F6)", moduleScore = 20 };
        ModComponetSolverInformation["MorseV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MorseV2", moduleDisplayName = "Moresmatics", helpText = "Turn the lights off with !{0} lights off. Turn the lights on with !{0} lights on. Tranmit the answer with !{0} transmit -..-", moduleScore = 12 };
        ModComponetSolverInformation["NeedyVentV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NeedyVentV2", moduleDisplayName = "Needy Answering Questions", helpText = "Answer the question with !{0} Y or !{0} N.", manualCode = "Answering%20Questions" };
        ModComponetSolverInformation["NeedyKnobV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NeedyKnobV2", moduleDisplayName = "Needy Rotary Phone", helpText = "Respond to the phone call with !{0} press 8 4 9.", manualCode = "Rotary%20Phone" };

        //Perky
        ModComponetSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "CrazyTalk", moduleDisplayName = "Crazy Talk", helpText = "Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.", moduleScore = 3 };
        ModComponetSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "CryptModule", moduleDisplayName = "Cryptography", helpText = "Solve the cryptography puzzle with !{0} press N B V T K.", moduleScore = 9 };
        ModComponetSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "ForeignExchangeRates", moduleDisplayName = "Foreign Exchange Rates", helpText = "Solve the module with !{0} press ML. Positions are TL, TM, TR, ML, MM, MR, BL, BM, BR.", moduleScore = 6 };
        ModComponetSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, statusLightLeft = true, moduleID = "Listening", moduleDisplayName = "Listening", helpText = "Listen to the sound with !{0} press play. Enter the response with !{0} press $ & * * #.", moduleScore = 8 };
        ModComponetSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "OrientationCube", moduleDisplayName = "Orientation Cube", helpText = "Move the cube with !{0} press cw l set.  The buttons are l, r, cw, ccw, set.", moduleScore = 6 };
        ModComponetSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "Probing", moduleDisplayName = "Probing", helpText = "Get the readings with !{0} cycle. Try a combination with !{0} connect 4 3.  Cycle reads 1&2, 1&3, 1&4, 1&5, 1&6.", moduleScore = 6 };
        ModComponetSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "TurnTheKey", moduleDisplayName = "Turn The Key", helpText = "Turn the key at specified time with !{0} turn 8:29", moduleScore = 6 };
        ModComponetSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "TurnTheKeyAdvanced", moduleDisplayName = "Turn The Keys", helpText = "Turn the left key with !{0} turn left. Turn the right key with !{0} turn right.", moduleScore = 15 };

        //Kaneb
        ModComponetSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "TwoBits", moduleDisplayName = "Two Bits", helpText = "Query the answer with !{0} press K T query. Submit the answer with !{0} press G Z submit.", moduleScore = 8};

        //SpareWizard
        ModComponetSolverInformation["spwiz3DMaze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "spwiz3DMaze", moduleDisplayName = "3D Maze", helpText = "Move around the maze using !{0} move left forward right. Walk slowly around the maze using !{0} walk left forawrd right. Shorten forms of the directions are also acceptable. You can use \"uturn\" or \"u\" to turn around.", moduleScore = 20};

        //Misc
        ModComponetSolverInformation["alphabet"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "alphabet", moduleDisplayName = "Alphabet", helpText = "Submit your anwser with !{0} press A B C D.", moduleScore = 5 };
        ModComponetSolverInformation["ChordQualities"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "ChordQualities", moduleDisplayName = "Chord Qualities", helpText = "Submit a chord using !{0} submit A B C# D", moduleScore = 9};
        ModComponetSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "Microcontroller", moduleDisplayName = "Microcontroller", helpText = "Set the current pin color with !{0} set red. Cycle the current pin !{0} cycle. Valid colors: white, red, yellow, magenta, blue, green.", moduleScore = 10 };
        ModComponetSolverInformation["NumberPad"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NumberPad", moduleDisplayName = "Number Pad", helpText = "Submit your anwser with !{0} submit 4236.", moduleScore = 5 };
        ModComponetSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "resistors", moduleDisplayName = "Resistors", helpText = "Connect sets of two pins with !{0} connect a tl tr c. Use !{0} submit to submit and !{0} clear to clear. Valid pins: A B C D TL TR BL BR. Top and Bottom refer to the top and bottom resistor.", moduleScore = 6};
        ModComponetSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "switchModule", moduleDisplayName = "Switches", helpText = "Flip switches using !{0} flip 1 5 3 2.", moduleScore = 3 };


        //Steel Crate Games (Need these in place even for the Vanilla modules)
        ModComponetSolverInformation["WireSetComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "WireSetComponentSolver", moduleDisplayName = "Simple Wires", helpText = "!{0} cut 3 [cut wire 3] | Wires are ordered from top to bottom | Empty spaces are not counted", moduleScore = 1};
        ModComponetSolverInformation["ButtonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "ButtonComponentSolver", moduleDisplayName = "Big Button", helpText = "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]", moduleScore = 1};
        ModComponetSolverInformation["WireSequenceComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "WireSequenceComponentSolver", moduleDisplayName = "Wire Sequence", helpText = "!{0} cut 7 [cut wire 7] | !{0} down, !{0} d [next stage] | !{0} up, !{0} u [previous stage] | !{0} cut 7 8 9 d [cut multiple wires and continue] | Use the numbers shown on the module", manualCode = "Wire Sequences", moduleScore = 4};
        ModComponetSolverInformation["WhosOnFirstComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "WhosOnFirstComponentSolver", moduleDisplayName = "Who's on First", helpText = "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive", manualCode = "Who%E2%80%99s on First", moduleScore = 4};
        ModComponetSolverInformation["VennWireComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "VennWireComponentSolver", moduleDisplayName = "Complicated Wires", helpText = "!{0} cut 3 [cut wire 3] | !{0} cut 2 3 6 [cut multiple wires] | Wires are ordered from left to right | Empty spaces are not counted", moduleScore = 3};
        ModComponetSolverInformation["SimonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "SimonComponentSolver", moduleDisplayName = "Simon Says", helpText = "!{0} press red green blue yellow, !{0} press rgby [press a sequence of colours] | You must include the input from any previous stages", moduleScore = 3};
        ModComponetSolverInformation["PasswordComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "PasswordComponentSolver", moduleDisplayName = "Password", helpText = "!{0} cycle 3 [cycle through the letters in column 3] | !{0} world [try to submit a word]", manualCode = "Passwords", moduleScore = 2};
        ModComponetSolverInformation["NeedyVentComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NeedyVentComponentSolver", moduleDisplayName = "Needy Vent Gas", helpText = "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]" };
        ModComponetSolverInformation["NeedyKnobComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NeedyKnobComponentSolver", moduleDisplayName = "Needy Knob", helpText = "!{0} rotate 3, !{0} turn 3 [rotate the knob 3 quarter-turns]", manualCode = "Knobs" };
        ModComponetSolverInformation["NeedyDischargeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "NeedyDischargeComponentSolver", moduleDisplayName = "Needy Capacitor", helpText = "!{0} hold 7 [hold the lever for 7 seconds]", manualCode = "Capacitor Discharge" };
        ModComponetSolverInformation["MorseCodeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MorseCodeComponentSolver", moduleDisplayName = "Morse Code", helpText = "!{0} transmit 3.573, !{0} trans 573, !{0} tx 573 [transmit frequency 3.573]", moduleScore = 3};
        ModComponetSolverInformation["MemoryComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MemoryComponentSolver", moduleDisplayName = "Memory", helpText = "!{0} position 2, !{0} pos 2, !{0} p 2 [2nd position] | !{0} label 3, !{0} lab 3, !{0} l 3 [label 3]", moduleScore = 4};
        ModComponetSolverInformation["KeypadComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "KeypadComponentSolver", moduleDisplayName = "Keypad", helpText = "!{0} press 3 1 2 4 | The buttons are 1=TL, 2=TR, 3=BL, 4=BR", manualCode = "Keypads", moduleScore = 1};
        ModComponetSolverInformation["InvisibleWallsComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "InvisibleWallsComponentSolver", moduleDisplayName = "Maze", helpText = "!{0} move up down left right, !{0} move udlr [make a series of white icon moves]", manualCode = "Mazes", moduleScore = 2};


        //Translated Modules - Pre-emptively added.
        ModComponetSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "BigButtonTranslated", moduleDisplayName = "Big Button Translated", helpText = "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]", moduleScore = 4 };
        ModComponetSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "MorseCodeTranslated", moduleDisplayName = "Morse Code Translated", helpText = "!{0} transmit 3.573, !{0} trans 573, !{0} tx 573 [transmit frequency 3.573]", moduleScore = 4 };
        ModComponetSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "PasswordsTranslated", moduleDisplayName = "Password Translated", helpText = "!{0} cycle 3 [cycle through the letters in column 3] | !{0} world [try to submit a word]", moduleScore = 4 };
        ModComponetSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "WhosOnFirstTranslated", moduleDisplayName = "Who's on First Translated", helpText = "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive", moduleScore = 4 };
        ModComponetSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleID = "VentGasTranslated", moduleDisplayName = "Needy Vent Gas Translated", helpText = "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]" };



        //Modded Modules not built into Twitch Plays
        ModComponetSolverInformation["spwizAdventureGame"] = new ModuleInformation { moduleScore = 10, helpText = "Cycle the stats with !{0} cycle stats.  Cycle the Weapons/Items with !{0} cycle items. Use weapons/Items with !{0} use potion. (spell out the item name completely. not case sensitive)"};
        ModComponetSolverInformation["AdjacentLettersModule"] = new ModuleInformation { moduleScore = 12, helpText = "Set the Letters with !{0} set W D J S.  (warning, this will unset ALL letters not specified.)  Submit your answer with !{0} submit." };
        ModComponetSolverInformation["AnagramsModule"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["spwizAstrology"] = new ModuleInformation { moduleScore = 7, helpText = "Press good on 3 with !{0} press good on 3.  Press bad on 2 with !{0} press bad on 2. No Omen is !{0} press no"};
        ModComponetSolverInformation["BattleshipModule"] = new ModuleInformation { moduleScore = 12, helpText = "Scan the safe spots with !{0} scan A2 B3 E5. Mark the spots as water with !{0} miss A1 A3 B4.  Mark the spots as ships with !{0} hit E3 E4. Fill in the rows with !{0} row 3 4. Fill in columns with !{0} col B D"};
        ModComponetSolverInformation["BigCircle"] = new ModuleInformation {moduleScore = 6};
        ModComponetSolverInformation["BitmapsModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit the correct answer with !{0} press 2."};
        ModComponetSolverInformation["BitOps"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct answer with !{0} submit 10101010.", manualCode = "Bitwise Operators", validCommands = new[] { "^submit [0-1]{8}$" } };
        ModComponetSolverInformation["BlindAlleyModule"] = new ModuleInformation { moduleScore = 6, helpText = "Hit the correct spots with !{0} press bl mm tm tl.  (Locations are tl, tm, ml, mm, mr, bl, bm, br)"};
        ModComponetSolverInformation["booleanVennModule"] = new ModuleInformation { moduleScore = 12, helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none)."};
        ModComponetSolverInformation["BrokenButtonsModule"] = new ModuleInformation { moduleScore = 10, helpText = "Press the button by name with !{0} press \"this\".  Press the button in column 2 row 3 with !{0} press 2 3. Press the right submit button with !{0} submit right."};
        ModComponetSolverInformation["CaesarCipherModule"] = new ModuleInformation { moduleScore = 3, helpText = "Press the correct cipher text with !{0} press K B Q I S."};
        ModComponetSolverInformation["CheapCheckoutModule"] = new ModuleInformation { moduleScore = 12, helpText = "Cycle the items with !{0} items. Get customers to pay the correct amount with !{0} submit.  Return the proper change with !{0} submit 3.24."};
        ModComponetSolverInformation["ChessModule"] = new ModuleInformation { moduleScore = 9, helpText = "Cycle the positions with !{0} cycle.  Submit the safe spot with !{0} press C2."};
        ModComponetSolverInformation["ColourFlash"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", manualCode = "Color Flash" };
        ModComponetSolverInformation["colormath"] = new ModuleInformation { moduleScore = 9, helpText = "Set the correct number with !{0} set a,k,m,y.  Submit your set answer with !{0} submit. colors are Red, Orange, Yellow, Green, Blue, Purple, Magenta, White, grAy, blackK. (note what letter is capitalized in each color.)"};
        ModComponetSolverInformation["ColoredSquaresModule"] = new ModuleInformation { moduleScore = 7, helpText = "Press the desired squares with !{0} red, !{0} green, !{0} blue, !{0} yellow, !{0} magenta, !{0} row, or !{0} col."};
        ModComponetSolverInformation["ColoredSwitchesModule"] = new ModuleInformation {moduleScore = 9};
        ModComponetSolverInformation["combinationLock"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["complicatedButtonsModule"] = new ModuleInformation { moduleScore = 6, helpText = "Press the top button with !{0} press top (also t, 1, etc.)."};
        ModComponetSolverInformation["graphModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR."}; // Connection Check
        ModComponetSolverInformation["CoordinatesModule"] = new ModuleInformation { moduleScore = 15, helpText = "Cycle the options with !{0} cycle.  Submit your answer with !{0} submit <3,2>.  Partial answers are acceptable. To do chinese numbers, its !{0} submit chinese 12."};
        ModComponetSolverInformation["CreationModule"] = new ModuleInformation { moduleScore = 12, helpText = "Combine two elements with !{0} combine water fire."};
        ModComponetSolverInformation["CruelPianoKeys"] = new ModuleInformation {moduleScore = 15};
        ModComponetSolverInformation["DoubleOhModule"] = new ModuleInformation { moduleScore = 8, helpText = "Cycle the buttons with !{0} cycle. (Cycle presses each button 3 times, in the order of vert1, horiz1, horiz2, vert2, submit.)  Submit your answer with !{0} press vert1 horiz1 horiz2 vert2 submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false};
        ModComponetSolverInformation["EdgeworkModule"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["Emoji Math"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["EnglishTest"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["fastMath"] = new ModuleInformation { moduleScore = 12, helpText = "Start the timer with !{0} go. Submit an answer with !{0} submit 12."};
        ModComponetSolverInformation["Filibuster"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["fizzBuzzModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press the top button with !{0} press top (also t, 1, etc.). Submit with !{0} press submit."};
        ModComponetSolverInformation["FollowTheLeaderModule"] = new ModuleInformation { moduleScore = 10, helpText = "Cut the wires in the order specified with !{0} cut 12 10 8 7 6 5 3 1. (note that order was the Lit CLR rule.)"};
        ModComponetSolverInformation["FriendshipModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit the desired friendship element with !{0} submit Fairness Conscientiousness Kindness Authenticity."};
        ModComponetSolverInformation["GridlockModule"] = new ModuleInformation {moduleScore = 12};
        ModComponetSolverInformation["HexamazeModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move towards the exit with !{0} move 12 10 6 6 6 2, or with !{0} move N NW S S S NE.  (clockface or cardinal)"};
        ModComponetSolverInformation["http"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the response with !{0} resp 123."};
        ModComponetSolverInformation["iceCreamModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move left/right with !{0} left and !{0} right. Sell with !{0} sell."};
        ModComponetSolverInformation["Laundry"] = new ModuleInformation { moduleScore = 15, helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning.  Set just washing with !{0} set wash 40C.  Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa"};
        ModComponetSolverInformation["LEDEnc"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button with label B with !{0} press b."};
        ModComponetSolverInformation["LetterKeys"] = new ModuleInformation { moduleScore = 3};
        ModComponetSolverInformation["LightCycleModule"] = new ModuleInformation { moduleScore = 12, helpText = "Submit your answer with !{0} B R W M G Y. (note, this module WILL try to input any answer you put into it.)"};
        ModComponetSolverInformation["LightsOut"] = new ModuleInformation { moduleScore = 5, helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right."};
        ModComponetSolverInformation["Logic"] = new ModuleInformation { moduleScore = 12, helpText = "Logic is answered with !{0} submit F T."};
        ModComponetSolverInformation["MazeV2"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["MinesweeperModule"] = new ModuleInformation { moduleScore = 20, helpText = "Clear the initial colour with !{0} dig blue. Clear the square on column 1 row 2 with !{0} dig 1 2. Flag the square on column 3 row 4 with !{0} flag 3 4. Separate multiple squares with a semicolon to interact with all of them."};
        ModComponetSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { moduleScore = 8, helpText = "Reset the module with !{0} press reset.  Move the black card +2 with !{0} move black 2.  Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false};
        ModComponetSolverInformation["monsplodeFight"] = new ModuleInformation { moduleScore = 10, helpText = "Use a move with !{0} use splash."};
        ModComponetSolverInformation["monsplodeWho"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["MorseAMaze"] = new ModuleInformation {moduleScore = 12};
        ModComponetSolverInformation["MorseV2"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["MouseInTheMaze"] = new ModuleInformation { moduleScore = 20, helpText = "Move with !{0} forward back. Turn with !{0} left right u-turn. The first letter only can be used instead. Submit with !{0} submit."};
        ModComponetSolverInformation["murder"] = new ModuleInformation { moduleScore = 10, helpText = ""};
        ModComponetSolverInformation["MusicRhythms"] = new ModuleInformation { moduleScore = 9};
        ModComponetSolverInformation["MysticSquareModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move the numbers around with !{0} press 1 3 2 1 3 4 6 8."};
        ModComponetSolverInformation["Needy Math"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["neutralization"] = new ModuleInformation { moduleScore = 12, helpText = "Select a base with !{0} base NaOH. Turn the filter on/off with !{0} filter. Set drop count with !{0} conc set 48. Submit with !{0} titrate."};
        ModComponetSolverInformation["OnlyConnectModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press a button by position with !{0} press tm or !{0} press 2. Round 1 also accepts symbol names (e.g. reeds, eye, flax, lion, water, viper)."};
        ModComponetSolverInformation["spwizPerspectivePegs"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["PianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb."};
        ModComponetSolverInformation["PointOfOrderModule"] = new ModuleInformation {moduleScore = 7};
        ModComponetSolverInformation["RockPaperScissorsLizardSpockModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press scissors lizard.", manualCode = "Rock-Paper-Scissors-Lizard-Spock" };
        ModComponetSolverInformation["RubiksCubeModule"] = new ModuleInformation { moduleScore = 15, helpText = "View the colors on all sides with !{0} rotate. Reset the cube to starting state with !{0} reset. Solve the Cube with !{0} r' d u f' r' d' u b' u' f", manualCode = "Rubik%E2%80%99s Cube", validCommands = new[] { "^reset$", "^rotate$", "(?>[fbudlr]['2]?)(?> [fbudlr]['2]?)*$" } };
        ModComponetSolverInformation["screw"] = new ModuleInformation { moduleScore = 9, helpText = "Screw with !{0} screw tr or !{0} screw 3. Options are TL, TM, TR, BL, BM, BR. Press a button with !{0} press b or !{0} press 2."};
        ModComponetSolverInformation["Semaphore"] = new ModuleInformation { moduleScore = 7, helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left.  Submit with !{0} press ok."};
        ModComponetSolverInformation["SillySlots"] = new ModuleInformation { moduleScore = 15, helpText = "Keep the slots with !{0} keep.  Pull the slots with !{0} pull."};
        ModComponetSolverInformation["SimonScreamsModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press the correct colors for each round with !{0} press B O Y."};
        ModComponetSolverInformation["SkewedSlotsModule"] = new ModuleInformation { moduleScore = 12, helpText = "Submit the correct response with !{0} submit 1 2 3."};
        ModComponetSolverInformation["SouvenirModule"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the correct response with !{0} answer 3. Order is from top to bottom, then left to right.", CameraPinningAlwaysAllowed = true};
        ModComponetSolverInformation["symbolicPasswordModule"] = new ModuleInformation { moduleScore = 9, helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!"};
        ModComponetSolverInformation["spwizTetris"] = new ModuleInformation { moduleScore = 5};
        ModComponetSolverInformation["TextField"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button in Row 2 column 3 and Row 3 Column 4 with !{0} press 3,2 4,3."};
        ModComponetSolverInformation["TicTacToeModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press a button with !{0} tl. Buttons are tl, tm, tr, ml, mm, mr, bl, bm, br.", manualCode = "Tic-Tac-Toe" };
        ModComponetSolverInformation["TheBulbModule"] = new ModuleInformation { moduleScore = 7, helpText = "Press O with !{0} press O.  Press I with !{0} press I. Unscrew the bulb with !{0} unscrew.  Screw in the bulb with !{0} screw."};
        ModComponetSolverInformation["TheClockModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit a time with !{0} set 12:34 am. Command must include a 12-hour time followed by AM/PM."};
        ModComponetSolverInformation["TheGamepadModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit your answer with !{0} submit l r u d a b."};
        ModComponetSolverInformation["ThirdBase"] = new ModuleInformation { moduleScore = 5, helpText = "Press a button with !{0} z0s8. Word must match the button as it would appear if the module was the right way up. Not case sensitive."};
        ModComponetSolverInformation["webDesign"] = new ModuleInformation { moduleScore = 9, helpText = "Accept the design with !{0} acc.  Consider the design with !{0} con.  Reject the design with !{0} reject."};
        ModComponetSolverInformation["WirePlacementModule"] = new ModuleInformation { moduleScore = 6, helpText = "Cut the correct wires with !{0} cut A2 B4 D3."};
        ModComponetSolverInformation["WordScrambleModule"] = new ModuleInformation { moduleScore = 5, helpText = ""};
        ModComponetSolverInformation["WordSearchModule"] = new ModuleInformation { moduleScore = 6, helpText = "Select the word starting at column B row 3, and ending at column C row 4, with !{0} select B3 C4."};
        ModComponetSolverInformation["XRayModule"] = new ModuleInformation {moduleScore = 12};
        ModComponetSolverInformation["YahtzeeModule"] = new ModuleInformation { moduleScore = 9, helpText = "Roll the dice with !{0} roll. Keep some dice with !{0} keep white,purple,blue,yellow,black. Roll the remaining dice until a 3 appears with !{0} roll until 3."};
        ModComponetSolverInformation["ZooModule"] = new ModuleInformation {moduleScore = 9};

        foreach (string key in ModComponetSolverInformation.Keys)
            ModComponetSolverInformation[key].moduleID = key;
    }

    public static ModuleInformation GetModuleInfo(string moduleType)
    {
        if (!ModComponetSolverInformation.ContainsKey(moduleType))
        {
            ModComponetSolverInformation[moduleType] = new ModuleInformation();
        }
        ModComponetSolverInformation[moduleType].moduleID = moduleType;
        return ModComponetSolverInformation[moduleType];
    }

    public static ModuleInformation[] GetModuleInformation()
    {
        return ModComponetSolverInformation.Values.ToArray();
    }

    public static void AddModuleInformation(ModuleInformation info)
    {
        if (info.moduleID == null) return;
        if (ModComponetSolverInformation.ContainsKey(info.moduleID))
        {
            ModuleInformation i = ModComponetSolverInformation[info.moduleID];
            if (i == null)
            {
                ModComponetSolverInformation[info.moduleID] = info;
                return;
            }
            i.moduleID = info.moduleID;

            if (!string.IsNullOrEmpty(info.moduleDisplayName))
                i.moduleDisplayName = info.moduleDisplayName;

            if (!string.IsNullOrEmpty(info.helpText) || info.helpTextOverride)
                i.helpText = info.helpText;

            if (!string.IsNullOrEmpty(info.manualCode) || info.manualCodeOverride)
                i.helpText = info.manualCode;

            i.DoesTheRightThing = info.DoesTheRightThing;
            i.statusLightLeft = info.statusLightLeft;
            i.statusLightDown = info.statusLightDown;
            i.chatRotation = info.chatRotation;
            i.validCommands = info.validCommands;

            i.helpTextOverride = info.helpTextOverride;
            i.manualCodeOverride = info.manualCodeOverride;
            i.statusLightOverride = info.statusLightOverride;
            i.validCommandsOverride = info.validCommandsOverride;

            i.moduleScore = info.moduleScore;
            i.moduleScoreIsDynamic = info.moduleScoreIsDynamic;
        }
        else
        {
            ModComponetSolverInformation[info.moduleID] = info;
        }
    }

    public static ComponentSolver CreateSolver(BombCommander bombCommander, MonoBehaviour bombComponent, ComponentTypeEnum componentType, IRCConnection ircConnection, CoroutineCanceller canceller)
    {
        switch (componentType)
        {
            case ComponentTypeEnum.Wires:
                return new WireSetComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Keypad:
                return new KeypadComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.BigButton:
                return new ButtonComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Memory:
                return new MemoryComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Simon:
                return new SimonComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Venn:
                return new VennWireComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Morse:
                return new MorseCodeComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.WireSequence:
                return new WireSequenceComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Password:
                return new PasswordComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Maze:
                return new InvisibleWallsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.WhosOnFirst:
                return new WhosOnFirstComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.NeedyVentGas:
                return new NeedyVentComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.NeedyCapacitor:
                return new NeedyDischargeComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.NeedyKnob:
                return new NeedyKnobComponentSolver(bombCommander, bombComponent, ircConnection, canceller);

            case ComponentTypeEnum.Mod:
                KMBombModule solvableModule = bombComponent.GetComponent<KMBombModule>();
                return CreateModComponentSolver(bombCommander, bombComponent, ircConnection, canceller, solvableModule.ModuleType, solvableModule.ModuleDisplayName);                

            case ComponentTypeEnum.NeedyMod:
                KMNeedyModule needyModule = bombComponent.GetComponent<KMNeedyModule>();
                return CreateModComponentSolver(bombCommander, bombComponent, ircConnection, canceller, needyModule.ModuleType, needyModule.ModuleDisplayName);

            default:
                throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays'.", (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(bombComponent, null)));
        }
    }

    private static ComponentSolver CreateModComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller, string moduleType, string displayName)
    {
        if (ModComponentSolverCreators.ContainsKey(moduleType))
        {
            ComponentSolver solver = ModComponentSolverCreators[moduleType](bombCommander, bombComponent, ircConnection, canceller);
            return solver;
        }

        Debug.LogFormat("Attempting to find a valid process command method to respond with on component {0}...", moduleType);

        ModComponentSolverDelegate modComponentSolverCreator = GenerateModComponentSolverCreator(bombComponent, moduleType, displayName);
        if (modComponentSolverCreator == null)
        {
            throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays' - Could not generate a valid componentsolver for the mod component!", (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(bombComponent, null)));
        }

        ModComponentSolverCreators[moduleType] = modComponentSolverCreator;

        return modComponentSolverCreator(bombCommander, bombComponent, ircConnection, canceller);
    }

    private static ModComponentSolverDelegate GenerateModComponentSolverCreator(MonoBehaviour bombComponent, string moduleType, string displayName)
    {
        ModCommandType commandType = ModCommandType.Simple;
        Type commandComponentType = null;
        MethodInfo method = FindProcessCommandMethod(bombComponent, out commandType, out commandComponentType);
        string help;
        string manual;
        bool statusLeft;
        bool statusBottom;
        float rotation;
        string[] regexList;

        ModuleInformation info = GetModuleInfo(moduleType);
        if (!info.helpTextOverride && FindHelpMessage(bombComponent, out help))
        {
            if (help != null)
                ModuleData.DataHasChanged |= !help.Equals(info.helpText);
            else
                ModuleData.DataHasChanged |= info.helpText != null;
            info.helpText = help;
        }

        if (!info.manualCodeOverride && FindManualCode(bombComponent, out manual))
        {
            if (manual != null)
                ModuleData.DataHasChanged |= !manual.Equals(info.manualCode);
            else
                ModuleData.DataHasChanged |= info.manualCode != null;
            info.manualCode = manual;
        }

        if (!info.statusLightOverride && FindStatusLightPosition(bombComponent, out statusLeft, out statusBottom, out rotation))
        {
            ModuleData.DataHasChanged |= info.statusLightLeft != statusLeft;
            ModuleData.DataHasChanged |= info.statusLightDown != statusBottom;
            ModuleData.DataHasChanged |= (Mathf.Abs(info.chatRotation - rotation) >= 0.2f);
            info.statusLightLeft = statusLeft;
            info.statusLightDown = statusBottom;
            info.chatRotation = rotation;
        }

        if (!info.validCommandsOverride && FindRegexList(bombComponent, out regexList))
        {
            if (info.validCommands != null && regexList == null)
                ModuleData.DataHasChanged = true;
            else if (info.validCommands == null && regexList != null)
                ModuleData.DataHasChanged = true;
            else if (info.validCommands != null && regexList != null)
            {
                if (info.validCommands.Length != regexList.Length)
                    ModuleData.DataHasChanged = true;
                else
                {
                    for (int i = 0; i < regexList.Length; i++)
                        ModuleData.DataHasChanged |= !info.validCommands[i].Equals(regexList[i]);
                }
            }
            info.validCommands = regexList;
        }

        if (displayName != null)
            ModuleData.DataHasChanged |= !displayName.Equals(info.moduleDisplayName);
        else
            ModuleData.DataHasChanged |= info.moduleID != null;

        info.moduleDisplayName = displayName;
        ModuleData.WriteDataToFile();

        if (method != null)
        {
            switch (commandType)
            {
                case ModCommandType.Simple:
                    return delegate (BombCommander _bombCommander, MonoBehaviour _bombComponent, IRCConnection _ircConnection, CoroutineCanceller _canceller)
                    {
                        Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
                        return new SimpleModComponentSolver(_bombCommander, _bombComponent, _ircConnection, _canceller, method, commandComponent);
                    };
                case ModCommandType.Coroutine:
                    FieldInfo cancelfield;
                    Type canceltype;
                    FindCancelBool(bombComponent, out cancelfield, out canceltype);
                    return delegate (BombCommander _bombCommander, MonoBehaviour _bombComponent, IRCConnection _ircConnection, CoroutineCanceller _canceller)
                    {
                        Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
                        return new CoroutineModComponentSolver(_bombCommander, _bombComponent, _ircConnection, _canceller, method, commandComponent, cancelfield, canceltype);
                    };

                default:
                    break;
            }
        }

        return null;
    }

    private static bool FindStatusLightPosition(MonoBehaviour bombComponent, out bool StatusLightLeft, out bool StatusLightBottom, out float Rotation)
    {
        Debug.Log("[TwitchPlays] Attempting to find the modules StatusLightParent");
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            if(type == ReflectionHelper.FindType("StatusLightParent"))
            {
                Debug.LogFormat("Local Position - X = {0}, Y = {1}, Z = {2}", component.transform.localPosition.x, component.transform.localPosition.y, component.transform.localPosition.z);
                Debug.LogFormat("Local Euler Angles - X = {0}, Y = {1}, Z = {2}", component.transform.localEulerAngles.x, component.transform.localEulerAngles.y, component.transform.localEulerAngles.z);
                StatusLightLeft = (component.transform.localPosition.x < 0);
                StatusLightBottom = (component.transform.localPosition.z < 0);
                Rotation = component.transform.localEulerAngles.y;
                return true;
            }
        }
        Debug.Log("StatusLightParent not found :(");
        StatusLightLeft = false;
        StatusLightBottom = false;
        Rotation = 0;
        return false;
    }

    private static bool FindRegexList(MonoBehaviour bombComponent, out string[] validCommands)
    {
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            //Debug.LogFormat("[TwitchPlays] component.GetType(): FullName = {0}, Name = {1}",type.FullName, type.Name);
            FieldInfo candidateString = type.GetField("TwitchValidCommands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (candidateString == null)
            {
                continue;
            }
            if (candidateString.GetValue(bombComponent.GetComponent(type)) is string[])
            {
                validCommands = (string[]) candidateString.GetValue(bombComponent.GetComponent(type));
                return true;
            }
        }
        validCommands = null;
        return false;
    }

    private static bool FindManualCode(MonoBehaviour bombComponent, out string manualCode)
    {
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            //Debug.LogFormat("[TwitchPlays] component.GetType(): FullName = {0}, Name = {1}",type.FullName, type.Name);
            FieldInfo candidateString = type.GetField("TwitchManualCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (candidateString == null)
            {
                continue;
            }
            if (candidateString.GetValue(bombComponent.GetComponent(type)) is string)
            {
                manualCode = (string) candidateString.GetValue(bombComponent.GetComponent(type));
                return true;
            }
        }
        manualCode = null;
        return false;
    }

    private static bool FindHelpMessage(MonoBehaviour bombComponent, out string helpText)
    {
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            FieldInfo candidateString = type.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (candidateString == null)
            {
                continue;
            }
            if (candidateString.GetValue(bombComponent.GetComponent(type)) is string)
            {
                helpText = (string) candidateString.GetValue(bombComponent.GetComponent(type));
                return true;
            }
        }
        helpText = null;
        return false;
    }

    private static bool FindCancelBool(MonoBehaviour bombComponent, out FieldInfo CancelField, out Type CancelType)
    {
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            FieldInfo candidateBoolField = type.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (candidateBoolField == null)
            {
                continue;
            }
            if (candidateBoolField.GetValue(bombComponent.GetComponent(type)) is bool)
            {
                CancelField = candidateBoolField;
                CancelType = type;
                return true;
            }
        }
        CancelField = null;
        CancelType = null;
        return false;
    }

    private static MethodInfo FindProcessCommandMethod(MonoBehaviour bombComponent, out ModCommandType commandType, out Type commandComponentType)
    {
        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            Type type = component.GetType();
            MethodInfo candidateMethod = type.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (candidateMethod == null)
            {
                continue;
            }

            if (ValidateMethodCommandMethod(type, candidateMethod, out commandType))
            {
                commandComponentType = type;
                return candidateMethod;
            }
        }

        commandType = ModCommandType.Simple;
        commandComponentType = null;
        return null;
    }

    private static bool ValidateMethodCommandMethod(Type type, MethodInfo candidateMethod, out ModCommandType commandType)
    {
        commandType = ModCommandType.Simple;

        ParameterInfo[] parameters = candidateMethod.GetParameters();
        if (parameters == null || parameters.Length == 0)
        {
            Debug.LogFormat("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too few parameters).", type.FullName);
            return false;
        }

        if (parameters.Length > 1)
        {
            Debug.LogFormat("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too many parameters).", type.FullName);
            return false;
        }

        if (parameters[0].ParameterType != typeof(string))
        {
            Debug.LogFormat("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (expected a single string parameter, got a single {1} parameter).", type.FullName, parameters[0].ParameterType.FullName);
            return false;
        }

        if (candidateMethod.ReturnType == typeof(KMSelectable[]))
        {
            Debug.LogFormat("Found a valid candidate ProcessCommand method in {0} (using easy/simple API).", type.FullName);
            commandType = ModCommandType.Simple;
            return true;
        }

        if (candidateMethod.ReturnType == typeof(IEnumerator))
        {
            Debug.LogFormat("Found a valid candidate ProcessCommand method in {0} (using advanced/coroutine API).", type.FullName);
            commandType = ModCommandType.Coroutine;
            return true;
        }

        return false;
    }
}

