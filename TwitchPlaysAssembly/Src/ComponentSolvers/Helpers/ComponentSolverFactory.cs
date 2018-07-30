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
	private static readonly Dictionary<string, ModuleInformation> ModComponentSolverInformation;
	private static readonly Dictionary<string, ModuleInformation> DefaultModComponentSolverInformation;



	static ComponentSolverFactory()
	{
		DebugHelper.Log();
		ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
		ModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
		DefaultModComponentSolverInformation = new Dictionary<string, ModuleInformation>();

		//AT_Bash Modules
		ModComponentSolverCreators["MotionSense"] = (bombCommander, bombComponent) => new MotionSenseComponentSolver(bombCommander, bombComponent);

		//Perky Modules
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

		//Mock Army Modules
		ModComponentSolverCreators["AnagramsModule"] = (bombCommander, bombComponent) => new AnagramsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Emoji Math"] = (bombCommander, bombComponent) => new EmojiMathComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["WordScrambleModule"] = (bombCommander, bombComponent) => new AnagramsComponentSolver(bombCommander, bombComponent);

		//Misc Modules
		ModComponentSolverCreators["EnglishTest"] = (bombCommander, bombComponent) => new EnglishTestComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["LetterKeys"] = (bombCommander, bombComponent) => new LetterKeysComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["Microcontroller"] = (bombCommander, bombComponent) => new MicrocontrollerComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["resistors"] = (bombCommander, bombComponent) => new ResistorsComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["switchModule"] = (bombCommander, bombComponent) => new SwitchesComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["EdgeworkModule"] = (bombCommander, bombComponent) => new EdgeworkComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["NeedyBeer"] = (bombCommander, bombComponent) => new NeedyBeerComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["errorCodes"] = (bombCommander, bombComponent) => new ErrorCodesComponentSolver(bombCommander, bombComponent);

		//Translated Modules
		ModComponentSolverCreators["BigButtonTranslated"] = (bombCommander, bombComponent) => new TranslatedButtonComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["MorseCodeTranslated"] = (bombCommander, bombComponent) => new TranslatedMorseCodeComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["PasswordsTranslated"] = (bombCommander, bombComponent) => new TranslatedPasswordComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["WhosOnFirstTranslated"] = (bombCommander, bombComponent) => new TranslatedWhosOnFirstComponentSolver(bombCommander, bombComponent);
		ModComponentSolverCreators["VentGasTranslated"] = (bombCommander, bombComponent) => new TranslatedNeedyVentComponentSolver(bombCommander, bombComponent);

		// SHIMS
		// These override at least one specific command or formatting, then pass on control to ProcessTwitchCommand in all other cases. (Or in some cases, enforce unsubmittable penalty)
		ModComponentSolverCreators["ExtendedPassword"] = (bombCommander, bombComponent) => new ExtendedPasswordComponentSolver(bombCommander, bombComponent);

		// Anti-troll shims - These are specifically meant to allow the troll commmands to be disabled.
		ModComponentSolverCreators["MazeV2"] = (bombCommander, bombComponent) => new AntiTrollShim(bombCommander, bombComponent, "MazeV2", new Dictionary<string, string> { { "spinme", "Sorry, I am not going to waste time spinning every single pipe 360 degrees." } });

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
				"validCommandsOverride": false,
				"validCommands": null,
				"DoesTheRightThing": true,
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
		 * helpTextOverride - If true, the help text will not be overwritten by the help text in the module.
		 * helpText - Instructions on how to interact with the module in twitch plays.
		 * 
		 * manualCodeOverride - If true, the manual code will not be overwritten by the manual code in the module.
		 * manualCode - If defined, is used instead of moduleDisplayName to look up the html/pdf manual.
		 * 
		 * statusLightOverride - Specifies an override of the ID# position / rotation. (This must be set if you wish to have the ID be anywhere other than
		 *      Above the status light, or if you wish to rotate the ID / chat box.)
		 * statusLightLeft - Specifies whether the ID should be on the left side of the module.
		 * statusLightDown - Specifies whether the ID should be on the bottom side of the module.
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
		ModComponentSolverInformation["murder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Murder", moduleScore = 10 };
		ModComponentSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Sea Shells", moduleScore = 7 };
		ModComponentSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Shape Shift", moduleScore = 8 };
		ModComponentSolverInformation["ThirdBase"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Third Base", moduleScore = 6 };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["MotionSense"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Motion Sense" };

		//Perky
		ModComponentSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Crazy Talk", moduleScore = 3 };
		ModComponentSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptography", moduleScore = 9 };
		ModComponentSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Foreign Exchange Rates", moduleScore = 6 };
		ModComponentSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, statusLightOverride = true, statusLightLeft = true, statusLightDown = false, moduleDisplayName = "Listening", moduleScore = 6 };
		ModComponentSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Orientation Cube", moduleScore = 10 };
		ModComponentSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Probing", moduleScore = 8 };
		ModComponentSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Key", moduleScore = 6 };
		ModComponentSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Keys", moduleScore = 15 };

		//Kaneb
		ModComponentSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Two Bits", moduleScore = 8 };

		//Mock Army
		ModComponentSolverInformation["AnagramsModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Anagrams", moduleScore = 3 };
		ModComponentSolverInformation["Emoji Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Emoji Math", moduleScore = 3 };
		ModComponentSolverInformation["WordScrambleModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Word Scramble", moduleScore = 3 };

		//Misc
		ModComponentSolverInformation["EnglishTest"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "English Test", moduleScore = 4 };
		ModComponentSolverInformation["LetterKeys"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Letter Keys", moduleScore = 3 };
		ModComponentSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Microcontroller", moduleScore = 10 };
		ModComponentSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Resistors", moduleScore = 6 };
		ModComponentSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Switches", moduleScore = 3 };
		ModComponentSolverInformation["EdgeworkModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Edgework" };
		ModComponentSolverInformation["NeedyBeer"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Beer Refill Mod" };
		ModComponentSolverInformation["errorCodes"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Error Codes", moduleScore = 3 };

		//Steel Crate Games (Need these in place even for the Vanilla modules)
		ModComponentSolverInformation["WireSetComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wires", moduleScore = 1 };
		ModComponentSolverInformation["ButtonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button", moduleScore = 1 };
		ModComponentSolverInformation["ButtonComponentModifiedSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button", moduleScore = 4 };
		ModComponentSolverInformation["WireSequenceComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wire Sequence", moduleScore = 4 };
		ModComponentSolverInformation["WhosOnFirstComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First", moduleScore = 4 };
		ModComponentSolverInformation["VennWireComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Complicated Wires", moduleScore = 3 };
		ModComponentSolverInformation["SimonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Says", moduleScore = 3 };
		ModComponentSolverInformation["PasswordComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password", moduleScore = 2 };
		ModComponentSolverInformation["NeedyVentComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas" };
		ModComponentSolverInformation["NeedyKnobComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Knob" };
		ModComponentSolverInformation["NeedyDischargeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Capacitor" };
		ModComponentSolverInformation["MorseCodeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code", moduleScore = 3 };
		ModComponentSolverInformation["MemoryComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memory", moduleScore = 4 };
		ModComponentSolverInformation["KeypadComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keypad", moduleScore = 1 };
		ModComponentSolverInformation["InvisibleWallsComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Maze", moduleScore = 2 };

		//Translated Modules
		ModComponentSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button Translated", moduleScore = 1 };
		ModComponentSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code Translated", moduleScore = 3 };
		ModComponentSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password Translated", moduleScore = 2 };
		ModComponentSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First Translated", moduleScore = 4 };
		ModComponentSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas Translated" };

		//Shim added in between Twitch Plays and module (This allows overriding a specific command, or for enforcing unsubmittable penalty)
		ModComponentSolverInformation["ExtendedPassword"] = new ModuleInformation { moduleDisplayName = "Extended Password", moduleScore = 7, DoesTheRightThing = true };

		//These modules have troll commands built in.
		ModComponentSolverInformation["Color Generator"] = new ModuleInformation { moduleDisplayName = "Color Generator", DoesTheRightThing = true };
		ModComponentSolverInformation["MazeV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Plumbing", moduleScore = 15 };
		ModComponentSolverInformation["SimonScreamsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };

		//These modules are not built into TP, but they are created by notable people.

		//AAces
		ModComponentSolverInformation["calendar"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["complexKeypad"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["doubleColor"] = new ModuleInformation { moduleScore = 2, DoesTheRightThing = true };
		ModComponentSolverInformation["subways"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["timeKeeper"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["ColourFlash"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", manualCode = "Color Flash", DoesTheRightThing = false };
		ModComponentSolverInformation["CruelPianoKeys"] = new ModuleInformation { moduleScore = 15, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["FestivePianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["LightsOut"] = new ModuleInformation { moduleScore = 5, helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right." };
		ModComponentSolverInformation["PianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["Semaphore"] = new ModuleInformation { moduleScore = 7, helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok.", DoesTheRightThing = true };
		ModComponentSolverInformation["Tangrams"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };

		//billy_bao
		ModComponentSolverInformation["binaryTree"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["greekCalculus"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = false };

		//CaitSith2
		ModComponentSolverInformation["BigCircle"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["MorseAMaze"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };

		//clutterArranger
		ModComponentSolverInformation["graphModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR.", DoesTheRightThing = true }; // Connection Check
		ModComponentSolverInformation["monsplodeCards"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeFight"] = new ModuleInformation { moduleScore = 8, helpText = "Use a move with !{0} use splash.", DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeWho"] = new ModuleInformation { moduleScore = 5, helpText = "", DoesTheRightThing = true };
		ModComponentSolverInformation["poetry"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };

		//Eotall
		ModComponentSolverInformation["GameOfLifeCruel"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["GameOfLifeSimple"] = new ModuleInformation { moduleScore = 15, manualCode = "Game%20of%20Life", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Simple"] = new ModuleInformation { moduleScore = 12, manualCode = "Mastermind", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Cruel"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };

		//Fixdoll
		ModComponentSolverInformation["curriculum"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };

		//Flamanis
		ModComponentSolverInformation["ChessModule"] = new ModuleInformation { moduleScore = 12, helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", DoesTheRightThing = false };
		ModComponentSolverInformation["Laundry"] = new ModuleInformation { moduleScore = 15, helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa", DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { moduleScore = 8, helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false, DoesTheRightThing = true };

		//Groover
		ModComponentSolverInformation["logicGates"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["rubiksClock"] = new ModuleInformation { manualCode = "Rubik%E2%80%99s Clock", moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["turtleRobot"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };

		//Hexicube
		ModComponentSolverInformation["MemoryV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Me Not", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["KeypadV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Round Keypad", moduleScore = 6 };
		ModComponentSolverInformation["ButtonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Square Button", moduleScore = 8 };
		ModComponentSolverInformation["SimonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Simon States", moduleScore = 6 };
		ModComponentSolverInformation["PasswordV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Safety Safe", moduleScore = 15 };
		ModComponentSolverInformation["MorseV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Morsematics", moduleScore = 12 };
		ModComponentSolverInformation["HexiEvilFMN"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Everything", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["NeedyVentV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Answering Questions" };
		ModComponentSolverInformation["NeedyKnobV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Rotary Phone" };

		//JoketteWuzHere
		ModComponentSolverInformation["Backgrounds"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["BlindMaze"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["FaultyBackgrounds"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["FontSelect"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["Sink"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };

		//KingBranBran
		ModComponentSolverInformation["pieModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["tapCode"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["visual_impairment"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };

		//Livio
		ModComponentSolverInformation["theCodeModule"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["CruelingCode"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["DrDoctorModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };

		//Marksam32
		ModComponentSolverInformation["burglarAlarm"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["cooking"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["logicalButtonsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["mashematics"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };

		//Piggered
		ModComponentSolverInformation["FlagsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["NonogramModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = false };

		//red031000
		ModComponentSolverInformation["digitalRoot"] = new ModuleInformation { moduleScore = 2, DoesTheRightThing = true };
		ModComponentSolverInformation["theNumber"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["radiator"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["wastemanagement"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };

		//Riverbui
		ModComponentSolverInformation["MazeScrambler"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["mineseeker"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["USA"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };

		//Royal_Flu$h
		ModComponentSolverInformation["algebra"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["alphabetNumbers"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["cube"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["europeanTravel"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["graffitiNumbers"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["identityParade"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["iPhone"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };
		ModComponentSolverInformation["jewelVault"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 15 };
		ModComponentSolverInformation["jukebox"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["ledGrid"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["londonUnderground"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["maintenance"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = false };
		ModComponentSolverInformation["moon"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["mortalKombat"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["Poker"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["simonsStar"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["skyrim"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 10 };
		ModComponentSolverInformation["sonic"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["stockMarket"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["stopwatch"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["sun"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicCoordinates"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["theSwan"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true, CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["wire"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = false };

		//samfun123
		ModComponentSolverInformation["BrokenButtonsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["CheapCheckoutModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["CreationModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["TheGamepadModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["MinesweeperModule"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["SkewedSlotsModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };

		//SL7205
		ModComponentSolverInformation["colormath"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["fastMath"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["http"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["Logic"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["neutralization"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["QRCode"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["screw"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TextField"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["webDesign"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		
		//Spare Wizard
		ModComponentSolverInformation["spwiz3DMaze"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 16 };
		ModComponentSolverInformation["spwizAdventureGame"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["spwizAstrology"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["spwizPerspectivePegs"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["spwizTetris"] = new ModuleInformation { moduleScore = 5 };

		//taggedjc
		//Extented passwords, which is shimmed above.
		ModComponentSolverInformation["hunting"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };

		//ThatGuyCalledJules
		ModComponentSolverInformation["PressX"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["synonyms"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//Timwi (includes Perky/Konqi/Eluminate/Mitterdoo/Riverbui modules maintained by Timwi)
		ModComponentSolverInformation["AdjacentLettersModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["alphabet"] = new ModuleInformation { moduleDisplayName = "Alphabet", moduleScore = 2, DoesTheRightThing = true };
		ModComponentSolverInformation["BattleshipModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BitmapsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["BlackHoleModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BlindAlleyModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["BrailleModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["CaesarCipherModule"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSquaresModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSwitchesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["CoordinatesModule"] = new ModuleInformation { moduleScore = 16, DoesTheRightThing = true };
		ModComponentSolverInformation["DoubleOhModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true, statusLightOverride = true, statusLightDown = false, statusLightLeft = false };
		ModComponentSolverInformation["FollowTheLeaderModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["FriendshipModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["GridlockModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["HexamazeModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["HumanResourcesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["lasers"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["LightCycleModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["MafiaModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["MarbleTumbleModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["MouseInTheMaze"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["MysticSquareModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["NumberPad"] = new ModuleInformation { moduleDisplayName = "Number Pad", moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["OnlyConnectModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["PerplexingWiresModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["PointOfOrderModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["PolyhedralMazeModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["RockPaperScissorsLizardSpockModule"] = new ModuleInformation { moduleScore = 6, manualCode = "Rock-Paper-Scissors-Lizard-Spock", DoesTheRightThing = true };
		ModComponentSolverInformation["RubiksCubeModule"] = new ModuleInformation { moduleScore = 12, manualCode = "Rubik%E2%80%99s Cube", DoesTheRightThing = true };
		ModComponentSolverInformation["SetModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["SillySlots"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSendsModule"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonShrieksModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSingsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["SouvenirModule"] = new ModuleInformation { moduleScore = 5, CameraPinningAlwaysAllowed = true };
		ModComponentSolverInformation["SuperlogicModule"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["SymbolCycleModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["TicTacToeModule"] = new ModuleInformation { moduleScore = 12, manualCode = "Tic-Tac-Toe", DoesTheRightThing = true };
		ModComponentSolverInformation["TheBulbModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["TheClockModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["WirePlacementModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["WordSearchModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["XRayModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["YahtzeeModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["ZooModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//Trainzack
		ModComponentSolverInformation["ChordQualities"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 9 };
		ModComponentSolverInformation["MusicRhythms"] = new ModuleInformation { moduleScore = 9, helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", DoesTheRightThing = false };

		//Virepri
		ModComponentSolverInformation["BitOps"] = new ModuleInformation { moduleScore = 10, helpText = "Submit the correct answer with !{0} submit 10101010.", manualCode = "Bitwise%20Operations", validCommands = new[] { "^submit [0-1]{8}$" }, DoesTheRightThing = true };
		ModComponentSolverInformation["LEDEnc"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button with label B with !{0} press b.", DoesTheRightThing = true };

		//Windesign
		ModComponentSolverInformation["Color Decoding"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["GridMatching"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };

		//ZekNikZ
		ModComponentSolverInformation["booleanVennModule"] = new ModuleInformation { moduleScore = 10, helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none).", DoesTheRightThing = true };
		ModComponentSolverInformation["buttonSequencesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["ColorMorseModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["complicatedButtonsModule"] = new ModuleInformation { moduleScore = 8, helpText = "Press the top button with !{0} press top (also t, 1, etc.).", DoesTheRightThing = true };
		ModComponentSolverInformation["fizzBuzzModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["iceCreamModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicPasswordModule"] = new ModuleInformation { moduleScore = 9, helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!", DoesTheRightThing = true };

		//Other modded Modules not built into Twitch Plays
		ModComponentSolverInformation["BinaryLeds"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["buttonMasherNeedy"] = new ModuleInformation { moduleScore = 5, moduleDisplayName = "Needy Button Masher", helpText = "Press the button 20 times with !{0} press 20", DoesTheRightThing = true };
		ModComponentSolverInformation["combinationLock"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the code using !{0} submit 1 2 3.", DoesTheRightThing = false };
		ModComponentSolverInformation["modernCipher"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["Playfair"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true, manualCode = "Playfair%20Cipher", moduleDisplayName = "Playfair Cipher" };
		ModComponentSolverInformation["SupermercadoSalvajeModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true, helpText = "Cycle the items with !{0} items. Go to a specific item number with !{0} item 3. Get customers to pay the correct amount with !{0} submit. Return the proper change with !{0} submit 3.24." };
		ModComponentSolverInformation["timezone"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["X01"] = new ModuleInformation { moduleScore = 8, moduleDisplayName = "X01", manualCode = "X01", DoesTheRightThing = true };

		foreach (KeyValuePair<string, ModuleInformation> kvp in ModComponentSolverInformation)
		{
			ModComponentSolverInformation[kvp.Key].moduleID = kvp.Key;
			AddDefaultModuleInformation(kvp.Value);
		}
	}

	private static void AddDefaultModuleInformation(ModuleInformation info)
	{
		if (string.IsNullOrEmpty(info?.moduleID)) return;
		if (!DefaultModComponentSolverInformation.ContainsKey(info.moduleID))
		{
			DefaultModComponentSolverInformation[info.moduleID] = new ModuleInformation
			{
				builtIntoTwitchPlays = info.builtIntoTwitchPlays,
				CameraPinningAlwaysAllowed = info.CameraPinningAlwaysAllowed,
				DoesTheRightThing = info.DoesTheRightThing,
				helpText = info.helpText,
				helpTextOverride = false,
				manualCode = info.manualCode,
				manualCodeOverride = false,
				moduleDisplayName = info.moduleDisplayName,
				moduleID = info.moduleID,
				moduleScore = info.moduleScore,
				moduleScoreIsDynamic = info.moduleScoreIsDynamic,
				statusLightDown = info.statusLightDown,
				statusLightLeft = info.statusLightLeft,
				statusLightOverride = false,
				strikePenalty = info.strikePenalty,
				unclaimedColor = info.unclaimedColor,
				validCommands = info.validCommands,
				validCommandsOverride = false
			};
		}
	}

	private static void AddDefaultModuleInformation(string moduleType, string moduleDisplayName, string helpText, string manualCode, bool statusLeft, bool statusBottom, string[] regexList)
	{
		if (string.IsNullOrEmpty(moduleType)) return;
		AddDefaultModuleInformation(GetModuleInfo(moduleType));
		ModuleInformation info = DefaultModComponentSolverInformation[moduleType];
		info.moduleDisplayName = moduleDisplayName;
		if (!string.IsNullOrEmpty(helpText)) info.helpText = helpText;
		if (!string.IsNullOrEmpty(manualCode)) info.manualCode = manualCode;
		info.statusLightLeft = statusLeft;
		info.statusLightDown = statusBottom;
		info.validCommands = regexList;
	}

	public static ModuleInformation GetDefaultInformation(string moduleType)
	{
		if (!DefaultModComponentSolverInformation.ContainsKey(moduleType))
			AddDefaultModuleInformation(new ModuleInformation() { moduleID = moduleType });
		return DefaultModComponentSolverInformation[moduleType];
	}

	public static void ResetModuleInformationToDefault(string moduleType)
	{
		if (!DefaultModComponentSolverInformation.ContainsKey(moduleType)) return;
		if (ModComponentSolverInformation.ContainsKey(moduleType)) ModComponentSolverInformation.Remove(moduleType);
		GetModuleInfo(moduleType);
		AddModuleInformation(DefaultModComponentSolverInformation[moduleType]);
	}

	public static void ResetAllModulesToDefault()
	{
		foreach (string key in ModComponentSolverInformation.Select(x => x.Key).ToArray())
		{
			ResetModuleInformationToDefault(key);
		}
	}

	public static ModuleInformation GetModuleInfo(string moduleType, bool writeData = true)
	{
		if (!ModComponentSolverInformation.ContainsKey(moduleType))
		{
			ModComponentSolverInformation[moduleType] = new ModuleInformation();
		}
		ModuleInformation info = ModComponentSolverInformation[moduleType];
		ModuleInformation defInfo = GetDefaultInformation(moduleType);
		info.moduleID = moduleType;
		defInfo.moduleID = moduleType;

		if (!info.helpTextOverride)
		{
			ModuleData.DataHasChanged |= info.helpText.TryEquals(defInfo.helpText);
			info.helpText = defInfo.helpText;
		}

		if (!info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= info.manualCode.TryEquals(defInfo.manualCode);
			info.manualCode = defInfo.manualCode;
		}

		if (!info.statusLightOverride)
		{
			ModuleData.DataHasChanged |= info.statusLightDown == defInfo.statusLightDown;
			ModuleData.DataHasChanged |= info.statusLightLeft == defInfo.statusLightLeft;
			info.statusLightDown = defInfo.statusLightDown;
			info.statusLightLeft = defInfo.statusLightLeft;
		}

		if (writeData && !info.builtIntoTwitchPlays)
		{
			ModuleData.WriteDataToFile();
		}

		return ModComponentSolverInformation[moduleType];
	}

	public static ModuleInformation GetModuleInfo(string moduleType, string helpText, string manualCode = null, bool statusLightLeft = false, bool statusLightBottom = false)
	{
		ModuleInformation info = GetModuleInfo(moduleType, false);
		ModuleInformation defInfo = GetDefaultInformation(moduleType);

		if (!info.helpTextOverride)
		{
			ModuleData.DataHasChanged |= !info.helpText.TryEquals(helpText);
			info.helpText = helpText;
		}
		if (!info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= !info.manualCode.TryEquals(manualCode);
			info.manualCode = manualCode;
		}
		if (!info.statusLightOverride)
		{
			ModuleData.DataHasChanged |= info.statusLightLeft == statusLightLeft;
			ModuleData.DataHasChanged |= info.statusLightDown == statusLightBottom;
			info.statusLightLeft = statusLightLeft;
			info.statusLightDown = statusLightBottom;
		}

		defInfo.helpText = helpText;
		defInfo.manualCode = manualCode;
		defInfo.statusLightLeft = statusLightLeft;
		defInfo.statusLightDown = statusLightBottom;

		ModuleData.WriteDataToFile();

		return info;
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

			i.statusLightLeft = info.statusLightLeft;
			i.statusLightDown = info.statusLightDown;

			i.moduleScore = info.moduleScore;
			i.moduleScoreIsDynamic = info.moduleScoreIsDynamic;
			i.strikePenalty = info.strikePenalty;

			i.helpTextOverride = info.helpTextOverride;
			i.manualCodeOverride = info.manualCodeOverride;
			i.statusLightOverride = info.statusLightOverride;

			if (!i.builtIntoTwitchPlays)
			{
				i.validCommandsOverride = info.validCommandsOverride;
				i.DoesTheRightThing |= info.DoesTheRightThing;
				i.validCommands = info.validCommands;
			}

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
				return new WireSetComponentSolver(bombCommander, (WireSetComponent) bombComponent);

			case ComponentTypeEnum.Keypad:
				return new KeypadComponentSolver(bombCommander, (KeypadComponent) bombComponent);

			case ComponentTypeEnum.BigButton:
				return new ButtonComponentSolver(bombCommander, (ButtonComponent) bombComponent);

			case ComponentTypeEnum.Memory:
				return new MemoryComponentSolver(bombCommander, (MemoryComponent) bombComponent);

			case ComponentTypeEnum.Simon:
				return new SimonComponentSolver(bombCommander, (SimonComponent) bombComponent);

			case ComponentTypeEnum.Venn:
				return new VennWireComponentSolver(bombCommander, (VennWireComponent) bombComponent);

			case ComponentTypeEnum.Morse:
				return new MorseCodeComponentSolver(bombCommander, (MorseCodeComponent) bombComponent);

			case ComponentTypeEnum.WireSequence:
				return new WireSequenceComponentSolver(bombCommander, (WireSequenceComponent) bombComponent);

			case ComponentTypeEnum.Password:
				return new PasswordComponentSolver(bombCommander, (PasswordComponent) bombComponent);

			case ComponentTypeEnum.Maze:
				return new InvisibleWallsComponentSolver(bombCommander, (InvisibleWallsComponent) bombComponent);

			case ComponentTypeEnum.WhosOnFirst:
				return new WhosOnFirstComponentSolver(bombCommander, (WhosOnFirstComponent) bombComponent);

			case ComponentTypeEnum.NeedyVentGas:
				return new NeedyVentComponentSolver(bombCommander, (NeedyVentComponent) bombComponent);

			case ComponentTypeEnum.NeedyCapacitor:
				return new NeedyDischargeComponentSolver(bombCommander, (NeedyDischargeComponent) bombComponent);

			case ComponentTypeEnum.NeedyKnob:
				return new NeedyKnobComponentSolver(bombCommander, (NeedyKnobComponent) bombComponent);

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

	/// <summary>Returns the solver for a specific module. If there is a shim or a built-in solver, it will return that.</summary>
	public static ComponentSolver CreateModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, string moduleType, string displayName)
	{
		if (ModComponentSolverCreators.TryGetValue(moduleType, out var solverCreator))
			return solverCreator(bombCommander, bombComponent);
		return CreateDefaultModComponentSolver(bombCommander, bombComponent, moduleType, displayName)
			?? throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays' - Could not generate a valid componentsolver for the mod component!", bombComponent.GetModuleDisplayName()));
	}

	/// <summary>Returns a solver that relies on the module�s own implementation, bypassing built-in solvers and shims.</summary>
	public static ComponentSolver CreateDefaultModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, string moduleType, string displayName)
	{
		MethodInfo method = FindProcessCommandMethod(bombComponent, out ModCommandType commandType, out Type commandComponentType);
		MethodInfo forcedSolved = FindSolveMethod(commandComponentType);

		ModuleInformation info = GetModuleInfo(moduleType);
		if (FindHelpMessage(bombComponent, commandComponentType, out string help) && !info.helpTextOverride)
		{
			ModuleData.DataHasChanged |= !help.TryEquals(info.helpText);
			info.helpText = help;
		}

		if (FindManualCode(bombComponent, commandComponentType, out string manual) && !info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= !manual.TryEquals(info.manualCode);
			info.manualCode = manual;
		}

		if (FindStatusLightPosition(bombComponent, out bool statusLeft, out bool statusBottom) && !info.statusLightOverride)
		{
			ModuleData.DataHasChanged |= info.statusLightLeft != statusLeft;
			ModuleData.DataHasChanged |= info.statusLightDown != statusBottom;
			info.statusLightLeft = statusLeft;
			info.statusLightDown = statusBottom;
		}

		if (FindRegexList(bombComponent, commandComponentType, out string[] regexList) && !info.validCommandsOverride)
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
						ModuleData.DataHasChanged |= !info.validCommands[i].TryEquals(regexList[i]);
				}
			}
			info.validCommands = regexList;
		}
		else
		{
			if (!info.validCommandsOverride)
				info.validCommands = null;
		}

		if (displayName != null)
			ModuleData.DataHasChanged |= !displayName.Equals(info.moduleDisplayName);
		else
			ModuleData.DataHasChanged |= info.moduleID != null;

		info.moduleDisplayName = displayName;
		ModuleData.WriteDataToFile();

		AddDefaultModuleInformation(moduleType, displayName, help, manual, statusLeft, statusBottom, regexList);

		if (method != null)
		{
			FieldInfo zenModeField = FindZenModeBool(commandComponentType);
			FieldInfo abandonModuleField = FindAbandonModuleList(commandComponentType);

			switch (commandType)
			{
				case ModCommandType.Simple:
					{
						Component commandComponent = bombComponent.GetComponentInChildren(commandComponentType);
						return new SimpleModComponentSolver(bombCommander, bombComponent, method, forcedSolved, commandComponent, zenModeField, abandonModuleField);
					}
				case ModCommandType.Coroutine:
					{
						FieldInfo cancelfield = FindCancelBool(commandComponentType);
						Component commandComponent = bombComponent.GetComponentInChildren(commandComponentType);
						return new CoroutineModComponentSolver(bombCommander, bombComponent, method, forcedSolved, commandComponent, cancelfield, zenModeField, abandonModuleField);
					}
				case ModCommandType.Unsupported:
					{
						DebugLog("No Valid Component Solver found. Falling back to unsupported component solver");
						return new UnsupportedModComponentSolver(bombCommander, bombComponent);
					}

				default:
					break;
			}
		}

		return null;
	}

	private static readonly List<string> FullNamesLogged = new List<string>();
	private static void LogAllComponentTypes(MonoBehaviour bombComponent)
	{
		try
		{
			Component[] allComponents = bombComponent?.GetComponentsInChildren<Component>(true) ?? new Component[0];
			foreach (Component component in allComponents)
			{
				string fullName = component?.GetType().FullName;
				if (string.IsNullOrEmpty(fullName) || FullNamesLogged.Contains(fullName)) continue;
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
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not log the component types due to an exception:");
		}
	}

	private static bool FindStatusLightPosition(MonoBehaviour bombComponent, out bool StatusLightLeft, out bool StatusLightBottom)
	{
		string statusLightStatus = "Attempting to find the modules StatusLightParent...";
		Component component = bombComponent.GetComponentInChildren<StatusLightParent>() ?? (Component) bombComponent.GetComponentInChildren<KMStatusLightParent>();
		if (component == null)
		{
			DebugLog($"{statusLightStatus} Not found.");
			StatusLightLeft = false;
			StatusLightBottom = false;
			return false;
		}

		StatusLightLeft = (component.transform.localPosition.x < 0);
		StatusLightBottom = (component.transform.localPosition.z < 0);
		DebugLog($"{statusLightStatus} Found in the {(StatusLightBottom ? "bottom" : "top")} {(StatusLightLeft ? "left" : "right")} corner.");
		return true;
	}

	private static bool FindRegexList(MonoBehaviour bombComponent, Type commandComponentType, out string[] validCommands)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchValidCommands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (candidateString == null)
		{
			validCommands = null;
			return false;
		}
		if (!(candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType)) is string[]))
		{
			validCommands = null;
			return false;
		}
		validCommands = (string[]) candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType));
		return true;
	}

	internal static bool FindManualCode(MonoBehaviour bombComponent, Type commandComponentType, out string manualCode)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchManualCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (candidateString == null)
		{
			manualCode = null;
			return false;
		}
		if (!(candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType)) is string))
		{
			manualCode = null;
			return false;
		}
		manualCode = (string) candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType));
		return true;
	}

	internal static bool FindHelpMessage(MonoBehaviour bombComponent, Type commandComponentType, out string helpText)
	{
		FieldInfo candidateString = commandComponentType.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (candidateString == null)
		{
			helpText = null;
			return false;
		}
		if (!(candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType)) is string))
		{
			helpText = null;
			return false;
		}
		helpText = (string) candidateString.GetValue(candidateString.IsStatic ? null : bombComponent.GetComponent(commandComponentType));
		return true;
	}

	internal static FieldInfo FindCancelBool(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(bool) ? cancelField : null;
	}

	internal static FieldInfo FindZenModeBool(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType.GetField("TwitchZenMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(bool) ? cancelField : null;
	}

	internal static MethodInfo FindSolveMethod(Type commandComponentType)
	{
		MethodInfo solveHandler = commandComponentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(x => (x.ReturnType == typeof(void) || x.ReturnType == typeof(IEnumerator)) && x.GetParameters().Length == 0 && x.Name.Equals("TwitchHandleForcedSolve"));
		return solveHandler;
	}

	internal static FieldInfo FindAbandonModuleList(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType.GetField("TwitchAbandonModule", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(List<KMBombModule>) ? cancelField : null;
	}

	internal static MethodInfo FindProcessCommandMethod(MonoBehaviour bombComponent, out ModCommandType commandType, out Type commandComponentType)
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

		if (typeof(IEnumerable<KMSelectable>).IsAssignableFrom(candidateMethod.ReturnType))
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
