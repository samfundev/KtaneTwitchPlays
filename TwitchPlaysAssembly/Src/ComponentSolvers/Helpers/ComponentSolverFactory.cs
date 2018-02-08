using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Components.VennWire;
using Assets.Scripts.Missions;
using UnityEngine;

public static class ComponentSolverFactory
{
	public static bool SilentMode = false;
	private static void DebugLog(string format, params object[] args)
	{
		if (SilentMode) return;
		DebugHelper.Log(format, args);
	}

	private delegate ComponentSolver ModComponentSolverDelegate(BombCommander bombCommander, BombComponent bombComponent);
	private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreators;
    private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreatorShims;
	private static readonly Dictionary<string, ModuleInformation> ModComponentSolverInformation;

	static ComponentSolverFactory()
	{
		DebugHelper.Log();
		ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
        ModComponentSolverCreatorShims = new Dictionary<string, ModComponentSolverDelegate>();
		ModComponentSolverInformation = new Dictionary<string, ModuleInformation>();

		//AT_Bash Modules
		ModComponentSolverCreators["MotionSense"] = (bombCommander, bombComponent) => new MotionSenseComponentSolver(bombCommander, bombComponent);

		//Hexi Modules
		ModComponentSolverCreators["MemoryV2"] = (bombCommander, bombComponent) => new ForgetMeNotComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["KeypadV2"] = (bombCommander, bombComponent) => new RoundKeypadComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["ButtonV2"] = (bombCommander, bombComponent) => new SquareButtonComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["SimonV2"] = (bombCommander, bombComponent) => new SimonStatesComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["PasswordV2"] = (bombCommander, bombComponent) => new SafetySafeComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["MazeV2"] = (bombCommander, bombComponent) => new PlumbingComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["MorseV2"] = (bombCommander, bombComponent) => new MorsematicsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["NeedyVentV2"] = (bombCommander, bombComponent) => new NeedyQuizComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["NeedyKnobV2"] = (bombCommander, bombComponent) => new NeedyRotaryPhoneComponentSolver(bombCommander, bombComponent);

		//Perky Modules (Silly Slots is maintained by Timwi, and as such its handler lives there.)
		ModComponentSolverCreators["CrazyTalk"] = (bombCommander, bombComponent) => new CrazyTalkComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["CryptModule"] = (bombCommander, bombComponent) => new CryptographyComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["ForeignExchangeRates"] = (bombCommander, bombComponent) => new ForeignExchangeRatesComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Listening"] = (bombCommander, bombComponent) => new ListeningComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["OrientationCube"] = (bombCommander, bombComponent) => new OrientationCubeComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Probing"] = (bombCommander, bombComponent) => new ProbingComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["TurnTheKey"] = (bombCommander, bombComponent) => new TurnTheKeyComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["TurnTheKeyAdvanced"] = (bombCommander, bombComponent) => new TurnTheKeyAdvancedComponentSolver(bombCommander, bombComponent);

		//Kaneb Modules
		ModComponentSolverCreators["TwoBits"] = (bombCommander, bombComponent) => new TwoBitsComponentSolver(bombCommander, bombComponent);

		//Asimir Modules
		ModComponentSolverCreators["murder"] = (bombCommander, bombComponent) => new MurderComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["SeaShells"] = (bombCommander, bombComponent) => new SeaShellsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["shapeshift"] = (bombCommander, bombComponent) => new ShapeShiftComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["ThirdBase"] = (bombCommander, bombComponent) => new ThirdBaseComponentSolver(bombCommander, bombComponent);

		//Spare Wizard Modules
		ModComponentSolverCreators["spwiz3DMaze"] = (bombCommander, bombComponent) => new ThreeDMazeComponentSolver(bombCommander, bombComponent);

		//Mock Army Modules
		ModComponentSolverCreators["AnagramsModule"] = (bombCommander, bombComponent) => new AnagramsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Emoji Math"] = (bombCommander, bombComponent) => new EmojiMathComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["WordScrambleModule"] = (bombCommander, bombComponent) => new AnagramsComponentSolver(bombCommander, bombComponent);

		//Misc Modules
		ModComponentSolverCreators["alphabet"] = (bombCommander, bombComponent) => new AlphabetComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["ChordQualities"] = (bombCommander, bombComponent) => new ChordQualitiesComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["ColorMorseModule"] = (bombCommander, bombComponent) => new ColorMorseComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["EnglishTest"] = (bombCommander, bombComponent) => new EnglishTestComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["LetterKeys"] = (bombCommander, bombComponent) => new LetteredKeysComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Microcontroller"] = (bombCommander, bombComponent) => new MicrocontrollerComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["NumberPad"] = (bombCommander, bombComponent) => new NumberPadComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["resistors"] = (bombCommander, bombComponent) => new ResistorsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["switchModule"] = (bombCommander, bombComponent) => new SwitchesComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["curriculum"] = (bombCommander, bombComponent) => new CurriculumComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["EdgeworkModule"] = (bombCommander, bombComponent) => new EdgeworkComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["NeedyBeer"] = (bombCommander, bombComponent) => new NeedyBeerComponentSolver(bombCommander, bombComponent);

		//Translated Modules
		ModComponentSolverCreators["BigButtonTranslated"] = (bombCommander, bombComponent) => new TranslatedButtonComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["MorseCodeTranslated"] = (bombCommander, bombComponent) => new TranslatedMorseCodeComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["PasswordsTranslated"] = (bombCommander, bombComponent) => new TranslatedPasswordComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["WhosOnFirstTranslated"] = (bombCommander, bombComponent) => new TranslatedWhosOnFirstComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["VentGasTranslated"] = (bombCommander, bombComponent) => new TranslatedNeedyVentComponentSolver(bombCommander, bombComponent);

