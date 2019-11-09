using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Missions;
using UnityEngine;

public static class ComponentSolverFactory
{
	public static bool SilentMode = false;
	private static void DebugLog(string format, params object[] args)
	{
		if (SilentMode) return;
		DebugHelper.Log(string.Format(format, args));
	}

	private delegate ComponentSolver ModComponentSolverDelegate(TwitchModule module);
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
		ModComponentSolverCreators["MotionSense"] = module => new MotionSenseComponentSolver(module);

		//Perky Modules
		ModComponentSolverCreators["CrazyTalk"] = module => new CrazyTalkComponentSolver(module);
		ModComponentSolverCreators["CryptModule"] = module => new CryptographyComponentSolver(module);
		ModComponentSolverCreators["ForeignExchangeRates"] = module => new ForeignExchangeRatesComponentSolver(module);
		ModComponentSolverCreators["Listening"] = module => new ListeningComponentSolver(module);
		ModComponentSolverCreators["OrientationCube"] = module => new OrientationCubeComponentSolver(module);
		ModComponentSolverCreators["Probing"] = module => new ProbingComponentSolver(module);
		ModComponentSolverCreators["TurnTheKey"] = module => new TurnTheKeyComponentSolver(module);
		ModComponentSolverCreators["TurnTheKeyAdvanced"] = module => new TurnTheKeyAdvancedComponentSolver(module);

		//Kaneb Modules
		ModComponentSolverCreators["TwoBits"] = module => new TwoBitsComponentSolver(module);

		//Asimir Modules
		ModComponentSolverCreators["murder"] = module => new MurderComponentSolver(module);
		ModComponentSolverCreators["SeaShells"] = module => new SeaShellsComponentSolver(module);
		ModComponentSolverCreators["shapeshift"] = module => new ShapeShiftComponentSolver(module);
		ModComponentSolverCreators["ThirdBase"] = module => new ThirdBaseComponentSolver(module);

		//Mock Army Modules
		ModComponentSolverCreators["AnagramsModule"] = module => new AnagramsComponentSolver(module);
		ModComponentSolverCreators["Emoji Math"] = module => new EmojiMathComponentSolver(module);
		ModComponentSolverCreators["Needy Math"] = module => new NeedyMathComponentSolver(module);
		ModComponentSolverCreators["WordScrambleModule"] = module => new AnagramsComponentSolver(module);

		//Royal_Flu$h Modules
		ModComponentSolverCreators["coffeebucks"] = module => new CoffeebucksComponentSolver(module);
		ModComponentSolverCreators["festiveJukebox"] = module => new FestiveJukeboxComponentSolver(module);
		ModComponentSolverCreators["hangover"] = module => new HangoverComponentSolver(module);
		ModComponentSolverCreators["hieroglyphics"] = module => new HieroglyphicsComponentSolver(module);
		ModComponentSolverCreators["labyrinth"] = module => new LabyrinthComponentSolver(module);
		ModComponentSolverCreators["simonsStages"] = module => new SimonsStagesComponentSolver(module);
		ModComponentSolverCreators["skinnyWires"] = module => new SkinnyWiresComponentSolver(module);
		ModComponentSolverCreators["streetFighter"] = module => new StreetFighterComponentSolver(module);
		ModComponentSolverCreators["tWords"] = module => new TWordsComponentSolver(module);

		//Misc Modules
		ModComponentSolverCreators["EnglishTest"] = module => new EnglishTestComponentSolver(module);
		ModComponentSolverCreators["KnowYourWay"] = module => new KnowYourWayComponentSolver(module);
		ModComponentSolverCreators["LetterKeys"] = module => new LetterKeysComponentSolver(module);
		ModComponentSolverCreators["Microcontroller"] = module => new MicrocontrollerComponentSolver(module);
		ModComponentSolverCreators["resistors"] = module => new ResistorsComponentSolver(module);
		ModComponentSolverCreators["speakEnglish"] = module => new SpeakEnglishComponentSolver(module);
		ModComponentSolverCreators["switchModule"] = module => new SwitchesComponentSolver(module);
		ModComponentSolverCreators["EdgeworkModule"] = module => new EdgeworkComponentSolver(module);
		ModComponentSolverCreators["NeedyBeer"] = module => new NeedyBeerComponentSolver(module);
		ModComponentSolverCreators["errorCodes"] = module => new ErrorCodesComponentSolver(module);
		ModComponentSolverCreators["JuckAlchemy"] = module => new AlchemyComponentSolver(module);
		ModComponentSolverCreators["LEGOModule"] = module => new LEGOComponentSolver(module);
		ModComponentSolverCreators["boolMaze"] = module => new BooleanMazeComponentSolver(module);
		ModComponentSolverCreators["MorseWar"] = module => new MorseWarComponentSolver(module);
		ModComponentSolverCreators["necronomicon"] = module => new NecronomiconComponentSolver(module);
		ModComponentSolverCreators["numberNimbleness"] = module => new NumberNimblenessComponentSolver(module);

		//Translated Modules
		ModComponentSolverCreators["BigButtonTranslated"] = module => new TranslatedButtonComponentSolver(module);
		ModComponentSolverCreators["MorseCodeTranslated"] = module => new TranslatedMorseCodeComponentSolver(module);
		ModComponentSolverCreators["PasswordsTranslated"] = module => new TranslatedPasswordComponentSolver(module);
		ModComponentSolverCreators["WhosOnFirstTranslated"] = module => new TranslatedWhosOnFirstComponentSolver(module);
		ModComponentSolverCreators["VentGasTranslated"] = module => new TranslatedNeedyVentComponentSolver(module);

		// SHIMS
		// These override at least one specific command or formatting, then pass on control to ProcessTwitchCommand in all other cases. (Or in some cases, enforce unsubmittable penalty)
		ModComponentSolverCreators["BooleanKeypad"] = module => new BooleanKeypadShim(module);
		ModComponentSolverCreators["Color Generator"] = module => new ColorGeneratorShim(module);
		ModComponentSolverCreators["ExtendedPassword"] = module => new ExtendedPasswordComponentSolver(module);
		ModComponentSolverCreators["groceryStore"] = module => new GroceryStoreShim(module);
		ModComponentSolverCreators["plungerButton"] = module => new PlungerButtonShim(module);

		// Anti-troll shims - These are specifically meant to allow the troll commands to be disabled.
		ModComponentSolverCreators["MazeV2"] = module => new AntiTrollShim(module, "MazeV2", new Dictionary<string, string> { { "spinme", "Sorry, I am not going to waste time spinning every single pipe 360 degrees." } });

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
		ModComponentSolverInformation["murder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Murder", moduleScore = 8 };
		ModComponentSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Sea Shells", moduleScore = 7 };
		ModComponentSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Shape Shift", moduleScore = 6 };
		ModComponentSolverInformation["ThirdBase"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Third Base", moduleScore = 7 };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["MotionSense"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Motion Sense" };