        //Shim added - This overrides at least one specific command or formatting, then passes on control to ProcessTwitchCommand in all other cases. (Or in some cases, enforce unsubmittable penalty)
	    ModComponentSolverCreatorShims["ExtendedPassword"] = (bombCommander, bombComponent) => new ExtendedPasswordComponentSolver(bombCommander, bombComponent);
        
		//Module Information
		//Information declared here will be used to generate ModuleInformation.json if it doesn't already exist, and will be overwritten by ModuleInformation.json if it does exist.
		/*
         * 
            Typical ModuleInformation json entry
            {
                "moduleDisplayName": "Double-Oh",
                "moduleID": "DoubleOhModule",
                "moduleScore": 8,
                "strikePenalty": -6,
                "moduleScoreIsDynamic": false,
                "helpTextOverride": false,
                "helpText": "Cycle the buttons with !{0} cycle. (Cycle presses each button 3 times, in the order of vert1, horiz1, horiz2, vert2, submit.) Submit your answer with !{0} press vert1 horiz1 horiz2 vert2 submit.",
                "manualCodeOverride": false,
                "manualCode": null,
                "statusLightOverride": true,
                "statusLightLeft": false,
                "statusLightDown": false,
                "chatRotation": 0.0,
                "validCommandsOverride": false,
                "validCommands": null,
                "DoesTheRightThing": false,
                "CameraPinningAlwaysAllowed": false
            },
         * 
         * moduleDisplayName - The name of the module as displayed in Mod Selector or the chat box.
         * moduleID - The unique identifier of the module.
         * 
         * moduleScore - The number of points the module will award the defuser on solve
         * strikePenalty - The number of points the module will take away from the defuser on a strike.
         * moduleScoreIsDynamic - Only used in limited cases. If true, moduleScore will define the scoring rules that apply.
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
         * CameraPinningAlwaysAllowed - Defines if a normal user is allowed to use view pin on this module.
         * 
         * 
         */

		//All of these modules are built into Twitch plays.

		//Asimir
		ModComponentSolverInformation["murder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Murder", moduleScore = 10, helpText = "CycleCycle the options with !{0} cycle or !{0} cycle people (also weapons and rooms). Make an accusation with !{0} It was Peacock, with the candlestick, in the kitchen. Or you can set the options individually, and accuse with !{0} accuse." };
		ModComponentSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Sea Shells", helpText = "Press buttons by typing !{0} press alar llama. You can submit partial text as long it only matches one button. NOTE: Each button press is separated by a space so typing \"burglar alarm\" will press a button twice.", moduleScore = 7 };
		ModComponentSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Shape Shift", helpText = "Submit your anwser with !{0} submit point round. Reset to initial state with !{0} reset. Valid shapes: flat, point, round and ticket.", moduleScore = 8 };
		ModComponentSolverInformation["ThirdBase"] = new ModuleInformation { builtIntoTwitchPlays = true, statusLightDown = true, statusLightLeft = true, moduleScore = 5, moduleDisplayName = "Third Base", helpText = "Press a button with !{0} z0s8. Word must match the button as it would appear if the module was the right way up. Not case sensitive." };

		//AT_Bash / Bashly
		ModComponentSolverInformation["MotionSense"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Motion Sense", helpText = "I am a passive module that awards strikes for motion while I am active. Use !{0} status to find out if I am active, and for how long." };

		//Hexicube
		ModComponentSolverInformation["MemoryV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Forget Me Not", helpText = "Enter forget me not sequence with !{0} press 5 3 1 8 2 0... The Sequence length depends on how many modules were on the bomb.", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["KeypadV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Round Keypad", helpText = "Solve the module with !{0} press 2 4 6 7 8. Button 1 is the top most botton, and are numbered in clockwise order.", moduleScore = 6 };
		ModComponentSolverInformation["ButtonV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Square Button", helpText = "Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.", moduleScore = 6 };
		ModComponentSolverInformation["SimonV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon States", helpText = "Enter the response with !{0} press B Y R G.", moduleScore = 8 };
		ModComponentSolverInformation["PasswordV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Safety Safe", helpText = "Listen to the dials with !{0} cycle. Listen to a single dial with !{0} cycle BR. Make a correction to a single dial with !{0} BM 3. Enter the solution with !{0} 6 0 6 8 2 5. Submit the answer with !{0} submit. Dial positions are TL, TM, TR, BL, BM, BR.", moduleScore = 15 };
		ModComponentSolverInformation["MazeV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Plumbing", helpText = "Rotate the pipes with !{0} rotate A1 A1 B2 B3 C2 C3 C3. Check your work for leaks Kappa with !{0} submit. (Pipes rotate clockwise. Top left is A1, Bottom right is F6)", moduleScore = 20 };
		ModComponentSolverInformation["MorseV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morsematics", helpText = "Turn the lights off with !{0} lights off. Turn the lights on with !{0} lights on. Tranmit the answer with !{0} transmit -..-", moduleScore = 12 };
		ModComponentSolverInformation["NeedyVentV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Answering Questions", helpText = "Answer the question with !{0} Y or !{0} N.", manualCode = "Answering%20Questions" };
		ModComponentSolverInformation["NeedyKnobV2"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Rotary Phone", helpText = "Respond to the phone call with !{0} press 8 4 9.", manualCode = "Rotary%20Phone" };

		//Perky
		ModComponentSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Crazy Talk", helpText = "Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.", moduleScore = 3 };
		ModComponentSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptography", helpText = "Solve the cryptography puzzle with !{0} press N B V T K.", moduleScore = 9 };
		ModComponentSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Foreign Exchange Rates", helpText = "Solve the module with !{0} press ML. Positions are TL, TM, TR, ML, MM, MR, BL, BM, BR.", moduleScore = 6 };
		ModComponentSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, statusLightLeft = true, moduleDisplayName = "Listening", helpText = "Listen to the sound with !{0} press play. Enter the response with !{0} press $ & * * #.", moduleScore = 8 };
		ModComponentSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Orientation Cube", helpText = "Move the cube with !{0} press cw l set. The buttons are l, r, cw, ccw, set.", moduleScore = 6 };
		ModComponentSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Probing", helpText = "Get the readings with !{0} cycle. Try a combination with !{0} connect 4 3. Cycle reads 1&2, 1&3, 1&4, 1&5, 1&6.", moduleScore = 6 };
		ModComponentSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Key", helpText = "Turn the key at specified time with !{0} turn 8:29", moduleScore = 6 };
		ModComponentSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Keys", helpText = "Turn the left key with !{0} turn left. Turn the right key with !{0} turn right.", moduleScore = 15 };

		//Kaneb
		ModComponentSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Two Bits", helpText = "Query the answer with !{0} press K T query. Submit the answer with !{0} press G Z submit.", moduleScore = 8 };

		//SpareWizard
		ModComponentSolverInformation["spwiz3DMaze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "3D Maze", helpText = "Move around the maze using !{0} move left forward right. Walk slowly around the maze using !{0} walk left forawrd right. Shorten forms of the directions are also acceptable. You can use \"uturn\" or \"u\" to turn around.", moduleScore = 20 };

		//Mock Army
		ModComponentSolverInformation["AnagramsModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Anagrams", statusLightLeft = true, helpText = "Submit your answer with !{0} submit poodle", moduleScore = 1 };
		ModComponentSolverInformation["Emoji Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Emoji Math", helpText = "Submit an answer using !{0} submit -47.", moduleScore = 1 };
		ModComponentSolverInformation["WordScrambleModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Word Scramble", helpText = "Submit your answer with !{0} submit poodle", moduleScore = 1 };

		//Misc
		ModComponentSolverInformation["alphabet"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alphabet", helpText = "Submit your anwser with !{0} press A B C D.", moduleScore = 1 };
		ModComponentSolverInformation["ChordQualities"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Chord Qualities", helpText = "Submit a chord using !{0} submit A B C# D", moduleScore = 9 };
		ModComponentSolverInformation["ColorMorseModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Color Morse", helpText = "Submit some morse code using !{0} transmit ....- --...", moduleScore = 5 };
		ModComponentSolverInformation["EnglishTest"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "English Test", moduleScore = 4, helpText = "Answer the displayed question with !{0} submit 2 or !{0} answer 2. (Answers are numbered from 1-4 starting from left to right.)" };
        ModComponentSolverInformation["LetterKeys"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Lettered Keys", moduleScore = 3, helpText = "!{0} press b" };
		ModComponentSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Microcontroller", helpText = "Set the current pin color with !{0} set red. Cycle the current pin !{0} cycle. Valid colors: white, red, yellow, magenta, blue, green.", moduleScore = 10 };
		ModComponentSolverInformation["NumberPad"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Number Pad", helpText = "Submit your anwser with !{0} submit 4236.", moduleScore = 5 };
		ModComponentSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Resistors", helpText = "Connect sets of two pins with !{0} connect a tl tr c. Use !{0} submit to submit and !{0} clear to clear. Valid pins: A B C D TL TR BL BR. Top and Bottom refer to the top and bottom resistor.", moduleScore = 6 };
		ModComponentSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Switches", helpText = "Flip switches using !{0} flip 1 5 3 2.", moduleScore = 3 };
		ModComponentSolverInformation["curriculum"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Curriculum", helpText = "Cycle the buttons !{0} cycle. Click a button using !{0} click 2. It's possible to add a number of times to click: !{0} click 2 3. Buttons are numbered left to right. Submit your answer with !{0} submit.", moduleScore = 12 };
		ModComponentSolverInformation["EdgeworkModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Edgework", helpText = "Press an answer using !{0} press left. Answers can be referred to numbered from left to right. They can also be referred to by their position." };
		ModComponentSolverInformation["NeedyBeer"] = new ModuleInformation {builtIntoTwitchPlays = true, moduleDisplayName = "Needy Beer Refill Mod", helpText = "Refill that beer with !{0} refill."};

		//Steel Crate Games (Need these in place even for the Vanilla modules)
		ModComponentSolverInformation["WireSetComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simple Wires", helpText = "!{0} cut 3 [cut wire 3] | Wires are ordered from top to bottom | Empty spaces are not counted", moduleScore = 1 };
		ModComponentSolverInformation["ButtonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button", helpText = "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]", moduleScore = 1 };
		ModComponentSolverInformation["WireSequenceComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wire Sequence", helpText = "!{0} cut 7 [cut wire 7] | !{0} down, !{0} d [next stage] | !{0} up, !{0} u [previous stage] | !{0} cut 7 8 9 d [cut multiple wires and continue] | Use the numbers shown on the module", manualCode = "Wire Sequences", moduleScore = 4 };
		ModComponentSolverInformation["WhosOnFirstComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First", helpText = "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive", manualCode = "Who%E2%80%99s on First", moduleScore = 4 };
		ModComponentSolverInformation["VennWireComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Complicated Wires", helpText = "!{0} cut 3 [cut wire 3] | !{0} cut 2 3 6 [cut multiple wires] | Wires are ordered from left to right | Empty spaces are not counted", moduleScore = 3 };
		ModComponentSolverInformation["SimonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Says", helpText = "!{0} press red green blue yellow, !{0} press rgby [press a sequence of colours] | You must include the input from any previous stages", moduleScore = 3 };
		ModComponentSolverInformation["PasswordComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password", helpText = "!{0} cycle 3 [cycle through the letters in column 3] | !{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} world [try to submit a word]", manualCode = "Passwords", moduleScore = 2 };
		ModComponentSolverInformation["NeedyVentComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas", helpText = "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]" };
		ModComponentSolverInformation["NeedyKnobComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Knob", helpText = "!{0} rotate 3, !{0} turn 3 [rotate the knob 3 quarter-turns]", manualCode = "Knobs" };
		ModComponentSolverInformation["NeedyDischargeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Capacitor", helpText = "!{0} hold 7 [hold the lever for 7 seconds]", manualCode = "Capacitor Discharge" };
		ModComponentSolverInformation["MorseCodeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code", helpText = "!{0} transmit 3.573, !{0} trans 573, !{0} tx 573 [transmit frequency 3.573]", moduleScore = 3 };
		ModComponentSolverInformation["MemoryComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memory", helpText = "!{0} position 2, !{0} pos 2, !{0} p 2 [2nd position] | !{0} label 3, !{0} lab 3, !{0} l 3 [label 3]", moduleScore = 4 };
		ModComponentSolverInformation["KeypadComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keypad", helpText = "!{0} press 3 1 2 4 | The buttons are 1=TL, 2=TR, 3=BL, 4=BR", manualCode = "Keypads", moduleScore = 1 };
		ModComponentSolverInformation["InvisibleWallsComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Maze", helpText = "!{0} move up down left right, !{0} move udlr [make a series of white icon moves]", manualCode = "Mazes", moduleScore = 2 };


		//Translated Modules
		ModComponentSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button Translated", helpText = "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7] | (Important - Take note of the strip color on hold, it will change as other translated buttons get held, and the answer retains original color.)", moduleScore = 1 };
		ModComponentSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code Translated", helpText = "!{0} transmit 3.573, !{0} trans 573, !{0} tx 573 [transmit frequency 3.573]", moduleScore = 3 };
		ModComponentSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password Translated", helpText = "!{0} cycle 3 [cycle through the letters in column 3] | !{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} world [try to submit a word]", moduleScore = 2 };
		ModComponentSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First Translated", helpText = "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive| If the language used asks for pressing a literally blank button, use \"!{0} literally blank\"", moduleScore = 4 };
		ModComponentSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas Translated", helpText = "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]" };

        //Shim added in between Twitch Plays and module (This allows overriding a specific command, or for enforcing unsubmittable penalty)
	    ModComponentSolverInformation["ExtendedPassword"] = new ModuleInformation { moduleDisplayName = "Extended Password", moduleScore = 7, helpText = "!{0} cycle 6 [cycle through the letters in column 6] | !{0} lambda [try to submit a word]", DoesTheRightThing = true };
        
		//Modded Modules not built into Twitch Plays
		ModComponentSolverInformation["spwizAdventureGame"] = new ModuleInformation { moduleScore = 10, helpText = "Cycle the stats with !{0} cycle stats. Cycle the Weapons/Items with !{0} cycle items. Use weapons/Items with !{0} use potion. (spell out the item name completely. not case sensitive)", DoesTheRightThing = false };
		ModComponentSolverInformation["AdjacentLettersModule"] = new ModuleInformation { moduleScore = 12, helpText = "Set the Letters with !{0} set W D J S. (warning, this will unset ALL letters not specified.) Submit your answer with !{0} submit." };
		ModComponentSolverInformation["spwizAstrology"] = new ModuleInformation { moduleScore = 7, helpText = "Press good on 3 with !{0} press good on 3. Press bad on 2 with !{0} press bad on 2. No Omen is !{0} press no", DoesTheRightThing = true };
		ModComponentSolverInformation["BattleshipModule"] = new ModuleInformation { moduleScore = 12, helpText = "Scan the safe spots with !{0} scan A2 B3 E5. Mark the spots as water with !{0} miss A1 A3 B4. Mark the spots as ships with !{0} hit E3 E4. Fill in the rows with !{0} row 3 4. Fill in columns with !{0} col B D" };
		ModComponentSolverInformation["BigCircle"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true, moduleDisplayName = "Big Circle" };
		ModComponentSolverInformation["BitmapsModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit the correct answer with !{0} press 2." };
		ModComponentSolverInformation["BitOps"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct answer with !{0} submit 10101010.", manualCode = "Bitwise Operators", validCommands = new[] { "^submit [0-1]{8}$" } };
		ModComponentSolverInformation["BlindAlleyModule"] = new ModuleInformation { moduleScore = 6, helpText = "Hit the correct spots with !{0} press bl mm tm tl. (Locations are tl, tm, ml, mm, mr, bl, bm, br)" };
		ModComponentSolverInformation["booleanVennModule"] = new ModuleInformation { moduleScore = 12, helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none)." };
		ModComponentSolverInformation["BrokenButtonsModule"] = new ModuleInformation { moduleScore = 10, helpText = "Press the button by name with !{0} press \"this\". Press the button in column 2 row 3 with !{0} press 2 3. Press the right submit button with !{0} submit right." };
		ModComponentSolverInformation["CaesarCipherModule"] = new ModuleInformation { moduleScore = 3, helpText = "Press the correct cipher text with !{0} press K B Q I S." };
		ModComponentSolverInformation["CheapCheckoutModule"] = new ModuleInformation { moduleScore = 12, helpText = "Cycle the items with !{0} items. Get customers to pay the correct amount with !{0} submit. Return the proper change with !{0} submit 3.24.", DoesTheRightThing = true };
		ModComponentSolverInformation["ChessModule"] = new ModuleInformation { moduleScore = 9, helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", DoesTheRightThing = false };
		ModComponentSolverInformation["ColourFlash"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", manualCode = "Color Flash", DoesTheRightThing = false };
	    ModComponentSolverInformation["Color Generator"] = new ModuleInformation { moduleDisplayName = "Color Generator", moduleScore = 6, helpText = "Submit a color using !{0} submit 123 123 123.", DoesTheRightThing = true };
        ModComponentSolverInformation["colormath"] = new ModuleInformation { moduleScore = 9, helpText = "Set the correct number with !{0} set a,k,m,y. Submit your set answer with !{0} submit. colors are Red, Orange, Yellow, Green, Blue, Purple, Magenta, White, grAy, blackK. (note what letter is capitalized in each color.)" };
		ModComponentSolverInformation["ColoredSquaresModule"] = new ModuleInformation { moduleScore = 7, helpText = "Press the desired squares with !{0} red, !{0} green, !{0} blue, !{0} yellow, !{0} magenta, !{0} row, or !{0} col." };
		ModComponentSolverInformation["ColoredSwitchesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["combinationLock"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the code using !{0} submit 1 2 3.", DoesTheRightThing = false };
		ModComponentSolverInformation["complicatedButtonsModule"] = new ModuleInformation { moduleScore = 6, helpText = "Press the top button with !{0} press top (also t, 1, etc.)." };
		ModComponentSolverInformation["graphModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR." }; // Connection Check
		ModComponentSolverInformation["CoordinatesModule"] = new ModuleInformation { moduleScore = 15, helpText = "Cycle the options with !{0} cycle. Submit your answer with !{0} submit <3,2>. Partial answers are acceptable. To do chinese numbers, its !{0} submit chinese 12.", DoesTheRightThing = false };
		ModComponentSolverInformation["CreationModule"] = new ModuleInformation { moduleScore = 12, helpText = "Combine two elements with !{0} combine water fire.", DoesTheRightThing = true };
		ModComponentSolverInformation["CruelPianoKeys"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = false };
		ModComponentSolverInformation["DoubleOhModule"] = new ModuleInformation { moduleScore = 8, helpText = "Cycle the buttons with !{0} cycle. (Cycle presses each button 3 times, in the order of vert1, horiz1, horiz2, vert2, submit.) Submit your answer with !{0} press vert1 horiz1 horiz2 vert2 submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false };
		ModComponentSolverInformation["fastMath"] = new ModuleInformation { moduleScore = 12, helpText = "Start the timer with !{0} go. Submit an answer with !{0} submit 12." };
		ModComponentSolverInformation["Filibuster"] = new ModuleInformation { moduleScore = 5, helpText = "" };
		ModComponentSolverInformation["fizzBuzzModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press the top button with !{0} press top (also t, 1, etc.). Submit with !{0} press submit." };
		ModComponentSolverInformation["FollowTheLeaderModule"] = new ModuleInformation { moduleScore = 10, helpText = "Cut the wires in the order specified with !{0} cut 12 10 8 7 6 5 3 1. (note that order was the Lit CLR rule.)" };
		ModComponentSolverInformation["FriendshipModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit the desired friendship element with !{0} submit Fairness Conscientiousness Kindness Authenticity.", DoesTheRightThing = false };
		ModComponentSolverInformation["GridlockModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["HexamazeModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move towards the exit with !{0} move 12 10 6 6 6 2, or with !{0} move N NW S S S NE. (clockface or cardinal)", DoesTheRightThing = false };
		ModComponentSolverInformation["http"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the response with !{0} resp 123." };
		ModComponentSolverInformation["iceCreamModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move left/right with !{0} left and !{0} right. Sell with !{0} sell.", DoesTheRightThing = false };
		ModComponentSolverInformation["Laundry"] = new ModuleInformation { moduleScore = 15, helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa", DoesTheRightThing = true };
		ModComponentSolverInformation["LEDEnc"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button with label B with !{0} press b." };
		ModComponentSolverInformation["LightCycleModule"] = new ModuleInformation { moduleScore = 12, helpText = "Submit your answer with !{0} B R W M G Y. (note, this module WILL try to input any answer you put into it.)", DoesTheRightThing = false };
		ModComponentSolverInformation["LightsOut"] = new ModuleInformation { moduleScore = 5, helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right." };
		ModComponentSolverInformation["Logic"] = new ModuleInformation { moduleScore = 12, helpText = "Logic is answered with !{0} submit F T." };
		ModComponentSolverInformation["MafiaModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Simple"] = new ModuleInformation { moduleScore = 12, manualCode = "Mastermind", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Cruel"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["MinesweeperModule"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { moduleScore = 8, helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false };
		ModComponentSolverInformation["monsplodeCards"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeFight"] = new ModuleInformation { moduleScore = 10, helpText = "Use a move with !{0} use splash.", DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeWho"] = new ModuleInformation { moduleScore = 5, helpText = "", DoesTheRightThing = true };
		ModComponentSolverInformation["MorseAMaze"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = false, moduleDisplayName = "Morse-A-Maze" };
		ModComponentSolverInformation["MouseInTheMaze"] = new ModuleInformation { moduleScore = 20, helpText = "Move with !{0} forward back. Turn with !{0} left right u-turn. The first letter only can be used instead. Submit with !{0} submit." };
		ModComponentSolverInformation["MusicRhythms"] = new ModuleInformation { moduleScore = 9, helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", DoesTheRightThing = false };
		ModComponentSolverInformation["MysticSquareModule"] = new ModuleInformation { moduleScore = 12, helpText = "Move the numbers around with !{0} press 1 3 2 1 3 4 6 8.", DoesTheRightThing = false };
		ModComponentSolverInformation["Needy Math"] = new ModuleInformation { moduleScore = 5, helpText = "" };
		ModComponentSolverInformation["neutralization"] = new ModuleInformation { moduleScore = 12, helpText = "Select a base with !{0} base NaOH. Turn the filter on/off with !{0} filter. Set drop count with !{0} conc set 48. Submit with !{0} titrate." };
		ModComponentSolverInformation["OnlyConnectModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press a button by position with !{0} press tm or !{0} press 2. Round 1 also accepts symbol names (e.g. reeds, eye, flax, lion, water, viper)." };
		ModComponentSolverInformation["spwizPerspectivePegs"] = new ModuleInformation { moduleScore = 5, helpText = "", DoesTheRightThing = true };
		ModComponentSolverInformation["PerplexingWiresModule"] = new ModuleInformation { moduleScore = 9 };
		ModComponentSolverInformation["PianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["PointOfOrderModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["RockPaperScissorsLizardSpockModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press scissors lizard.", manualCode = "Rock-Paper-Scissors-Lizard-Spock" };
		ModComponentSolverInformation["RubiksCubeModule"] = new ModuleInformation { moduleScore = 15, helpText = "View the colors on all sides with !{0} rotate. Reset the cube to starting state with !{0} reset. Solve the Cube with !{0} r' d u f' r' d' u b' u' f", manualCode = "Rubik%E2%80%99s Cube", DoesTheRightThing = true };
		ModComponentSolverInformation["screw"] = new ModuleInformation { moduleScore = 9, helpText = "Screw with !{0} screw tr or !{0} screw 3. Options are TL, TM, TR, BL, BM, BR. Press a button with !{0} press b or !{0} press 2." };
		ModComponentSolverInformation["SetModule"] = new ModuleInformation { moduleScore = 6 };
		ModComponentSolverInformation["Semaphore"] = new ModuleInformation { moduleScore = 7, helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok.", DoesTheRightThing = false };
		ModComponentSolverInformation["SillySlots"] = new ModuleInformation { moduleScore = 15, helpText = "Keep the slots with !{0} keep. Pull the slots with !{0} pull.", DoesTheRightThing = false };
		ModComponentSolverInformation["SimonScreamsModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press the correct colors for each round with !{0} press B O Y.", DoesTheRightThing = false };
		ModComponentSolverInformation["SkewedSlotsModule"] = new ModuleInformation { moduleScore = 12, helpText = "Submit the correct response with !{0} submit 1 2 3.", DoesTheRightThing = true };
		ModComponentSolverInformation["SouvenirModule"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the correct response with !{0} answer 3. Order is from top to bottom, then left to right.", CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["symbolicPasswordModule"] = new ModuleInformation { moduleScore = 9, helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!" };
		ModComponentSolverInformation["spwizTetris"] = new ModuleInformation { moduleScore = 5 };
		ModComponentSolverInformation["TextField"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button in column 3 row 2 and column 4 row 3 with !{0} press 3,2 4,3." };
		ModComponentSolverInformation["TicTacToeModule"] = new ModuleInformation { moduleScore = 12, helpText = "Press a button with !{0} tl. Buttons are tl, tm, tr, ml, mm, mr, bl, bm, br.", manualCode = "Tic-Tac-Toe" };
		ModComponentSolverInformation["TheBulbModule"] = new ModuleInformation { moduleScore = 7, helpText = "Press O with !{0} press O. Press I with !{0} press I. Unscrew the bulb with !{0} unscrew. Screw in the bulb with !{0} screw.", DoesTheRightThing = false };
		ModComponentSolverInformation["TheClockModule"] = new ModuleInformation { moduleScore = 9, helpText = "Submit a time with !{0} set 12:34 am. Command must include a 12-hour time followed by AM/PM.", DoesTheRightThing = true };
		ModComponentSolverInformation["TheGamepadModule"] = new ModuleInformation { moduleScore = 9 };
		ModComponentSolverInformation["webDesign"] = new ModuleInformation { moduleScore = 9, helpText = "Accept the design with !{0} acc. Consider the design with !{0} con. Reject the design with !{0} reject." };
		ModComponentSolverInformation["WirePlacementModule"] = new ModuleInformation { moduleScore = 6, helpText = "Cut the correct wires with !{0} cut A2 B4 D3." };
		ModComponentSolverInformation["WordSearchModule"] = new ModuleInformation { moduleScore = 6, helpText = "Select the word starting at column B row 3, and ending at column C row 4, with !{0} select B3 C4.", DoesTheRightThing = false };
		ModComponentSolverInformation["XRayModule"] = new ModuleInformation { moduleScore = 12 };
		ModComponentSolverInformation["YahtzeeModule"] = new ModuleInformation { moduleScore = 9, helpText = "Roll the dice with !{0} roll. Keep some dice with !{0} keep white,purple,blue,yellow,black. Roll the remaining dice until a 3 appears with !{0} roll until 3.", DoesTheRightThing = true };
		ModComponentSolverInformation["ZooModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = false };

		foreach (string key in ModComponentSolverInformation.Keys)
			ModComponentSolverInformation[key].moduleID = key;
	}

	public static ModuleInformation GetModuleInfo(string moduleType)
	{
		if (!ModComponentSolverInformation.ContainsKey(moduleType))
		{
			ModComponentSolverInformation[moduleType] = new ModuleInformation();
		}
		ModComponentSolverInformation[moduleType].moduleID = moduleType;
		return ModComponentSolverInformation[moduleType];
	}

	public static ModuleInformation[] GetModuleInformation()
	{
		return ModComponentSolverInformation.Values.ToArray();
	}

	public static void AddModuleInformation(ModuleInformation info)
	{
		if (info.moduleID == null) return;
		if (ModComponentSolverInformation.ContainsKey(info.moduleID))
		{
			ModuleInformation i = ModComponentSolverInformation[info.moduleID];
			if (i == null)
			{
				ModComponentSolverInformation[info.moduleID] = info;
				return;
			}
			i.moduleID = info.moduleID;

			if (!string.IsNullOrEmpty(info.moduleDisplayName))
				i.moduleDisplayName = info.moduleDisplayName;

			if (!string.IsNullOrEmpty(info.helpText) || info.helpTextOverride)
				i.helpText = info.helpText;

			if (!string.IsNullOrEmpty(info.manualCode) || info.manualCodeOverride)
				i.manualCode = info.manualCode;

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
			i.strikePenalty = info.strikePenalty;

			i.unclaimedColor = info.unclaimedColor;
		}
		else
		{
			ModComponentSolverInformation[info.moduleID] = info;
		}
	}

	public static ComponentSolver CreateSolver(BombCommander bombCommander, BombComponent bombComponent, ComponentTypeEnum componentType)
	{
		switch (componentType)
		{
			case ComponentTypeEnum.Wires:
				return new WireSetComponentSolver(bombCommander, (WireSetComponent)bombComponent);

			case ComponentTypeEnum.Keypad:
				return new KeypadComponentSolver(bombCommander, (KeypadComponent)bombComponent);

			case ComponentTypeEnum.BigButton:
				return new ButtonComponentSolver(bombCommander, (ButtonComponent)bombComponent);

			case ComponentTypeEnum.Memory:
				return new MemoryComponentSolver(bombCommander, (MemoryComponent)bombComponent);

			case ComponentTypeEnum.Simon:
				return new SimonComponentSolver(bombCommander, (SimonComponent)bombComponent);

			case ComponentTypeEnum.Venn:
				return new VennWireComponentSolver(bombCommander, (VennWireComponent)bombComponent);

			case ComponentTypeEnum.Morse:
				return new MorseCodeComponentSolver(bombCommander, (MorseCodeComponent)bombComponent);

			case ComponentTypeEnum.WireSequence:
				return new WireSequenceComponentSolver(bombCommander, (WireSequenceComponent)bombComponent);

			case ComponentTypeEnum.Password:
				return new PasswordComponentSolver(bombCommander, (PasswordComponent)bombComponent);

			case ComponentTypeEnum.Maze:
				return new InvisibleWallsComponentSolver(bombCommander, (InvisibleWallsComponent)bombComponent);

			case ComponentTypeEnum.WhosOnFirst:
				return new WhosOnFirstComponentSolver(bombCommander, (WhosOnFirstComponent)bombComponent);

			case ComponentTypeEnum.NeedyVentGas:
				return new NeedyVentComponentSolver(bombCommander, (NeedyVentComponent)bombComponent);

			case ComponentTypeEnum.NeedyCapacitor:
				return new NeedyDischargeComponentSolver(bombCommander, (NeedyDischargeComponent)bombComponent);

			case ComponentTypeEnum.NeedyKnob:
				return new NeedyKnobComponentSolver(bombCommander, (NeedyKnobComponent)bombComponent);

			case ComponentTypeEnum.Mod:
				KMBombModule solvableModule = bombComponent.GetComponent<KMBombModule>();
                try
                {
					return CreateModComponentSolver(bombCommander, bombComponent, solvableModule.ModuleType, solvableModule.ModuleDisplayName);
                }
                catch
                {
	                DebugLog("Failed to create a valid Component Solver for Bomb Module: {0}", solvableModule.ModuleDisplayName);
	                DebugLog("Using Fallback Compoment solver instead.");
	                LogAllComponentTypes(solvableModule);

					return new UnsupportedModComponentSolver(bombCommander, bombComponent);
                }

			case ComponentTypeEnum.NeedyMod:
				KMNeedyModule needyModule = bombComponent.GetComponent<KMNeedyModule>();
                try
                {
				    return CreateModComponentSolver(bombCommander, bombComponent, needyModule.ModuleType, needyModule.ModuleDisplayName);
                }
                catch
                {
	                DebugLog("Failed to create a valid Component Solver for Needy Module: {0}", needyModule.ModuleDisplayName);
	                DebugLog("Using Fallback Compoment solver instead.");
					LogAllComponentTypes(needyModule);

					return new UnsupportedModComponentSolver(bombCommander, bombComponent);
                }

			default:
                LogAllComponentTypes(bombComponent);
			    throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays'.", bombComponent.GetModuleDisplayName()));
		}
	}

	private static ComponentSolver CreateModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, string moduleType, string displayName)
	{
        bool shimExists = TwitchPlaySettings.data.EnableTwitchPlayShims && ModComponentSolverCreatorShims.ContainsKey(moduleType);
		if (ModComponentSolverCreators.ContainsKey(moduleType))
		{
            ComponentSolver solver = !shimExists ? ModComponentSolverCreators[moduleType](bombCommander, bombComponent) : ModComponentSolverCreatorShims[moduleType](bombCommander, bombComponent);
			return solver;
		}

		DebugLog("Attempting to find a valid process command method to respond with on component {0}...", moduleType);

		ModComponentSolverDelegate modComponentSolverCreator = GenerateModComponentSolverCreator(bombComponent, moduleType, displayName);

		ModComponentSolverCreators[moduleType] = modComponentSolverCreator ?? throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays' - Could not generate a valid componentsolver for the mod component!", bombComponent.GetModuleDisplayName()));

		return !shimExists ? modComponentSolverCreator(bombCommander, bombComponent) : ModComponentSolverCreatorShims[moduleType](bombCommander, bombComponent);
	}

	private static ModComponentSolverDelegate GenerateModComponentSolverCreator(BombComponent bombComponent, string moduleType, string displayName)
	{
	    MethodInfo method = FindProcessCommandMethod(bombComponent, out ModCommandType commandType, out Type commandComponentType);

	    ModuleInformation info = GetModuleInfo(moduleType);
		if (!info.helpTextOverride && FindHelpMessage(bombComponent, commandComponentType, out string help))
		{
			if (help != null)
				ModuleData.DataHasChanged |= !help.Equals(info.helpText);
			else
				ModuleData.DataHasChanged |= info.helpText != null;
			info.helpText = help;
		}

		if (!info.manualCodeOverride && FindManualCode(bombComponent, commandComponentType, out string manual))
		{
			if (manual != null)
				ModuleData.DataHasChanged |= !manual.Equals(info.manualCode);
			else
				ModuleData.DataHasChanged |= info.manualCode != null;
			info.manualCode = manual;
		}

		if (!info.statusLightOverride && FindStatusLightPosition(bombComponent, out bool statusLeft, out bool statusBottom, out float rotation))
		{
			ModuleData.DataHasChanged |= info.statusLightLeft != statusLeft;
			ModuleData.DataHasChanged |= info.statusLightDown != statusBottom;
			ModuleData.DataHasChanged |= (Mathf.Abs(info.chatRotation - rotation) >= 0.2f);
			info.statusLightLeft = statusLeft;
			info.statusLightDown = statusBottom;
			info.chatRotation = rotation;
		}

		if (!info.validCommandsOverride && FindRegexList(bombComponent, commandComponentType, out string[] regexList))
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
		ModuleData.DataHasChanged &= !SilentMode;
		ModuleData.WriteDataToFile();

		if (method != null)
		{
			switch (commandType)
			{
				case ModCommandType.Simple:
					return delegate (BombCommander _bombCommander, BombComponent _bombComponent)
					{
						Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
						return new SimpleModComponentSolver(_bombCommander, _bombComponent, method, commandComponent);
					};
				case ModCommandType.Coroutine:
				    FindCancelBool(bombComponent, commandComponentType, out FieldInfo cancelfield);
					return delegate (BombCommander _bombCommander, BombComponent _bombComponent)
					{
						Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
						return new CoroutineModComponentSolver(_bombCommander, _bombComponent, method, commandComponent, cancelfield);
					};
				case ModCommandType.Unsupported:
					DebugLog("No Valid Component Solver found. Falling back to unsupported component solver");
					return (_bombCommander, _bombComponent) => new UnsupportedModComponentSolver(_bombCommander, _bombComponent);
					
				default:
					break;
			}
		}

		return null;
	}

    private static readonly List<string> FullNamesLogged = new List<string>();
    private static void LogAllComponentTypes(MonoBehaviour bombComponent)
    {
        //If and when there is a potential conflict between multiple assemblies, this will help to find these conflicts so that
        //ReflectionHelper.FindType(fullName, assemblyName) can be used instead.

        Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
        foreach (Component component in allComponents)
        {
            string fullName = component.GetType().FullName;
	        if (FullNamesLogged.Contains(fullName)) continue;
	        FullNamesLogged.Add(fullName);

			Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).Where(t => t.FullName?.Equals(fullName) ?? false).ToArray();
            if (types.Length < 2)
                continue;


	        DebugLog("Found {0} types with fullName = \"{1}\"", types.Length, fullName);
            foreach (Type type in types)
            {
	            DebugLog("\ttype.FullName=\"{0}\" type.Assembly.GetName().Name=\"{1}\"", type.FullName, type.Assembly.GetName().Name);
            }
        }
    }

	private static bool FindStatusLightPosition(MonoBehaviour bombComponent, out bool StatusLightLeft, out bool StatusLightBottom, out float Rotation)
	{
		string statusLightStatus = "Attempting to find the modules StatusLightParent...";
		Component component = bombComponent.GetComponentInChildren<StatusLightParent>() ?? (Component) bombComponent.GetComponentInChildren<KMStatusLightParent>();
		if (component == null)
		{
			DebugLog($"{statusLightStatus} Not found.");
			StatusLightLeft = false;
			StatusLightBottom = false;
			Rotation = 0;
			return false;
		}

		StatusLightLeft = (component.transform.localPosition.x < 0);
		StatusLightBottom = (component.transform.localPosition.z < 0);
		Rotation = component.transform.localEulerAngles.y;
		DebugLog($"{statusLightStatus} Found in the {(StatusLightBottom ? "bottom" : "top")} {(StatusLightLeft ? "left" : "right")} corner, rotated {((int) Rotation)} degrees.");
		return true;
	}

	private static bool FindRegexList(MonoBehaviour bombComponent, Type commandComponentType, out string[] validCommands)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchValidCommands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (!(candidateString?.GetValue(bombComponent.GetComponent(commandComponentType)) is string[]))
		{
			validCommands = null;
			return false;
		}
		validCommands = (string[])candidateString.GetValue(bombComponent.GetComponent(commandComponentType));
		return true;
	}

	private static bool FindManualCode(MonoBehaviour bombComponent, Type commandComponentType, out string manualCode)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchManualCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (!(candidateString?.GetValue(bombComponent.GetComponent(commandComponentType)) is string))
		{
			manualCode = null;
			return false;
		}
		manualCode = (string)candidateString.GetValue(bombComponent.GetComponent(commandComponentType));
		return true;
	}

	private static bool FindHelpMessage(MonoBehaviour bombComponent, Type commandComponentType, out string helpText)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (!(candidateString?.GetValue(bombComponent.GetComponent(commandComponentType)) is string))
		{
			helpText = null;
			return false;
		}
		helpText = (string)candidateString.GetValue(bombComponent.GetComponent(commandComponentType));
		return true;
	}

	private static bool FindCancelBool(MonoBehaviour bombComponent, Type commandComponentType, out FieldInfo CancelField)
	{
		CancelField = commandComponentType.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		return CancelField?.GetValue(bombComponent.GetComponent(commandComponentType)) is bool;
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

		commandType = ModCommandType.Unsupported;
		commandComponentType = null;
		return null;
	}

	private static bool ValidateMethodCommandMethod(Type type, MethodInfo candidateMethod, out ModCommandType commandType)
	{
		commandType = ModCommandType.Unsupported;

		ParameterInfo[] parameters = candidateMethod.GetParameters();
		if (parameters == null || parameters.Length == 0)
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too few parameters).", type.FullName);
			return false;
		}

		if (parameters.Length > 1)
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (too many parameters).", type.FullName);
			return false;
		}

		if (parameters[0].ParameterType != typeof(string))
		{
			DebugLog("Found a potential candidate ProcessCommand method in {0}, but the parameter list does not match the expected parameter list (expected a single string parameter, got a single {1} parameter).", type.FullName, parameters[0].ParameterType.FullName);
			return false;
		}

		if (candidateMethod.ReturnType == typeof(KMSelectable[]))
		{
			DebugLog("Found a valid candidate ProcessCommand method in {0} (using easy/simple API).", type.FullName);
			commandType = ModCommandType.Simple;
			return true;
		}

		if (candidateMethod.ReturnType == typeof(IEnumerator))
		{
			DebugLog("Found a valid candidate ProcessCommand method in {0} (using advanced/coroutine API).", type.FullName);
			commandType = ModCommandType.Coroutine;
			return true;
		}

		return false;
	}
}