		//Perky
		ModComponentSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Crazy Talk", moduleScore = 3 };
		ModComponentSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptography", moduleScore = 9 };
		ModComponentSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Foreign Exchange Rates", moduleScore = 5 };
		ModComponentSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, statusLightOverride = true, statusLightLeft = true, statusLightDown = false, moduleDisplayName = "Listening", moduleScore = 4 };
		ModComponentSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Orientation Cube", moduleScore = 8 };
		ModComponentSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Probing", moduleScore = 7 };
		ModComponentSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Key", moduleScore = 3, announceModule = true };
		ModComponentSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Keys", moduleScore = 8, announceModule = true };

		//Kaneb
		ModComponentSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Two Bits", moduleScore = 6 };

		//Mock Army
		ModComponentSolverInformation["AnagramsModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Anagrams", moduleScore = 1 };
		ModComponentSolverInformation["Emoji Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Emoji Math", moduleScore = 3 };
		ModComponentSolverInformation["Needy Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Math", manualCode = "Math", moduleScore = 1.1f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["WordScrambleModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Word Scramble", moduleScore = 1 };

		//Royal_Flu$h
		ModComponentSolverInformation["coffeebucks"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 11 };
		ModComponentSolverInformation["festiveJukebox"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 3 };
		ModComponentSolverInformation["hangover"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 7 };
		ModComponentSolverInformation["hieroglyphics"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 10 };
		ModComponentSolverInformation["labyrinth"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 10 };
		ModComponentSolverInformation["simonsStages"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 0, CameraPinningAlwaysAllowed = true, announceModule = true, moduleScoreIsDynamic = true, manualCode = "Simon%E2%80%99s Stages" };
		ModComponentSolverInformation["skinnyWires"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 6 };
		ModComponentSolverInformation["streetFighter"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 7 };
		ModComponentSolverInformation["tWords"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 5 };

		//Misc
		ModComponentSolverInformation["EnglishTest"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "English Test", moduleScore = 5 };
		ModComponentSolverInformation["KnowYourWay"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Know Your Way", moduleScore = 10 };
		ModComponentSolverInformation["LetterKeys"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Letter Keys", moduleScore = 3 };
		ModComponentSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Microcontroller", moduleScore = 8 };
		ModComponentSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Resistors", moduleScore = 7 };
		ModComponentSolverInformation["speakEnglish"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Speak English" };
		ModComponentSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Switches", moduleScore = 4 };
		ModComponentSolverInformation["EdgeworkModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Edgework", moduleScore = 2.2f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["NeedyBeer"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Refill That Beer!", moduleScore = 0.3f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["numberNimbleness"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleScore = 9 };
		ModComponentSolverInformation["errorCodes"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Error Codes", moduleScore = 4 };
		ModComponentSolverInformation["JuckAlchemy"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alchemy", moduleScore = 8 };
		ModComponentSolverInformation["LEGOModule"] = new ModuleInformation { moduleScore = 14, builtIntoTwitchPlays = true };
		ModComponentSolverInformation["boolMaze"] = new ModuleInformation { moduleScore = 9, builtIntoTwitchPlays = true };
		ModComponentSolverInformation["MorseWar"] = new ModuleInformation { moduleScore = 6, builtIntoTwitchPlays = true, statusLightDown = true, statusLightLeft = true };
		ModComponentSolverInformation["necronomicon"] = new ModuleInformation { moduleScore = 12, builtIntoTwitchPlays = true };

		//Steel Crate Games (Need these in place even for the Vanilla modules)
		ModComponentSolverInformation["WireSetComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wires", moduleScore = 1 };
		ModComponentSolverInformation["ButtonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button", moduleScore = 1 };
		ModComponentSolverInformation["ButtonComponentModifiedSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button", moduleScore = 4 };
		ModComponentSolverInformation["WireSequenceComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wire Sequence", moduleScore = 4 };
		ModComponentSolverInformation["WhosOnFirstComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First", moduleScore = 4 };
		ModComponentSolverInformation["VennWireComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Complicated Wires", moduleScore = 3 };
		ModComponentSolverInformation["SimonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Says", moduleScore = 3 };
		ModComponentSolverInformation["PasswordComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password", moduleScore = 2 };
		ModComponentSolverInformation["NeedyVentComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas", moduleScore = 0.4f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["NeedyKnobComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Knob", moduleScore = 0.6f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["NeedyDischargeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Capacitor", moduleScore = 0.02f, scoreMethod = ScoreMethod.NeedyTime };
		ModComponentSolverInformation["MorseCodeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code", moduleScore = 3 };
		ModComponentSolverInformation["MemoryComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memory", moduleScore = 4 };
		ModComponentSolverInformation["KeypadComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keypad", moduleScore = 1 };
		ModComponentSolverInformation["InvisibleWallsComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Maze", moduleScore = 2 };

		//Translated Modules
		ModComponentSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button Translated", moduleScore = 1 };
		ModComponentSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code Translated", moduleScore = 3 };
		ModComponentSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password Translated", moduleScore = 2 };
		ModComponentSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First Translated", moduleScore = 4 };
		ModComponentSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas Translated", moduleScore = 0.4f, scoreMethod = ScoreMethod.NeedySolves };

		//Shim added in between Twitch Plays and module (This allows overriding a specific command, or for enforcing unsubmittable penalty)
		ModComponentSolverInformation["Color Generator"] = new ModuleInformation { moduleDisplayName = "Color Generator", DoesTheRightThing = true, moduleScore = 5, helpText = "Submit a color using \"!{0} press bigred 1,smallred 2,biggreen 1,smallblue 1\" !{0} press <buttonname> <amount of times to push>. If you want to be silly, you can have this module change the color of the status light when solved with \"!{0} press smallblue UseRedOnSolve\" or UseOffOnSolve. You can make this module tell a story with !{0} tellmeastory, make a needy sound with !{0} needystart or !{0} needyend, fake strike with !{0} faksestrike, and troll with !{0} troll", helpTextOverride = true };
		ModComponentSolverInformation["ExtendedPassword"] = new ModuleInformation { moduleDisplayName = "Extended Password", moduleScore = 7, DoesTheRightThing = true };

		//These modules have troll commands built in.
		ModComponentSolverInformation["MazeV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Plumbing", moduleScore = 12 };
		ModComponentSolverInformation["SimonScreamsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//These modules are not built into TP, but they are created by notable people.

		//AAces
		ModComponentSolverInformation["bases"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["boggle"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["calendar"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["characterShift"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["complexKeypad"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["doubleColor"] = new ModuleInformation { moduleScore = 2, DoesTheRightThing = true, statusLightOverride = true, statusLightDown = true, statusLightLeft = true };
		ModComponentSolverInformation["dragonEnergy"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true, statusLightOverride = true, statusLightLeft = true, statusLightDown = true };
		ModComponentSolverInformation["equations"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["insanagrams"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["subways"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["timeKeeper"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["ColourFlash"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", DoesTheRightThing = true };
		ModComponentSolverInformation["CruelPianoKeys"] = new ModuleInformation { moduleScore = 13, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["FestivePianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["LightsOut"] = new ModuleInformation { helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right.", moduleScore = 2.4f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["Painting"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["PianoKeys"] = new ModuleInformation { moduleScore = 6, helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["Semaphore"] = new ModuleInformation { moduleScore = 6, helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok.", DoesTheRightThing = true };
		ModComponentSolverInformation["Tangrams"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//billy_bao
		ModComponentSolverInformation["binaryTree"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["greekCalculus"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = false };

		//Blananas2
		ModComponentSolverInformation["boneAppleTea"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["colourTalk"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["commonSense"] = new ModuleInformation { moduleScore = 0.9f, DoesTheRightThing = true, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["flowerPatch"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["garfieldKart"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["jackAttack"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["matchematics"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["weirdAlYankovic"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["snakesAndLadders"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };

		//CaitSith2
		ModComponentSolverInformation["BigCircle"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["MorseAMaze"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//Candela
		ModComponentSolverInformation["alphaBits"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = false };
		ModComponentSolverInformation["partialDerivatives"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };

		//catcraze777
		ModComponentSolverInformation["calcModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["pictionaryModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };

		//clutterArranger
		ModComponentSolverInformation["graphModule"] = new ModuleInformation { moduleScore = 6, helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR.", DoesTheRightThing = true }; // Connection Check
		ModComponentSolverInformation["monsplodeCards"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeFight"] = new ModuleInformation { moduleScore = 6, helpText = "Use a move with !{0} use splash.", DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeWho"] = new ModuleInformation { DoesTheRightThing = true, helpText = "Press either button with “!{ 0 } press left / right | Left and Right can be abbreviated to(L) & (R)", moduleScore = 0.03f, scoreMethod = ScoreMethod.NeedyTime };
		ModComponentSolverInformation["poetry"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };

		//DVD
		ModComponentSolverInformation["Detonato"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["Questionmark"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["unrelatedAnagrams"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//EggFriedCheese
		ModComponentSolverInformation["theBlock"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["stickyNotes"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//Eotall
		ModComponentSolverInformation["GameOfLifeCruel"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["GameOfLifeSimple"] = new ModuleInformation { moduleScore = 11, manualCode = "Game%20of%20Life%20Simple", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Simple"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Cruel"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };

		//EpicToast
		ModComponentSolverInformation["brushStrokes"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["burgerAlarm"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["buttonGrid"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["challengeAndContact"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["cookieJars"] = new ModuleInformation { moduleScoreIsDynamic = true, moduleScore = 0, DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["factoryMaze"] = new ModuleInformation { moduleScore = 18, DoesTheRightThing = true };
		ModComponentSolverInformation["hexabutton"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["instructions"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["krazyTalk"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = false };
		ModComponentSolverInformation["subscribeToPewdiepie"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["tashaSqueals"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };

		//Espik
		ModComponentSolverInformation["ForgetMeNow"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, DoesTheRightThing = false };
		ModComponentSolverInformation["MistakeModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["UnownCipher"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };

		//eXish
		ModComponentSolverInformation["blueArrowsModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["cruelDigitalRootModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["equationsXModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["faultyDigitalRootModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["FlavorText"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["FlavorTextCruel"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["geometryDashModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["greenArrowsModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["kookyKeypadModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["MadMemory"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["masyuModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["organizationModule"] = new ModuleInformation { moduleScore = 0, DoesTheRightThing = true, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["PrimeChecker"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["redArrowsModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["romanArtModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["romanNumeralsModule"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1.2f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["simonSelectsModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["StareModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["sync125_3"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["transmittedMorseModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["vectorsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["yellowArrowsModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };

		//Fixdoll
		ModComponentSolverInformation["curriculum"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };

		//Flamanis
		ModComponentSolverInformation["ChessModule"] = new ModuleInformation { moduleScore = 8, helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", DoesTheRightThing = false };
		ModComponentSolverInformation["Laundry"] = new ModuleInformation { moduleScore = 11, helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa", DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { moduleScore = 7, helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.", statusLightOverride = true, statusLightDown = false, statusLightLeft = false, DoesTheRightThing = true };

		//GHXX
		ModComponentSolverInformation["characterCodes"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["thedealmaker"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };

		//Goofy
		ModComponentSolverInformation["elderFuthark"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["harmonySequence"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["leftandRight"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["megaMan2"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["melodySequencer"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["simonSounds"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["stackem"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//Groover
		ModComponentSolverInformation["3dTunnels"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["logicGates"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["rubiksClock"] = new ModuleInformation { manualCode = "Rubik%E2%80%99s Clock", moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["shikaku"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["simonSamples"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["turtleRobot"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };

		//Hexicube
		ModComponentSolverInformation["MemoryV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Me Not", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["KeypadV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Round Keypad", moduleScore = 6 };
		ModComponentSolverInformation["ButtonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Square Button", moduleScore = 6 };
		ModComponentSolverInformation["SimonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Simon States", moduleScore = 6 };
		ModComponentSolverInformation["PasswordV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Safety Safe", moduleScore = 11 };
		ModComponentSolverInformation["MorseV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Morsematics", moduleScore = 10 };
		ModComponentSolverInformation["HexiEvilFMN"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Everything", moduleScoreIsDynamic = true, moduleScore = 0, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["NeedyVentV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Answering Questions", moduleScore = 0.8f, scoreMethod = ScoreMethod.NeedySolves, manualCode = "Answering Questions" };
		ModComponentSolverInformation["NeedyKnobV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Rotary Phone", moduleScore = 1.4f, scoreMethod = ScoreMethod.NeedySolves };

		//hockeygoalie78
		ModComponentSolverInformation["daylightDirections"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["riskyWires"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };

		//JerryErris
		ModComponentSolverInformation["arithmelogic"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["digitString"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["footnotes"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["forgetThis"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["gryphons"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["qFunctions"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["qSchlagDenBomb"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };
		ModComponentSolverInformation["qSwedishMaze"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };
		ModComponentSolverInformation["quizBuzz"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["simonStops"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["theTriangleButton"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//Kaito Sinclaire
		ModComponentSolverInformation["ksmAmazeingButtons"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["ksmBobBarks"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["ksmSimonLitSays"] = new ModuleInformation { moduleScore = 0.5f, DoesTheRightThing = true, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["ksmTetraVex"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//KingBranBran
		ModComponentSolverInformation["pieModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["tapCode"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["valves"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["visual_impairment"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };

		//KingSlendy
		ModComponentSolverInformation["ColorfulInsanity"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["ColorfulMadness"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["FlashMemory"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["PartyTime"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["ShapesBombs"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["SueetWall"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TenButtonColorCode"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["Wavetapping"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };

		//Kritzy
		ModComponentSolverInformation["Krit4CardMonte"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["KritBlackjack"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["KritConnectionDev"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["KritCMDPrompt"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["KritFlipTheCoin"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1.1f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["KritHoldUps"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["KritHomework"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["KritMicroModules"] = new ModuleInformation { moduleScore = 17, DoesTheRightThing = false };
		ModComponentSolverInformation["KritRadio"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = false };
		ModComponentSolverInformation["KritScripts"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };

		//LeGeND
		ModComponentSolverInformation["lgndAnnoyingArrows"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 0.7f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["lgndColoredKeys"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["lgndColorMatch"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 0.5f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["lgndEightPages"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["lgndGadgetronVendor"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["lgndHiddenColors"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["lgndLEDMath"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["lgndSnap"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 0.6f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["lgndTerrariaQuiz"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 0.7f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["lgndZoni"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//Livio
		ModComponentSolverInformation["theCodeModule"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["DrDoctorModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };

		//Maca
		ModComponentSolverInformation["Playfair"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true, manualCode = "Playfair%20Cipher", moduleDisplayName = "Playfair Cipher" };
		ModComponentSolverInformation["unfairCipher"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };

		//Marksam32
		ModComponentSolverInformation["burglarAlarm"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["cooking"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["CrackboxModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TheDigitModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["logicalButtonsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["mashematics"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["SplittingTheLootModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["Yoinkingmodule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//McNiko67
		ModComponentSolverInformation["Backgrounds"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["BigSwitch"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = false };
		ModComponentSolverInformation["BlindMaze"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["FaultyBackgrounds"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["FontSelect"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["MazeScrambler"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["Sink"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };

		//MrMelon
		ModComponentSolverInformation["colourcode"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = false };
		ModComponentSolverInformation["planets"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };

		//MrSpekCraft
		ModComponentSolverInformation["periodicTable"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["vexillology"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };

		//NoahCoolBoy
		ModComponentSolverInformation["pigpenRotations"] = new ModuleInformation { moduleScore = 8, helpTextOverride = true, helpText = "To submit abcdefhijklm use '!{0} abcdefhijklm'.", DoesTheRightThing = true };
		ModComponentSolverInformation["simonScrambles"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };

		//Piggered
		ModComponentSolverInformation["FlagsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["NonogramModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = false };

		//Qkrisi
		ModComponentSolverInformation["booleanWires"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["qkForgetPerspective"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["qkTernaryConverter"] = new ModuleInformation { DoesTheRightThing = true };

		//red031000
		ModComponentSolverInformation["digitalRoot"] = new ModuleInformation { moduleScore = 2, DoesTheRightThing = true };
		ModComponentSolverInformation["HotPotato"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["theNumber"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["PurgatoryModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["radiator"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["wastemanagement"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };

		//Red Penguin
		ModComponentSolverInformation["coloredMaze"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["encryptionBingo"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, announceModule = true, CameraPinningAlwaysAllowed = true, DoesTheRightThing = true };
		ModComponentSolverInformation["fruits"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["moduleMovements"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["robotProgramming"] = new ModuleInformation { moduleScore = 19, DoesTheRightThing = true };
		ModComponentSolverInformation["simonSimons"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };

		//Riverbui
		ModComponentSolverInformation["dominoes"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["FaultySink"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["insanetalk"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["mineseeker"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleMaze"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["USA"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };

		//Royal_Flu$h
		ModComponentSolverInformation["accumulation"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["algebra"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["alphabetNumbers"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["benedictCumberbatch"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["blockbusters"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["britishSlang"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["catchphrase"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["christmasPresents"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = false };
		ModComponentSolverInformation["countdown"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["cruelCountdown"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["cube"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["europeanTravel"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["flashingLights"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["freeParking"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["graffitiNumbers"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["guitarChords"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["horribleMemory"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["identityParade"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["iPhone"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 10 };
		ModComponentSolverInformation["jackOLantern"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 4, manualCode = "The%20Jack-O%E2%80%99-Lantern" };
		ModComponentSolverInformation["jewelVault"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["jukebox"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["ledGrid"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["lightspeed"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["londonUnderground"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["maintenance"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = false };
		ModComponentSolverInformation["modulo"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = false };
		ModComponentSolverInformation["moon"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["mortalKombat"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["numberCipher"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["plungerButton"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["Poker"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["quintuples"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["retirement"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = false };
		ModComponentSolverInformation["reverseMorse"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["simonsStar"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true, manualCode = "Simon%E2%80%99s%20Star" };
		ModComponentSolverInformation["skyrim"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12 };
		ModComponentSolverInformation["snooker"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["sonic"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["sphere"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["stockMarket"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["stopwatch"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["sun"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicCoordinates"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["taxReturns"] = new ModuleInformation { moduleScore = 18, DoesTheRightThing = true, announceModule = true };
		ModComponentSolverInformation["theSwan"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["wire"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = false };
		ModComponentSolverInformation["wireSpaghetti"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };

		//samfun123
		ModComponentSolverInformation["BrokenButtonsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["CheapCheckoutModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["CreationModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedEquationsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedValuesModule"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1.8f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["TheGamepadModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["MinesweeperModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["SkewedSlotsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["SynchronizationModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };

		//Sean Obach
		ModComponentSolverInformation["blackCipher"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["blueCipher"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["forgetEnigma"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["grayCipher"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["greenCipher"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["indigoCipher"] = new ModuleInformation { moduleScore = 17, DoesTheRightThing = true };
		ModComponentSolverInformation["orangeCipher"] = new ModuleInformation { moduleScore = 17, DoesTheRightThing = true };
		ModComponentSolverInformation["redCipher"] = new ModuleInformation { moduleScore = 16, DoesTheRightThing = true };
		ModComponentSolverInformation["toonEnough"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["violetCipher"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["whiteCipher"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["yellowCipher"] = new ModuleInformation { moduleScore = 16, DoesTheRightThing = true };

		//SL7205
		ModComponentSolverInformation["colormath"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["fastMath"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["http"] = new ModuleInformation { moduleScore = 1.2f, DoesTheRightThing = true, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["Logic"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["neutralization"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["QRCode"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1.5f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["screw"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TextField"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["webDesign"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//Spare Wizard
		ModComponentSolverInformation["spwiz3DMaze"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 12, helpTextOverride = true, helpText = "!4 move L F R F U [move] | !4 walk L F R F U [walk slower] [L = left, R = right, F = forward, U = u-turn]" };
		ModComponentSolverInformation["spwizAdventureGame"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true, helpTextOverride = true, helpText = "Cycle the stats with !{0} cycle stats. Cycle the Weapons/Items with !{0} cycle items. Cycle everything with !{0} cycle all. Use weapons/Items with !{0} use potion. Use multiple items with !{0} use ticket, crystal ball, caber. (spell out the item name completely. not case sensitive)" };
		ModComponentSolverInformation["spwizAstrology"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["spwizPerspectivePegs"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["spwizTetris"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 2.5f, scoreMethod = ScoreMethod.NeedySolves };

		//Speakingevil
		ModComponentSolverInformation["affineCycle"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = false };
		ModComponentSolverInformation["bamboozledAgain"] = new ModuleInformation { moduleScore = 40, DoesTheRightThing = true };
		ModComponentSolverInformation["bamboozlingButton"] = new ModuleInformation { moduleScore = 17, DoesTheRightThing = true };
		ModComponentSolverInformation["bamboozlingButtonGrid"] = new ModuleInformation { moduleScore = 25, DoesTheRightThing = true };
		ModComponentSolverInformation["borderedKeys"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["caesarCycle"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = false };
		ModComponentSolverInformation["crypticCycle"] = new ModuleInformation { moduleScore = 18, DoesTheRightThing = false };
		ModComponentSolverInformation["disorderedKeys"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["doubleArrows"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["faultyrgbMaze"] = new ModuleInformation { moduleScore = 18, DoesTheRightThing = true };
		ModComponentSolverInformation["forgetMeLater"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["hillCycle"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = false };
		ModComponentSolverInformation["jumbleCycle"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = false };
		ModComponentSolverInformation["misorderedKeys"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["orderedKeys"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["pigpenCycle"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = false };
		ModComponentSolverInformation["playfairCycle"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = false };
		ModComponentSolverInformation["recordedKeys"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["reorderedKeys"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["rgbMaze"] = new ModuleInformation { moduleScore = 16, DoesTheRightThing = true };
		ModComponentSolverInformation["tallorderedKeys"] = new ModuleInformation { moduleScore = 0, CameraPinningAlwaysAllowed = true, announceModule = true, moduleScoreIsDynamic = true, DoesTheRightThing = true };
		ModComponentSolverInformation["ultimateCycle"] = new ModuleInformation { moduleScore = 30, DoesTheRightThing = true };
		ModComponentSolverInformation["UltraStores"] = new ModuleInformation { moduleScore = 40, DoesTheRightThing = true };
		ModComponentSolverInformation["unorderedKeys"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//Strike_Kaboom
		ModComponentSolverInformation["KanjiModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//taggedjc
		//Extended passwords, which is shimmed above.
		ModComponentSolverInformation["hunting"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };

		//TasThing
		ModComponentSolverInformation["chineseCounting"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["NandMs"] = new ModuleInformation { moduleScore = 1, DoesTheRightThing = true };

		//ThatGuyCalledJules
		ModComponentSolverInformation["PressX"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["synonyms"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };

		//TheThirdMan
		ModComponentSolverInformation["bombDiffusal"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["bootTooBig"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["constellations"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["deckOfManyThings"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["doubleExpert"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["forgetThemAll"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["geneticSequence"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["giantsDrink"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true, manualCode = "The%20Giant%E2%80%99s%20Drink" };
		ModComponentSolverInformation["graphicMemory"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["heraldry"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["langtonAnt"] = new ModuleInformation { moduleScore = 17, DoesTheRightThing = true };
		ModComponentSolverInformation["luckyDice"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["maze3"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true, manualCode = "Maze%C2%B3" };
		ModComponentSolverInformation["modkit"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["moduleListening"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["morseButtons"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["oldFogey"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["qwirkle"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["raidingTemples"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["sevenDeadlySins"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicColouring"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["towerOfHanoi"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["treasureHunt"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = false };

		//Timwi (includes Perky/Konqi/Eluminate/Mitterdoo/Riverbui modules maintained by Timwi)
		ModComponentSolverInformation["AdjacentLettersModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["AdjacentLettersModule_Rus"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["alphabet"] = new ModuleInformation { moduleDisplayName = "Alphabet", moduleScore = 2, DoesTheRightThing = true };
		ModComponentSolverInformation["BattleshipModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BinaryPuzzleModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["BitmapsModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BlackHoleModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["BlindAlleyModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["BrailleModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BrokenGuitarChordsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true, statusLightOverride = true, statusLightLeft = true };
		ModComponentSolverInformation["TheBulbModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["CaesarCipherModule"] = new ModuleInformation { moduleScore = 3, DoesTheRightThing = true };
		ModComponentSolverInformation["TheClockModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSquaresModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSwitchesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["CoordinatesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["CornersModule"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["CursedDoubleOhModule"] = new ModuleInformation { moduleScore = 13, DoesTheRightThing = true };
		ModComponentSolverInformation["DecoloredSquaresModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["DiscoloredSquaresModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["DividedSquaresModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true, announceModule = true };
		ModComponentSolverInformation["DoubleOhModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true, statusLightOverride = true, statusLightDown = false, statusLightLeft = false };
		ModComponentSolverInformation["FollowTheLeaderModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["FriendshipModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["GridlockModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["HexamazeModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["HogwartsModule"] = new ModuleInformation { announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["HumanResourcesModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TheHypercubeModule"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["KudosudokuModule"] = new ModuleInformation { moduleScore = 16, DoesTheRightThing = true };
		ModComponentSolverInformation["lasers"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["LightCycleModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["LionsShareModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true, manualCode = "Lion%E2%80%99s%20Share" };
		ModComponentSolverInformation["MafiaModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["MahjongModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["MarbleTumbleModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["MaritimeFlagsModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["MouseInTheMaze"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["MysticSquareModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["NumberPad"] = new ModuleInformation { moduleDisplayName = "Number Pad", moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["OddOneOutModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["OneHundredAndOneDalmatiansModule"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["OnlyConnectModule"] = new ModuleInformation { moduleScore = 11, DoesTheRightThing = true };
		ModComponentSolverInformation["PatternCubeModule"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["PerplexingWiresModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["PointOfOrderModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["PolyhedralMazeModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["RegularCrazyTalkModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["RockPaperScissorsLizardSpockModule"] = new ModuleInformation { moduleScore = 6, manualCode = "Rock-Paper-Scissors-Lizard-Spock", DoesTheRightThing = true };
		ModComponentSolverInformation["RubiksCubeModule"] = new ModuleInformation { moduleScore = 10, manualCode = "Rubik%E2%80%99s Cube", DoesTheRightThing = true };
		ModComponentSolverInformation["SetModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["SillySlots"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSendsModule"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonShrieksModule"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSingsModule"] = new ModuleInformation { moduleScore = 14, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSpeaksModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSpinsModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["SouvenirModule"] = new ModuleInformation { moduleScore = 0, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true, unclaimable = true };
		ModComponentSolverInformation["SuperlogicModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["SymbolCycleModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["TennisModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["TicTacToeModule"] = new ModuleInformation { moduleScore = 10, manualCode = "Tic-Tac-Toe", DoesTheRightThing = true };
		ModComponentSolverInformation["TheUltracubeModule"] = new ModuleInformation { moduleScore = 20, DoesTheRightThing = true };
		ModComponentSolverInformation["UncoloredSquaresModule"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["WirePlacementModule"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["WordSearchModule"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["XRayModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["YahtzeeModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["ZooModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };

		//Trainzack
		ModComponentSolverInformation["ChordQualities"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["MusicRhythms"] = new ModuleInformation { moduleScore = 7, helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", DoesTheRightThing = false };

		//Virepri
		ModComponentSolverInformation["BitOps"] = new ModuleInformation { moduleScore = 6, helpText = "Submit the correct answer with !{0} submit 10101010.", manualCode = "Bitwise%20Operations", validCommands = new[] { "^submit [0-1]{8}$" }, DoesTheRightThing = true };
		ModComponentSolverInformation["LEDEnc"] = new ModuleInformation { moduleScore = 6, helpText = "Press the button with label B with !{0} press b.", DoesTheRightThing = true };

		//Windesign
		ModComponentSolverInformation["Color Decoding"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true, moduleDisplayName = "Color Decoding", manualCode = "Color Decoding" };
		ModComponentSolverInformation["GridMatching"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true, helpText = "Commands are “left/right/up/down/clockwise/counter-clockwise/submit” or “l/r/u/d/cw/ccw/s”. The letter can be set by using “set d” or “'d'”. All of these can be chained, for example: “!{0} up right right clockwise 'd' submit”. You can only use one letter-setting command at a time." };

		//ZekNikZ
		ModComponentSolverInformation["booleanVennModule"] = new ModuleInformation { moduleScore = 7, helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none).", DoesTheRightThing = true };
		ModComponentSolverInformation["buttonSequencesModule"] = new ModuleInformation { moduleScore = 8, manualCode = "Button%20Sequence", DoesTheRightThing = true };
		ModComponentSolverInformation["ColorMorseModule"] = new ModuleInformation { moduleScore = 10, DoesTheRightThing = true };
		ModComponentSolverInformation["complicatedButtonsModule"] = new ModuleInformation { moduleScore = 5, helpText = "Press the top button with !{0} press top (also t, 1, etc.).", DoesTheRightThing = true };
		ModComponentSolverInformation["fizzBuzzModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["iceCreamModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicPasswordModule"] = new ModuleInformation { moduleScore = 5, helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!", DoesTheRightThing = true };
		ModComponentSolverInformation["VaricoloredSquaresModule"] = new ModuleInformation { moduleScore = 9, DoesTheRightThing = true };

		//Other modded modules not built into Twitch Plays
		ModComponentSolverInformation["aa"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 1.3f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["BartendingModule"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["BinaryLeds"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["BooleanKeypad"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["blockStacks"] = new ModuleInformation { DoesTheRightThing = true, moduleScore = 0.6f, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["buttonMasherNeedy"] = new ModuleInformation { moduleScore = 0.5f, moduleDisplayName = "Needy Button Masher", helpText = "Press the button 20 times with !{0} press 20", DoesTheRightThing = true, scoreMethod = ScoreMethod.NeedySolves, manualCode = "Button Masher" };
		ModComponentSolverInformation["combinationLock"] = new ModuleInformation { moduleScore = 5, helpText = "Submit the code using !{0} submit 1 2 3.", DoesTheRightThing = false };
		ModComponentSolverInformation["DateFinder"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedMorse"] = new ModuleInformation { moduleScore = 15, DoesTheRightThing = true };
		ModComponentSolverInformation["EternitySDec"] = new ModuleInformation { DoesTheRightThing = false, moduleScore = 1, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["forgetUsNot"] = new ModuleInformation { moduleScore = 0, moduleScoreIsDynamic = true, CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = false };
		ModComponentSolverInformation["groceryStore"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true, helpText = "Use !{0} add item to cart | Adds an item to the cart. Use !{0} pay and leave | Pays and leaves | Commands can be abbreviated with !{0} add & !{0} pay" };
		ModComponentSolverInformation["keypadCombinations"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["keypadLock"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["legendreSymbol"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["manometers"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["mazematics"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };
		ModComponentSolverInformation["meter"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["modernCipher"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["R4YNeedyFlowerMash"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["Numbers"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["passportControl"] = new ModuleInformation { moduleScore = 6, DoesTheRightThing = true };
		ModComponentSolverInformation["needyPiano"] = new ModuleInformation { DoesTheRightThing = false, moduleScore = 1, scoreMethod = ScoreMethod.NeedySolves };
		ModComponentSolverInformation["safetySquare"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["SamRedButtons"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["sevenWires"] = new ModuleInformation { moduleScore = 4, DoesTheRightThing = true };
		ModComponentSolverInformation["Signals"] = new ModuleInformation { moduleScore = 8, DoesTheRightThing = true };
		ModComponentSolverInformation["simonStores"] = new ModuleInformation { moduleScore = 25, DoesTheRightThing = true };
		ModComponentSolverInformation["timezone"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["thewitness"] = new ModuleInformation { moduleScore = 5, DoesTheRightThing = true };
		ModComponentSolverInformation["vigenereCipher"] = new ModuleInformation { moduleScore = 7, DoesTheRightThing = true };
		ModComponentSolverInformation["X01"] = new ModuleInformation { moduleScore = 12, DoesTheRightThing = true };

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
				moduleScoreOverride = false,
				moduleScoreIsDynamic = info.moduleScoreIsDynamic,
				statusLightDown = info.statusLightDown,
				statusLightLeft = info.statusLightLeft,
				statusLightOverride = false,
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
			AddDefaultModuleInformation(new ModuleInformation { moduleID = moduleType });
		return DefaultModComponentSolverInformation[moduleType];
	}

	private static void ResetModuleInformationToDefault(string moduleType)
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
			ModuleData.DataHasChanged |= !info.helpText.TryEquals(defInfo.helpText);
			info.helpText = defInfo.helpText;
		}

		if (!info.moduleScoreOverride)
		{
			ModuleData.DataHasChanged |= !info.moduleScore.Equals(defInfo.moduleScore);
			info.moduleScore = defInfo.moduleScore;
		}

		if (!info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= !info.manualCode.TryEquals(defInfo.manualCode);
			info.manualCode = defInfo.manualCode;
		}

		if (!info.statusLightOverride)
		{
			ModuleData.DataHasChanged |= info.statusLightDown != defInfo.statusLightDown;
			ModuleData.DataHasChanged |= info.statusLightLeft != defInfo.statusLightLeft;
			info.statusLightDown = defInfo.statusLightDown;
			info.statusLightLeft = defInfo.statusLightLeft;
		}

		if (writeData && !info.builtIntoTwitchPlays)
			ModuleData.WriteDataToFile();

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
			ModuleData.DataHasChanged |= info.statusLightLeft != statusLightLeft;
			ModuleData.DataHasChanged |= info.statusLightDown != statusLightBottom;
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

	public static ModuleInformation[] GetModuleInformation() => ModComponentSolverInformation.Values.ToArray();

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
			i.announceModule = info.announceModule;
			i.unclaimable = info.unclaimable;

			i.moduleScoreOverride = info.moduleScoreOverride;
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

	public static ComponentSolver CreateSolver(TwitchModule module)
	{
		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (module.BombComponent.ComponentType)
		{
			case ComponentTypeEnum.Wires:
				return new WireSetComponentSolver(module);

			case ComponentTypeEnum.Keypad:
				return new KeypadComponentSolver(module);

			case ComponentTypeEnum.BigButton:
				return new ButtonComponentSolver(module);

			case ComponentTypeEnum.Memory:
				return new MemoryComponentSolver(module);

			case ComponentTypeEnum.Simon:
				return new SimonComponentSolver(module);

			case ComponentTypeEnum.Venn:
				return new VennWireComponentSolver(module);

			case ComponentTypeEnum.Morse:
				return new MorseCodeComponentSolver(module);

			case ComponentTypeEnum.WireSequence:
				return new WireSequenceComponentSolver(module);

			case ComponentTypeEnum.Password:
				return new PasswordComponentSolver(module);

			case ComponentTypeEnum.Maze:
				return new InvisibleWallsComponentSolver(module);

			case ComponentTypeEnum.WhosOnFirst:
				return new WhosOnFirstComponentSolver(module);

			case ComponentTypeEnum.NeedyVentGas:
				return new NeedyVentComponentSolver(module);

			case ComponentTypeEnum.NeedyCapacitor:
				return new NeedyDischargeComponentSolver(module);

			case ComponentTypeEnum.NeedyKnob:
				return new NeedyKnobComponentSolver(module);

			case ComponentTypeEnum.Mod:
				KMBombModule solvableModule = module.BombComponent.GetComponent<KMBombModule>();
				try
				{
					return CreateModComponentSolver(module, solvableModule.ModuleType, solvableModule.ModuleDisplayName);
				}
				catch (Exception exc)
				{
					if (!SilentMode)
					{
						DebugHelper.LogException(exc, string.Format("Failed to create a valid solver for regular module: {0}. Using fallback solver instead.", solvableModule.ModuleDisplayName));
						LogAllComponentTypes(solvableModule);
					}

					return new UnsupportedModComponentSolver(module);
				}

			case ComponentTypeEnum.NeedyMod:
				KMNeedyModule needyModule = module.BombComponent.GetComponent<KMNeedyModule>();
				try
				{
					return CreateModComponentSolver(module, needyModule.ModuleType, needyModule.ModuleDisplayName);
				}
				catch (Exception exc)
				{
					if (!SilentMode)
					{
						DebugHelper.LogException(exc, string.Format("Failed to create a valid solver for needy module: {0}. Using fallback solver instead.", needyModule.ModuleDisplayName));
						LogAllComponentTypes(needyModule);
					}

					return new UnsupportedModComponentSolver(module);
				}

			default:
				LogAllComponentTypes(module.BombComponent);
				throw new NotSupportedException($"Currently {module.BombComponent.GetModuleDisplayName()} is not supported by 'Twitch Plays'.");
		}
	}

	/// <summary>Returns the solver for a specific module. If there is a shim or a built-in solver, it will return that.</summary>
	private static ComponentSolver CreateModComponentSolver(TwitchModule module, string moduleType, string displayName) => ModComponentSolverCreators.TryGetValue(moduleType, out ModComponentSolverDelegate solverCreator)
			? solverCreator(module)
			: CreateDefaultModComponentSolver(module, moduleType, displayName)
			  ?? throw new NotSupportedException(
				  $"Currently {module.BombComponent.GetModuleDisplayName()} is not supported by 'Twitch Plays' - Could not generate a valid componentsolver for the mod component!");

	/// <summary>Returns a solver that relies on the module’s own implementation, bypassing built-in solvers and shims.</summary>
	public static ComponentSolver CreateDefaultModComponentSolver(TwitchModule module, string moduleType, string displayName, bool hookUpEvents = true)
	{
		MethodInfo method = FindProcessCommandMethod(module.BombComponent, out ModCommandType commandType, out Type commandComponentType);
		MethodInfo forcedSolved = FindSolveMethod(module.BombComponent, ref commandComponentType);

		ModuleInformation info = GetModuleInfo(moduleType);
		if (FindHelpMessage(module.BombComponent, commandComponentType, out string help) && !info.helpTextOverride)
		{
			ModuleData.DataHasChanged |= !help.TryEquals(info.helpText);
			info.helpText = help;
		}

		if (FindManualCode(module.BombComponent, commandComponentType, out string manual) && !info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= !manual.TryEquals(info.manualCode);
			info.manualCode = manual;
		}

		if (FindStatusLightPosition(module.BombComponent, out bool statusLeft, out bool statusBottom) && !info.statusLightOverride)
		{
			ModuleData.DataHasChanged |= info.statusLightLeft != statusLeft;
			ModuleData.DataHasChanged |= info.statusLightDown != statusBottom;
			info.statusLightLeft = statusLeft;
			info.statusLightDown = statusBottom;
		}

		if (FindModuleScore(module.BombComponent, commandComponentType, out int score) && !info.moduleScoreOverride)
		{
			ModuleData.DataHasChanged |= !score.Equals(info.moduleScore);
			info.moduleScore = score;
		}

		if (FindRegexList(module.BombComponent, commandComponentType, out string[] regexList) && !info.validCommandsOverride)
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

		if (commandComponentType == null) return null;
		ComponentSolverFields componentSolverFields = new ComponentSolverFields
		{
			CommandComponent = module.BombComponent.GetComponentInChildren(commandComponentType),
			Method = method,
			ForcedSolveMethod = forcedSolved,
			ModuleInformation = info,

			HelpMessageField = FindHelpMessage(commandComponentType),
			ManualCodeField = FindManualCode(commandComponentType),
			ZenModeField = FindZenModeBool(commandComponentType),
			TimeModeField = FindTimeModeBool(commandComponentType),
			AbandonModuleField = FindAbandonModuleList(commandComponentType),
			TwitchPlaysField = FindTwitchPlaysBool(commandComponentType),
			TwitchPlaysSkipTimeField = FindTwitchPlaysSkipTimeBool(commandComponentType),
			CancelField = FindCancelBool(commandComponentType),

			HookUpEvents = hookUpEvents
		};

		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (commandType)
		{
			case ModCommandType.Simple:
				return new SimpleModComponentSolver(module, componentSolverFields);

			case ModCommandType.Coroutine:
				return new CoroutineModComponentSolver(module, componentSolverFields);

			case ModCommandType.Unsupported:
				DebugLog("No Valid Component Solver found. Falling back to unsupported component solver");
				return new UnsupportedModComponentSolver(module, componentSolverFields);
		}

		return null;
	}

	private static readonly List<string> FullNamesLogged = new List<string>();
	private static void LogAllComponentTypes(Component bombComponent)
	{
		try
		{
			Component[] allComponents = bombComponent != null ? bombComponent.GetComponentsInChildren<Component>(true) : new Component[0];
			foreach (Component component in allComponents)
			{
#pragma warning disable IDE0031 // Use null propagation
				string fullName = component != null ? component.GetType().FullName : null;
#pragma warning restore IDE0031 // Use null propagation
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

	private static bool FindStatusLightPosition(Component bombComponent, out bool statusLightLeft, out bool statusLightBottom)
	{
		const string statusLightStatus = "Attempting to find the module’s StatusLightParent...";
		Component component = bombComponent.GetComponentInChildren<StatusLightParent>() ?? (Component) bombComponent.GetComponentInChildren<KMStatusLightParent>();
		if (component == null)
		{
			DebugLog($"{statusLightStatus} Not found.");
			statusLightLeft = false;
			statusLightBottom = false;
			return false;
		}

		Vector3 position = bombComponent.transform.InverseTransformPoint(component.transform.position);
		statusLightLeft = (Math.Round(position.x, 5) < 0);
		statusLightBottom = (Math.Round(position.z, 5) < 0);
		//DebugLog($"{statusLightStatus} Found in the {(statusLightBottom ? "bottom" : "top")} {(statusLightLeft ? "left" : "right")} corner.");
		return true;
	}

	private static bool FindRegexList(Component bombComponent, Type commandComponentType, out string[] validCommands)
	{
		FieldInfo candidateString = commandComponentType?.GetField("TwitchValidCommands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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

	private static bool FindManualCode(Component bombComponent, Type commandComponentType, out string manualCode)
	{
		FieldInfo candidateString = FindManualCode(commandComponentType);
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

	private static bool FindModuleScore(Component bombComponent, Type commandComponentType, out int moduleScore)
	{
		FieldInfo candidateInt = commandComponentType?.GetField("TwitchModuleScore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (candidateInt == null)
		{
			moduleScore = 5;
			return false;
		}
		if (!(candidateInt.GetValue(candidateInt.IsStatic ? null : bombComponent.GetComponent(commandComponentType)) is int))
		{
			moduleScore = 5;
			return false;
		}
		moduleScore = (int) candidateInt.GetValue(candidateInt.IsStatic ? null : bombComponent.GetComponent(commandComponentType));
		return true;
	}

	private static bool FindStrikePenalty(Component bombComponent, Type commandComponentType, out int strikePenalty)
	{
		FieldInfo candidateInt = commandComponentType?.GetField("TwitchStrikePenalty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (candidateInt == null)
		{
			strikePenalty = -6;
			return false;
		}
		if (!(candidateInt.GetValue(candidateInt.IsStatic ? null : bombComponent.GetComponent(commandComponentType)) is int))
		{
			strikePenalty = -6;
			return false;
		}
		strikePenalty = (int) candidateInt.GetValue(candidateInt.IsStatic ? null : bombComponent.GetComponent(commandComponentType));
		return true;
	}

	private static bool FindHelpMessage(Component bombComponent, Type commandComponentType, out string helpText)
	{
		FieldInfo candidateString = FindHelpMessage(commandComponentType);
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

	private static FieldInfo FindHelpMessage(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(string) ? cancelField : null;
	}

	private static FieldInfo FindManualCode(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetField("TwitchManualCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(string) ? cancelField : null;
	}

	private static FieldInfo FindCancelBool(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetField("TwitchShouldCancelCommand", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(bool) ? cancelField : null;
	}

	private static FieldInfo FindZenModeBool(Type commandComponentType)
	{
		FieldInfo zenField = commandComponentType?.GetField("TwitchZenMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ??
							commandComponentType?.GetField("ZenModeActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return zenField?.FieldType == typeof(bool) ? zenField : null;
	}

	private static FieldInfo FindTimeModeBool(Type commandComponentType)
	{
		FieldInfo timeField = commandComponentType?.GetField("TwitchTimeMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ??
							commandComponentType?.GetField("TimeModeActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return timeField?.FieldType == typeof(bool) ? timeField : null;
	}

	private static FieldInfo FindTwitchPlaysBool(Type commandComponentType)
	{
		FieldInfo twitchPlaysActiveField = commandComponentType?.GetField("TwitchPlaysActive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return twitchPlaysActiveField?.FieldType == typeof(bool) ? twitchPlaysActiveField : null;
	}

	private static FieldInfo FindTwitchPlaysSkipTimeBool(Type commandComponentType)
	{
		FieldInfo twitchPlaysActiveField = commandComponentType?.GetField("TwitchPlaysSkipTimeAllowed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return twitchPlaysActiveField?.FieldType == typeof(bool) ? twitchPlaysActiveField : null;
	}

	private static MethodInfo FindSolveMethod(Component bombComponent, ref Type commandComponentType)
	{
		if (commandComponentType == null)
		{
			Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
			foreach (Component component in allComponents)
			{
				Type type = component.GetType();
				MethodInfo candidateMethod = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					.FirstOrDefault(x => (x.ReturnType == typeof(void) || x.ReturnType == typeof(IEnumerator)) && x.GetParameters().Length == 0 && x.Name.Equals("TwitchHandleForcedSolve"));
				if (candidateMethod == null) continue;

				commandComponentType = type;
				return candidateMethod;
			}

			return null;
		}

		MethodInfo solveHandler = commandComponentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.FirstOrDefault(x => (x.ReturnType == typeof(void) || x.ReturnType == typeof(IEnumerator)) && x.GetParameters().Length == 0 && x.Name.Equals("TwitchHandleForcedSolve"));
		return solveHandler;
	}

	private static FieldInfo FindAbandonModuleList(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetField("TwitchAbandonModule", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		return cancelField?.FieldType == typeof(List<KMBombModule>) ? cancelField : null;
	}

	private static MethodInfo FindProcessCommandMethod(Component bombComponent, out ModCommandType commandType, out Type commandComponentType)
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

			if (!ValidateMethodCommandMethod(type, candidateMethod, out commandType)) continue;
			commandComponentType = type;
			return candidateMethod;
		}

		commandType = ModCommandType.Unsupported;
		commandComponentType = null;
		return null;
	}

	private static bool ValidateMethodCommandMethod(Type type, MethodInfo candidateMethod, out ModCommandType commandType)
	{
		commandType = ModCommandType.Unsupported;

		ParameterInfo[] parameters = candidateMethod.GetParameters();
		if (parameters.Length == 0)
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
			//DebugLog("Found a valid candidate ProcessCommand method in {0} (using easy/simple API).", type.FullName);
			commandType = ModCommandType.Simple;
			return true;
		}

		if (candidateMethod.ReturnType != typeof(IEnumerator)) return false;
		//DebugLog("Found a valid candidate ProcessCommand method in {0} (using advanced/coroutine API).", type.FullName);
		commandType = ModCommandType.Coroutine;
		return true;
	}
}
