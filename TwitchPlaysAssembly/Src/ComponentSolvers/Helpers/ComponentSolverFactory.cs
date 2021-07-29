using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public static class ComponentSolverFactory
{
	public static bool SilentMode = false;
	private static void DebugLog(string format, params object[] args)
	{
		if (SilentMode) return;
		DebugHelper.Log(string.Format(format, args));
	}

	private delegate ComponentSolver ModComponentSolverDelegate(TwitchModule module);
	private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
	private static readonly Dictionary<string, ModuleInformation> ModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private static readonly Dictionary<string, ModuleInformation> DefaultModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

	static ComponentSolverFactory()
	{
		//AT_Bash Modules
		ModComponentSolverCreators["MotionSense"] = module => new MotionSenseComponentSolver(module);
		ModComponentSolverCreators["AppreciateArt"] = Module => new AppreciateArtComponentSolver(Module);

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

		//LeGeND Modules
		ModComponentSolverCreators["lgndAlpha"] = module => new AlphaComponentSolver(module);
		ModComponentSolverCreators["lgndHyperactiveNumbers"] = module => new HyperactiveNumsComponentSolver(module);
		ModComponentSolverCreators["lgndMorseIdentification"] = module => new MorseIdentificationComponentSolver(module);
		ModComponentSolverCreators["lgndReflex"] = module => new ReflexComponentSolver(module);

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
		ModComponentSolverCreators["labyrinth"] = module => new LabyrinthComponentSolver(module);
		ModComponentSolverCreators["matrix"] = module => new TheMatrixComponentSolver(module);
		ModComponentSolverCreators["memorableButtons"] = module => new MemorableButtonsComponentSolver(module);
		ModComponentSolverCreators["simonsOnFirst"] = module => new SimonsOnFirstComponentSolver(module);
		ModComponentSolverCreators["simonsStages"] = module => new SimonsStagesComponentSolver(module);
		ModComponentSolverCreators["skinnyWires"] = module => new SkinnyWiresComponentSolver(module);
		ModComponentSolverCreators["stainedGlass"] = module => new StainedGlassComponentSolver(module);
		ModComponentSolverCreators["streetFighter"] = module => new StreetFighterComponentSolver(module);
		ModComponentSolverCreators["troll"] = module => new TheTrollComponentSolver(module);
		ModComponentSolverCreators["tWords"] = module => new TWordsComponentSolver(module);
		ModComponentSolverCreators["primeEncryption"] = module => new PrimeEncryptionComponentSolver(module);
		ModComponentSolverCreators["needyMrsBob"] = module => new NeedyMrsBobComponentSolver(module);
		ModComponentSolverCreators["simonSquawks"] = module => new SimonSquawksComponentSolver(module);
		ModComponentSolverCreators["rapidButtons"] = module => new RapidButtonsComponentSolver(module);

		//Samloper Modules
		ModComponentSolverCreators["buttonOrder"] = module => new ButtonOrderComponentSolver(module);
		ModComponentSolverCreators["pressTheShape"] = module => new PressTheShapeComponentSolver(module);
		ModComponentSolverCreators["standardButtonMasher"] = module => new StandardButtonMasherComponentSolver(module);
		ModComponentSolverCreators["BinaryButtons"] = module => new BinaryButtonsComponentSolver(module);

		//Misc Modules
		ModComponentSolverCreators["EnglishTest"] = module => new EnglishTestComponentSolver(module);
		ModComponentSolverCreators["LetterKeys"] = module => new LetterKeysComponentSolver(module);
		ModComponentSolverCreators["Microcontroller"] = module => new MicrocontrollerComponentSolver(module);
		ModComponentSolverCreators["resistors"] = module => new ResistorsComponentSolver(module);
		ModComponentSolverCreators["speakEnglish"] = module => new SpeakEnglishComponentSolver(module);
		ModComponentSolverCreators["NeedyBeer"] = module => new NeedyBeerComponentSolver(module);
		ModComponentSolverCreators["errorCodes"] = module => new ErrorCodesComponentSolver(module);
		ModComponentSolverCreators["JuckAlchemy"] = module => new AlchemyComponentSolver(module);
		ModComponentSolverCreators["boolMaze"] = module => new BooleanMazeComponentSolver(module);
		ModComponentSolverCreators["MorseWar"] = module => new MorseWarComponentSolver(module);
		ModComponentSolverCreators["necronomicon"] = module => new NecronomiconComponentSolver(module);
		ModComponentSolverCreators["numberNimbleness"] = module => new NumberNimblenessComponentSolver(module);
		ModComponentSolverCreators["babaIsWho"] = module => new BabaIsWhoComponentSolver(module);
		ModComponentSolverCreators["chordProgressions"] = module => new ChordProgressionsComponentSolver(module);
		ModComponentSolverCreators["rng"] = module => new RNGComponentSolver(module);
		ModComponentSolverCreators["needyShapeMemory"] = module => new ShapeMemoryComponentSolver(module);
		ModComponentSolverCreators["caesarsMaths"] = module => new CaesarsMathsComponentSolver(module);
		ModComponentSolverCreators["gatekeeper"] = module => new GatekeeperComponentSolver(module);
		ModComponentSolverCreators["stateOfAggregation"] = module => new StateOfAggregationComponentSolver(module);
		ModComponentSolverCreators["conditionalButtons"] = module => new ConditionalButtonsComponentSolver(module);
		ModComponentSolverCreators["strikeSolve"] = module => new StrikeSolveComponentSolver(module);
		ModComponentSolverCreators["abstractSequences"] = module => new AbstractSequencesComponentSolver(module);
		ModComponentSolverCreators["bridge"] = module => new BridgeComponentSolver(module);
		ModComponentSolverCreators["NotTimerModule"] = module => new NotTimerComponentSolver(module);
		ModComponentSolverCreators["needyHotate"] = module => new NeedyHotateComponentSolver(module);
		// Misc [ZekNikZ]
		ModComponentSolverCreators["EdgeworkModule"] = module => new EdgeworkComponentSolver(module);
		ModComponentSolverCreators["LEGOModule"] = module => new LEGOComponentSolver(module);
		// Misc [hockeygoalie78]
		ModComponentSolverCreators["CrypticPassword"] = module => new CrypticPasswordComponentSolver(module);
		ModComponentSolverCreators["modulusManipulation"] = module => new ModulusManipulationComponentSolver(module);

		//StrangaDanga Modules
		ModComponentSolverCreators["keepClicking"] = module => new KeepClickingComponentSolver(module);
		ModComponentSolverCreators["sixteenCoins"] = module => new SixteenCoinsComponentSolver(module);

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
		ModComponentSolverCreators["theSwan"] = module => new SwanShim(module);
		ModComponentSolverCreators["ButtonV2"] = module => new SquareButtonShim(module);
		ModComponentSolverCreators["spwizAstrology"] = module => new AstrologyShim(module);
		ModComponentSolverCreators["mysterymodule"] = module => new MysteryModuleShim(module);
		ModComponentSolverCreators["widgetModule"] = module => new MysteryWidgetShim(module);
		ModComponentSolverCreators["catchphrase"] = module => new CatchphraseShim(module);
		ModComponentSolverCreators["accumulation"] = module => new AccumulationShim(module);
		ModComponentSolverCreators["wire"] = module => new WireShim(module);
		ModComponentSolverCreators["moon"] = module => new MoonShim(module);
		ModComponentSolverCreators["sun"] = module => new SunShim(module);
		ModComponentSolverCreators["cube"] = module => new CubeShim(module);
		ModComponentSolverCreators["jackOLantern"] = module => new JackOLanternShim(module);
		ModComponentSolverCreators["simonsStar"] = module => new SimonStarShim(module);
		ModComponentSolverCreators["hieroglyphics"] = module => new HieroglyphicsShim(module);
		ModComponentSolverCreators["sphere"] = module => new SphereShim(module);
		ModComponentSolverCreators["lightspeed"] = module => new LightspeedShim(module);
		ModComponentSolverCreators["jukebox"] = module => new JukeboxShim(module);
		ModComponentSolverCreators["algebra"] = module => new AlgebraShim(module);
		ModComponentSolverCreators["horribleMemory"] = module => new HorribleMemoryShim(module);
		ModComponentSolverCreators["Poker"] = module => new PokerShim(module);
		ModComponentSolverCreators["stopwatch"] = module => new StopwatchShim(module);
		ModComponentSolverCreators["alphabetNumbers"] = module => new AlphabetNumbersShim(module);
		ModComponentSolverCreators["combinationLock"] = module => new CombinationLockShim(module);
		ModComponentSolverCreators["wireSpaghetti"] = module => new WireSpaghettiShim(module);
		ModComponentSolverCreators["christmasPresents"] = module => new ChristmasPresentsShim(module);
		ModComponentSolverCreators["numberCipher"] = module => new NumberCipherShim(module);
		ModComponentSolverCreators["maintenance"] = module => new MaintenanceShim(module);
		ModComponentSolverCreators["flashingLights"] = module => new FlashingLightsShim(module);
		ModComponentSolverCreators["sonic"] = module => new SonicShim(module);
		ModComponentSolverCreators["blockbusters"] = module => new BlockbustersShim(module);
		ModComponentSolverCreators["taxReturns"] = module => new TaxReturnsShim(module);
		ModComponentSolverCreators["reverseMorse"] = module => new ReverseMorseShim(module);
		ModComponentSolverCreators["spinningButtons"] = module => new SpinningButtonsShim(module);
		ModComponentSolverCreators["symbolicCoordinates"] = module => new SymbolicCoordinatesShim(module);
		ModComponentSolverCreators["britishSlang"] = module => new BritishSlangShim(module);
		ModComponentSolverCreators["lgndColoredKeys"] = module => new ColoredKeysShim(module);
		ModComponentSolverCreators["lgndHiddenColors"] = module => new HiddenColorsShim(module);
		ModComponentSolverCreators["lgndAudioMorse"] = module => new AudioMorseShim(module);
		ModComponentSolverCreators["lgndZoni"] = module => new ZoniShim(module);
		ModComponentSolverCreators["snooker"] = module => new SnookerShim(module);
		ModComponentSolverCreators["Mastermind Simple"] = module => new MastermindShim(module);
		ModComponentSolverCreators["Mastermind Cruel"] = module => new MastermindShim(module);
		ModComponentSolverCreators["hunting"] = module => new HuntingShim(module);
		ModComponentSolverCreators["NonogramModule"] = module => new NonogramShim(module);
		ModComponentSolverCreators["FlagsModule"] = module => new FlagsShim(module);
		ModComponentSolverCreators["theCodeModule"] = module => new CodeShim(module);
		ModComponentSolverCreators["Numbers"] = module => new NumbersShim(module);

		// Anti-troll shims - These are specifically meant to allow the troll commands to be disabled.
		ModComponentSolverCreators["MazeV2"] = module => new AntiTrollShim(module, new Dictionary<string, string> { { "spinme", "Sorry, I am not going to waste time spinning every single pipe 360 degrees." } });

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
				"statusLightPosition": "Default",
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
		ModComponentSolverInformation["murder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Murder", scoreString = "8" };
		ModComponentSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Sea Shells", scoreString = "8" };
		ModComponentSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Shape Shift", scoreString = "6" };
		ModComponentSolverInformation["ThirdBase"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Third Base", scoreString = "8" };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["MotionSense"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Motion Sense" };
		ModComponentSolverInformation["AppreciateArt"] = new ModuleInformation { builtIntoTwitchPlays = true, unclaimable = true, moduleDisplayName = "Art Appreciation" };

		//Perky
		ModComponentSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Crazy Talk", scoreString = "2" };
		ModComponentSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptography", scoreString = "8" };
		ModComponentSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Foreign Exchange Rates", scoreString = "3" };
		ModComponentSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Listening", scoreString = "5" };
		ModComponentSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Orientation Cube", scoreString = "7" };
		ModComponentSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Probing", scoreString = "7" };
		ModComponentSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Key", scoreString = "2", announceModule = true };
		ModComponentSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Keys", scoreString = "5", announceModule = true };

		//Kaneb
		ModComponentSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Two Bits", scoreString = "5" };

		//LeGeND
		ModComponentSolverInformation["lgndAlpha"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alpha", scoreString = "D 0.8" };
		ModComponentSolverInformation["lgndHyperactiveNumbers"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Hyperactive Numbers", scoreString = "6" };
		ModComponentSolverInformation["lgndMorseIdentification"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Identification", scoreString = "D 0.4" };
		ModComponentSolverInformation["lgndReflex"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Reflex", scoreString = "2" };

		//Mock Army
		ModComponentSolverInformation["AnagramsModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Anagrams", scoreString = "2" };
		ModComponentSolverInformation["Emoji Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Emoji Math", scoreString = "3" };
		ModComponentSolverInformation["Needy Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Math", manualCode = "Math", scoreString = "D 1.1" };
		ModComponentSolverInformation["WordScrambleModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Word Scramble", scoreString = "2" };

		//Royal_Flu$h
		ModComponentSolverInformation["coffeebucks"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Coffeebucks", scoreString = "12" };
		ModComponentSolverInformation["festiveJukebox"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Festive Jukebox", scoreString = "2" };
		ModComponentSolverInformation["hangover"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Hangover", scoreString = "7" };
		ModComponentSolverInformation["labyrinth"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Labyrinth", scoreString = "10" };
		ModComponentSolverInformation["matrix"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Matrix", scoreString = "7" };
		ModComponentSolverInformation["memorableButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memorable Buttons", scoreString = "9" };
		ModComponentSolverInformation["simonsOnFirst"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon's On First", scoreString = "8" };
		ModComponentSolverInformation["simonsStages"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon's Stages", scoreString = "S 1.5x", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["skinnyWires"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Skinny Wires", scoreString = "5" };
		ModComponentSolverInformation["stainedGlass"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Stained Glass", scoreString = "9" };
		ModComponentSolverInformation["streetFighter"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Street Fighter", scoreString = "8" };
		ModComponentSolverInformation["troll"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Troll", scoreString = "6", announceModule = true };
		ModComponentSolverInformation["tWords"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "T-Words", scoreString = "4" };
		ModComponentSolverInformation["primeEncryption"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Prime Encryption", scoreString = "10" };
		ModComponentSolverInformation["needyMrsBob"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Mrs Bob" };
		ModComponentSolverInformation["simonSquawks"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Squawks" };
		ModComponentSolverInformation["rapidButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Rapid Buttons" };

		//Misc
		ModComponentSolverInformation["EnglishTest"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "English Test", scoreString = "4" };
		ModComponentSolverInformation["LetterKeys"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Letter Keys", scoreString = "2" };
		ModComponentSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Microcontroller", scoreString = "7" };
		ModComponentSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Resistors", scoreString = "7" };
		ModComponentSolverInformation["speakEnglish"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Speak English" };
		ModComponentSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Switches", scoreString = "5" };
		ModComponentSolverInformation["EdgeworkModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Edgework", scoreString = "D 2.2" };
		ModComponentSolverInformation["NeedyBeer"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Refill That Beer!", scoreString = "D 0.2" };
		ModComponentSolverInformation["numberNimbleness"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Number Nimbleness", scoreString = "9" };
		ModComponentSolverInformation["errorCodes"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Error Codes", scoreString = "4" };
		ModComponentSolverInformation["JuckAlchemy"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alchemy", scoreString = "8" };
		ModComponentSolverInformation["LEGOModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "LEGOs", scoreString = "16" };
		ModComponentSolverInformation["boolMaze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Boolean Maze", scoreString = "8" };
		ModComponentSolverInformation["MorseWar"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse War", scoreString = "5" };
		ModComponentSolverInformation["necronomicon"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Necronomicon", scoreString = "11" };
		ModComponentSolverInformation["babaIsWho"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Baba Is Who?", scoreString = "6" };
		ModComponentSolverInformation["chordProgressions"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Chord Progressions", scoreString = "6" };
		ModComponentSolverInformation["CrypticPassword"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptic Password", scoreString = "6" };
		ModComponentSolverInformation["modulusManipulation"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Modulus Manipulation", scoreString = "8" };
		ModComponentSolverInformation["rng"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Random Number Generator", scoreString = "D 0.7", additionalNeedyTime = 30 };
		ModComponentSolverInformation["needyShapeMemory"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Shape Memory", manualCode = "Shape Memory", scoreString = "D 0.2" };
		ModComponentSolverInformation["caesarsMaths"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Caesar's Maths", scoreString = "5" };
		ModComponentSolverInformation["gatekeeper"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Gatekeeper", scoreString = "3" };
		ModComponentSolverInformation["stateOfAggregation"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "State of Aggregation", scoreString = "7" };
		ModComponentSolverInformation["conditionalButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Conditional Buttons", scoreString = "5" };
		ModComponentSolverInformation["strikeSolve"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Strike Solve", manualCode = "StrikeSolve", scoreString = "2" };
		ModComponentSolverInformation["abstractSequences"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Abstract Sequences" };
		ModComponentSolverInformation["bridge"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Bridge" };
		ModComponentSolverInformation["NotTimerModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Not Timer" };
		ModComponentSolverInformation["needyHotate"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Hotate" };

		//Samloper
		ModComponentSolverInformation["buttonOrder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Button Order", scoreString = "1" };
		ModComponentSolverInformation["pressTheShape"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Press The Shape" };
		ModComponentSolverInformation["standardButtonMasher"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Standard Button Masher" };
		ModComponentSolverInformation["BinaryButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Binary Buttons" };

		//Steel Crate Games (Need these in place even for the Vanilla modules)
		ModComponentSolverInformation["WireSetComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wires", scoreString = "2" };
		ModComponentSolverInformation["ButtonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Button", scoreString = "2" };
		ModComponentSolverInformation["ButtonComponentModifiedSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Button", scoreString = "4" };
		ModComponentSolverInformation["WireSequenceComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wire Sequence", scoreString = "4" };
		ModComponentSolverInformation["WhosOnFirstComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First", scoreString = "5" };
		ModComponentSolverInformation["VennWireComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Complicated Wires", scoreString = "4" };
		ModComponentSolverInformation["SimonComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Says", scoreString = "3" };
		ModComponentSolverInformation["PasswordComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password", scoreString = "3" };
		ModComponentSolverInformation["NeedyVentComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas", scoreString = "D 0.25" };
		ModComponentSolverInformation["NeedyKnobComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Knob", scoreString = "D 0.6" };
		ModComponentSolverInformation["NeedyDischargeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Capacitor", scoreString = "T 0.02" };
		ModComponentSolverInformation["MorseCodeComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code", scoreString = "4" };
		ModComponentSolverInformation["MemoryComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memory", scoreString = "3" };
		ModComponentSolverInformation["KeypadComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keypad", scoreString = "2" };
		ModComponentSolverInformation["InvisibleWallsComponentSolver"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Maze", scoreString = "2" };

		//StrangaDanga
		ModComponentSolverInformation["keepClicking"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keep Clicking", scoreString = "4" };
		ModComponentSolverInformation["sixteenCoins"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "16 Coins", scoreString = "4" };

		//Translated Modules
		ModComponentSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button Translated", scoreString = "2" };
		ModComponentSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code Translated", scoreString = "4" };
		ModComponentSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password Translated", scoreString = "4" };
		ModComponentSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First Translated", scoreString = "6" };
		ModComponentSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Vent Gas Translated", scoreString = "D 0.25" };

		//Shim added in between Twitch Plays and module (This allows overriding a specific command, or for enforcing unsubmittable penalty)
		ModComponentSolverInformation["Color Generator"] = new ModuleInformation { moduleDisplayName = "Color Generator", DoesTheRightThing = true, scoreString = "6", helpText = "Submit a color using \"!{0} press bigred 1,smallred 2,biggreen 1,smallblue 1\" !{0} press <buttonname> <amount of times to push>. If you want to be silly, you can have this module change the color of the status light when solved with \"!{0} press smallblue UseRedOnSolve\" or UseOffOnSolve. You can make this module tell a story with !{0} tellmeastory, make a needy sound with !{0} needystart or !{0} needyend, fake strike with !{0} faksestrike, and troll with !{0} troll", helpTextOverride = true };
		ModComponentSolverInformation["ExtendedPassword"] = new ModuleInformation { moduleDisplayName = "Extended Password", scoreString = "6", DoesTheRightThing = true };

		//These modules have troll commands built in.
		ModComponentSolverInformation["MazeV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Plumbing", scoreString = "12" };
		ModComponentSolverInformation["SimonScreamsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//These modules are not built into TP, but they are created by notable people.

		//AAces
		ModComponentSolverInformation["bases"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["boggle"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["calendar"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["characterShift"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["complexKeypad"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["doubleColor"] = new ModuleInformation { scoreString = "2", DoesTheRightThing = true };
		ModComponentSolverInformation["dragonEnergy"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["equations"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["insanagrams"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["subways"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["timeKeeper"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["ColourFlash"] = new ModuleInformation { scoreString = "6", helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", DoesTheRightThing = true };
		ModComponentSolverInformation["CruelPianoKeys"] = new ModuleInformation { scoreString = "13", helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["FestivePianoKeys"] = new ModuleInformation { scoreString = "6", helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["LightsOut"] = new ModuleInformation { helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right.", scoreString = "D 2.4" };
		ModComponentSolverInformation["Painting"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["PianoKeys"] = new ModuleInformation { scoreString = "6", helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", DoesTheRightThing = false };
		ModComponentSolverInformation["Semaphore"] = new ModuleInformation { scoreString = "6", helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok.", DoesTheRightThing = true };
		ModComponentSolverInformation["Tangrams"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//billy_bao
		ModComponentSolverInformation["binaryTree"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["greekCalculus"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = false };

		//Blananas2
		ModComponentSolverInformation["boneAppleTea"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["cheepCheckout"] = new ModuleInformation { scoreString = "9" /*, DoesTheRightThing = ??? */ };
		ModComponentSolverInformation["colourTalk"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["commonSense"] = new ModuleInformation { scoreString = "D 0.9", DoesTheRightThing = true };
		ModComponentSolverInformation["flowerPatch"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["garfieldKart"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["hyperlink"] = new ModuleInformation { scoreString = "10" /*, DoesTheRightThing = ??? */ };
		ModComponentSolverInformation["jackAttack"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["matchematics"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["snakesAndLadders"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["spellingBee"] = new ModuleInformation { scoreString = "4" /*, DoesTheRightThing = ??? */ };
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["weirdAlYankovic"] = new ModuleInformation { scoreString = "2", DoesTheRightThing = true };

		//CaitSith2
		ModComponentSolverInformation["BigCircle"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["MorseAMaze"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//catcraze777
		ModComponentSolverInformation["calcModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["pictionaryModule"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };

		//clutterArranger
		ModComponentSolverInformation["graphModule"] = new ModuleInformation { scoreString = "6", helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR.", DoesTheRightThing = true }; // Connection Check
		ModComponentSolverInformation["monsplodeCards"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeFight"] = new ModuleInformation { scoreString = "6", helpText = "Use a move with !{0} use splash.", DoesTheRightThing = true };
		ModComponentSolverInformation["monsplodeWho"] = new ModuleInformation { DoesTheRightThing = true, helpText = "Press either button with “!{ 0 } press left / right | Left and Right can be abbreviated to(L) & (R)", scoreString = "T 0.03" };
		ModComponentSolverInformation["poetry"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };

		//DVD
		ModComponentSolverInformation["Detonato"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["Questionmark"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["unrelatedAnagrams"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//EggFriedCheese
		ModComponentSolverInformation["theBlock"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["stickyNotes"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//Eotall
		ModComponentSolverInformation["GameOfLifeCruel"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["GameOfLifeSimple"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Simple"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["Mastermind Cruel"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };

		//EpicToast
		ModComponentSolverInformation["brushStrokes"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["burgerAlarm"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };
		ModComponentSolverInformation["buttonGrid"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["challengeAndContact"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["cookieJars"] = new ModuleInformation { scoreString = "S0.25", DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["factoryMaze"] = new ModuleInformation { scoreString = "18", DoesTheRightThing = true };
		ModComponentSolverInformation["hexabutton"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["instructions"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["krazyTalk"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = false };
		ModComponentSolverInformation["subscribeToPewdiepie"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["tashaSqueals"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };

		//Espik
		ModComponentSolverInformation["ForgetMeNow"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, DoesTheRightThing = false };
		ModComponentSolverInformation["MistakeModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["UnownCipher"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };

		//eXish
		ModComponentSolverInformation["blueArrowsModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["cruelDigitalRootModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["equationsXModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["faultyDigitalRootModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["FlavorText"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["FlavorTextCruel"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["geometryDashModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["greenArrowsModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["kookyKeypadModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["MadMemory"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["masyuModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["organizationModule"] = new ModuleInformation { scoreString = "S1", DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["PrimeChecker"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["redArrowsModule"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["romanArtModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["romanNumeralsModule"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1.2" };
		ModComponentSolverInformation["simonSelectsModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["StareModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["sync125_3"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["transmittedMorseModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["vectorsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["yellowArrowsModule"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["blinkstopModule"] = new ModuleInformation { statusLightPosition = StatusLightPosition.TopLeft, DoesTheRightThing = true };

		//Fixdoll
		ModComponentSolverInformation["curriculum"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "12" };

		//Flamanis
		ModComponentSolverInformation["ChessModule"] = new ModuleInformation { scoreString = "8", helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", DoesTheRightThing = false };
		ModComponentSolverInformation["Laundry"] = new ModuleInformation { scoreString = "11", helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa", DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { scoreString = "7", helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.", DoesTheRightThing = true };

		//GHXX
		ModComponentSolverInformation["characterCodes"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["thedealmaker"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };

		//Goofy
		ModComponentSolverInformation["elderFuthark"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["harmonySequence"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["leftandRight"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["megaMan2"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["melodySequencer"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["simonSounds"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["stackem"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//Groover
		ModComponentSolverInformation["3dTunnels"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["logicGates"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["rubiksClock"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["shikaku"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["simonSamples"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["turtleRobot"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };

		//Hexicube
		ModComponentSolverInformation["MemoryV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Me Not", scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["KeypadV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Round Keypad", scoreString = "6" };
		ModComponentSolverInformation["ButtonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Square Button", scoreString = "6" };
		ModComponentSolverInformation["SimonV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Simon States", scoreString = "6" };
		ModComponentSolverInformation["PasswordV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Safety Safe", scoreString = "10" };
		ModComponentSolverInformation["MorseV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Morsematics", scoreString = "10" };
		ModComponentSolverInformation["HexiEvilFMN"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Forget Everything", scoreString = "S1.5", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["NeedyVentV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Answering Questions", scoreString = "D 0.8", manualCode = "Answering Questions" };
		ModComponentSolverInformation["NeedyKnobV2"] = new ModuleInformation { DoesTheRightThing = true, moduleDisplayName = "Needy Rotary Phone", scoreString = "D 1.4" };

		//hockeygoalie78
		ModComponentSolverInformation["daylightDirections"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["riskyWires"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };

		//JerryErris
		ModComponentSolverInformation["arithmelogic"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["desertBus"] = new ModuleInformation { scoreString = "T 0.01", DoesTheRightThing = false };
		ModComponentSolverInformation["digitString"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["footnotes"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["forgetThis"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["gryphons"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["qFunctions"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["qSchlagDenBomb"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "12" };
		ModComponentSolverInformation["qSwedishMaze"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "12" };
		ModComponentSolverInformation["quizBuzz"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["simonStops"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["theTriangleButton"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//Kaito Sinclaire
		ModComponentSolverInformation["ksmAmazeingButtons"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["ksmBobBarks"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["ksmHighScore"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["ksmSimonLitSays"] = new ModuleInformation { scoreString = "D 0.5", DoesTheRightThing = true };
		ModComponentSolverInformation["ksmTetraVex"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//KingBranBran
		ModComponentSolverInformation["intervals"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = false };
		ModComponentSolverInformation["pieModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["tapCode"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["valves"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["visual_impairment"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };

		//KingSlendy
		ModComponentSolverInformation["ColorfulInsanity"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["ColorfulMadness"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["FlashMemory"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["PartyTime"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["ShapesBombs"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["SueetWall"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TenButtonColorCode"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["Wavetapping"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };

		//Kritzy
		ModComponentSolverInformation["Krit4CardMonte"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["KritBlackjack"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["KritConnectionDev"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["KritCMDPrompt"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1" };
		ModComponentSolverInformation["KritFlipTheCoin"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1.1" };
		ModComponentSolverInformation["KritHoldUps"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["KritHomework"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["KritMicroModules"] = new ModuleInformation { scoreString = "17", DoesTheRightThing = false };
		ModComponentSolverInformation["KritRadio"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = false };
		ModComponentSolverInformation["KritScripts"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };

		//LeGeND
		ModComponentSolverInformation["lgndAnnoyingArrows"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 0.7" };
		ModComponentSolverInformation["lgndColoredKeys"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndColorMatch"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 0.5" };
		ModComponentSolverInformation["lgndEightPages"] = new ModuleInformation { scoreString = "D 0.6", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndGadgetronVendor"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndHiddenColors"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndLEDMath"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndLombaxCubes"] = new ModuleInformation { scoreString = "16", DoesTheRightThing = true };
		ModComponentSolverInformation["lgndSnap"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 0.6" };
		ModComponentSolverInformation["lgndTerrariaQuiz"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 0.7" };
		ModComponentSolverInformation["lgndZoni"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//lingomaniac88
		ModComponentSolverInformation["ingredients"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["legendreSymbol"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };

		//Livio
		ModComponentSolverInformation["theCodeModule"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["DrDoctorModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };

		//Maca
		ModComponentSolverInformation["Playfair"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true, moduleDisplayName = "Playfair Cipher" };
		ModComponentSolverInformation["unfairCipher"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };

		//Marksam32
		ModComponentSolverInformation["burglarAlarm"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["cooking"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["CrackboxModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TheDigitModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["logicalButtonsModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["mashematics"] = new ModuleInformation { scoreString = "2", DoesTheRightThing = true };
		ModComponentSolverInformation["SplittingTheLootModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["Yoinkingmodule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//McNiko67
		ModComponentSolverInformation["Backgrounds"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["BigSwitch"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = false };
		ModComponentSolverInformation["BlindMaze"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["FaultyBackgrounds"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["FontSelect"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["MazeScrambler"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["Sink"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };

		//MrMelon
		ModComponentSolverInformation["colourcode"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = false };
		ModComponentSolverInformation["planets"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };

		//MrSpekCraft
		ModComponentSolverInformation["periodicTable"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["vexillology"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };

		//NoahCoolBoy
		ModComponentSolverInformation["pigpenRotations"] = new ModuleInformation { scoreString = "8", helpTextOverride = true, helpText = "To submit abcdefhijklm use '!{0} abcdefhijklm'.", DoesTheRightThing = true };
		ModComponentSolverInformation["simonScrambles"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };

		//Piggered
		ModComponentSolverInformation["FlagsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["NonogramModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = false };

		//Procyon
		ModComponentSolverInformation["alphaBits"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = false };
		ModComponentSolverInformation["partialDerivatives"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };

		//Qkrisi
		ModComponentSolverInformation["booleanWires"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["qkForgetPerspective"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["qkTernaryConverter"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };

		//red031000
		ModComponentSolverInformation["digitalRoot"] = new ModuleInformation { scoreString = "1", DoesTheRightThing = true };
		ModComponentSolverInformation["HotPotato"] = new ModuleInformation { DoesTheRightThing = true };
		ModComponentSolverInformation["theNumber"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["PurgatoryModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["radiator"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["wastemanagement"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };

		//Red Penguin
		ModComponentSolverInformation["coloredMaze"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["encryptionBingo"] = new ModuleInformation { scoreString = "S1", announceModule = true, CameraPinningAlwaysAllowed = true, DoesTheRightThing = true };
		ModComponentSolverInformation["fruits"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["moduleMovements"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["robotProgramming"] = new ModuleInformation { scoreString = "19", DoesTheRightThing = true };
		ModComponentSolverInformation["simonSimons"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };

		//Riverbui
		ModComponentSolverInformation["dominoes"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["FaultySink"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["insanetalk"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["mineseeker"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["ModuleMaze"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["USA"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };

		//Royal_Flu$h
		ModComponentSolverInformation["accumulation"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["algebra"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["alphabetNumbers"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["benedictCumberbatch"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["blockbusters"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["britishSlang"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["catchphrase"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["christmasPresents"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = false };
		ModComponentSolverInformation["countdown"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["cruelCountdown"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["cube"] = new ModuleInformation { scoreString = "20", DoesTheRightThing = true };
		ModComponentSolverInformation["europeanTravel"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["flashingLights"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["freeParking"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["graffitiNumbers"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["guitarChords"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["horribleMemory"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["identityParade"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["iPhone"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "10" };
		ModComponentSolverInformation["jackOLantern"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "4" };
		ModComponentSolverInformation["jewelVault"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["jukebox"] = new ModuleInformation { scoreString = "2", DoesTheRightThing = true };
		ModComponentSolverInformation["ledGrid"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["lightspeed"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["londonUnderground"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["maintenance"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = false };
		ModComponentSolverInformation["modulo"] = new ModuleInformation { scoreString = "2", DoesTheRightThing = false };
		ModComponentSolverInformation["moon"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["mortalKombat"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["numberCipher"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["plungerButton"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["Poker"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["quintuples"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["retirement"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = false };
		ModComponentSolverInformation["reverseMorse"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["simonsStar"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["skyrim"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "12" };
		ModComponentSolverInformation["snooker"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["sonic"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["sphere"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["stockMarket"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["stopwatch"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["sun"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicCoordinates"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["taxReturns"] = new ModuleInformation { scoreString = "18", DoesTheRightThing = true, announceModule = true };
		ModComponentSolverInformation["theSwan"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true, CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["wire"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = false };
		ModComponentSolverInformation["wireSpaghetti"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };

		//samfundev
		ModComponentSolverInformation["BrokenButtonsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["CheapCheckoutModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["CreationModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedEquationsModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedValuesModule"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1.8" };
		ModComponentSolverInformation["TheGamepadModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["MinesweeperModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["SkewedSlotsModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["SynchronizationModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };

		//Sean Obach
		ModComponentSolverInformation["blackCipher"] = new ModuleInformation { scoreString = "18", DoesTheRightThing = true };
		ModComponentSolverInformation["blueCipher"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["forgetEnigma"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["grayCipher"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["greenCipher"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["indigoCipher"] = new ModuleInformation { scoreString = "17", DoesTheRightThing = true };
		ModComponentSolverInformation["orangeCipher"] = new ModuleInformation { scoreString = "17", DoesTheRightThing = true };
		ModComponentSolverInformation["redCipher"] = new ModuleInformation { scoreString = "16", DoesTheRightThing = true };
		ModComponentSolverInformation["toonEnough"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["ultimateCipher"] = new ModuleInformation { scoreString = /* TEMP */ "30", DoesTheRightThing = true };
		ModComponentSolverInformation["violetCipher"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["whiteCipher"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["yellowCipher"] = new ModuleInformation { scoreString = "16", DoesTheRightThing = true };

		//short_c1rcuit
		ModComponentSolverInformation["divisibleNumbers"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["keypadCombinations"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["keypadLock"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };

		//SL7205
		ModComponentSolverInformation["colormath"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["fastMath"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["http"] = new ModuleInformation { scoreString = "D 1.2", DoesTheRightThing = true, };
		ModComponentSolverInformation["Logic"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["neutralization"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["QRCode"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1.5", };
		ModComponentSolverInformation["screw"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TextField"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["webDesign"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//Spare Wizard
		ModComponentSolverInformation["spwiz3DMaze"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "12", helpTextOverride = true, helpText = "!{0} move L F R F U [move] | !{0} walk L F R F U [walk slower] [L = left, R = right, F = forward, U = u-turn]" };
		ModComponentSolverInformation["spwizAdventureGame"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true, helpTextOverride = true, helpText = "Cycle the stats with !{0} cycle stats. Cycle the Weapons/Items with !{0} cycle items. Cycle everything with !{0} cycle all. Use weapons/Items with !{0} use potion. Use multiple items with !{0} use ticket, crystal ball, caber. (spell out the item name completely. not case sensitive)" };
		ModComponentSolverInformation["spwizAstrology"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["spwizPerspectivePegs"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["spwizTetris"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 2.5", };

		//Speakingevil
		ModComponentSolverInformation["affineCycle"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = false };
		ModComponentSolverInformation["bamboozledAgain"] = new ModuleInformation { scoreString = "40", DoesTheRightThing = true };
		ModComponentSolverInformation["bamboozlingButton"] = new ModuleInformation { scoreString = "17", DoesTheRightThing = true };
		ModComponentSolverInformation["bamboozlingButtonGrid"] = new ModuleInformation { scoreString = "30", DoesTheRightThing = true };
		ModComponentSolverInformation["borderedKeys"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["caesarCycle"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = false };
		ModComponentSolverInformation["crypticCycle"] = new ModuleInformation { scoreString = "18", DoesTheRightThing = false };
		ModComponentSolverInformation["disorderedKeys"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["doubleArrows"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["faultyrgbMaze"] = new ModuleInformation { scoreString = "20", DoesTheRightThing = true };
		ModComponentSolverInformation["forgetMeLater"] = new ModuleInformation { scoreString = "0", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["hillCycle"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = false };
		ModComponentSolverInformation["jumbleCycle"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = false };
		ModComponentSolverInformation["misorderedKeys"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["orderedKeys"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["pigpenCycle"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = false };
		ModComponentSolverInformation["playfairCycle"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = false };
		ModComponentSolverInformation["recordedKeys"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["reorderedKeys"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["rgbMaze"] = new ModuleInformation { scoreString = "16", DoesTheRightThing = true };
		ModComponentSolverInformation["tallorderedKeys"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["ultimateCycle"] = new ModuleInformation { scoreString = "40", DoesTheRightThing = true };
		ModComponentSolverInformation["UltraStores"] = new ModuleInformation { scoreString = "40", DoesTheRightThing = true };
		ModComponentSolverInformation["unorderedKeys"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["veryAnnoyingButton"] = new ModuleInformation { scoreString = "0", announceModule = true, DoesTheRightThing = true };

		//Strike_Kaboom
		ModComponentSolverInformation["KanjiModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//taggedjc
		//Extended passwords, which is shimmed above.
		ModComponentSolverInformation["hunting"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };

		//TasThing
		ModComponentSolverInformation["chineseCounting"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["loopover"] = new ModuleInformation { scoreString = "11" /*, DoesTheRightThing = ??? */ };
		ModComponentSolverInformation["NandMs"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };

		//ThatGuyCalledJules
		ModComponentSolverInformation["PressX"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["synonyms"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };

		//Theta
		ModComponentSolverInformation["boolMazeCruel"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["sevenChooseFour"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };

		//TheThirdMan
		ModComponentSolverInformation["bombDiffusal"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["bootTooBig"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["constellations"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["deckOfManyThings"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["doubleExpert"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["forgetThemAll"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["geneticSequence"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["giantsDrink"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["graphicMemory"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["heraldry"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };
		ModComponentSolverInformation["langtonAnt"] = new ModuleInformation { scoreString = "17", DoesTheRightThing = true };
		ModComponentSolverInformation["luckyDice"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["maze3"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["modkit"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["moduleListening"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["morseButtons"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["oldFogey"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["qwirkle"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["raidingTemples"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["sevenDeadlySins"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicColouring"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["towerOfHanoi"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["treasureHunt"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = false };

		//Timwi (includes Perky/Konqi/Eluminate/Mitterdoo/Riverbui modules maintained by Timwi)
		ModComponentSolverInformation["AdjacentLettersModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["AdjacentLettersModule_Rus"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["alphabet"] = new ModuleInformation { moduleDisplayName = "Alphabet", scoreString = "2", DoesTheRightThing = true };
		ModComponentSolverInformation["BattleshipModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["BinaryPuzzleModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["BitmapsModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["BlackHoleModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["BlindAlleyModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["BrailleModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["BrokenGuitarChordsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TheBulbModule"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["CaesarCipherModule"] = new ModuleInformation { scoreString = "3", DoesTheRightThing = true };
		ModComponentSolverInformation["TheClockModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSquaresModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["ColoredSwitchesModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["CoordinatesModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["CornersModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true, statusLightPosition = StatusLightPosition.Center };
		ModComponentSolverInformation["CursedDoubleOhModule"] = new ModuleInformation { scoreString = "13", DoesTheRightThing = true };
		ModComponentSolverInformation["DecoloredSquaresModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["DiscoloredSquaresModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["DividedSquaresModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true, announceModule = true };
		ModComponentSolverInformation["DoubleOhModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["FollowTheLeaderModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["FriendshipModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["GridlockModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["HexamazeModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["HogwartsModule"] = new ModuleInformation { scoreString = "10", announceModule = true, DoesTheRightThing = true };
		ModComponentSolverInformation["HumanResourcesModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TheHypercubeModule"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };
		ModComponentSolverInformation["KudosudokuModule"] = new ModuleInformation { scoreString = "16", DoesTheRightThing = true };
		ModComponentSolverInformation["lasers"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["LightCycleModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["LionsShareModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["MafiaModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["MahjongModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["MarbleTumbleModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["MaritimeFlagsModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["MouseInTheMaze"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["MysticSquareModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["NumberPad"] = new ModuleInformation { moduleDisplayName = "Number Pad", scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["OddOneOutModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["OneHundredAndOneDalmatiansModule"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["OnlyConnectModule"] = new ModuleInformation { scoreString = "11", DoesTheRightThing = true };
		ModComponentSolverInformation["PatternCubeModule"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["PerplexingWiresModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["PointOfOrderModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["PolyhedralMazeModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["RegularCrazyTalkModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["RockPaperScissorsLizardSpockModule"] = new ModuleInformation { scoreString = "6", manualCode = "Rock-Paper-Scissors-Lizard-Spock", DoesTheRightThing = true };
		ModComponentSolverInformation["RubiksCubeModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["SetModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["SillySlots"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSendsModule"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };
		ModComponentSolverInformation["SimonShrieksModule"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSingsModule"] = new ModuleInformation { scoreString = "14", DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSpeaksModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["SimonSpinsModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["SouvenirModule"] = new ModuleInformation { scoreString = "0", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = true, unclaimable = true };
		ModComponentSolverInformation["SuperlogicModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["SymbolCycleModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["TennisModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["TicTacToeModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["TheUltracubeModule"] = new ModuleInformation { scoreString = "20", DoesTheRightThing = true };
		ModComponentSolverInformation["UncoloredSquaresModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["WirePlacementModule"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["WordSearchModule"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["XRayModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["YahtzeeModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["ZooModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };

		//Trainzack
		ModComponentSolverInformation["ChordQualities"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["MusicRhythms"] = new ModuleInformation { scoreString = "7", helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", DoesTheRightThing = false };

		//Virepri
		ModComponentSolverInformation["BitOps"] = new ModuleInformation { scoreString = "6", helpText = "Submit the correct answer with !{0} submit 10101010.", validCommands = new[] { "^submit [0-1]{8}$" }, DoesTheRightThing = true };
		ModComponentSolverInformation["LEDEnc"] = new ModuleInformation { scoreString = "6", helpText = "Press the button with label B with !{0} press b.", DoesTheRightThing = true };

		//Windesign
		ModComponentSolverInformation["Color Decoding"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true, moduleDisplayName = "Color Decoding" };
		ModComponentSolverInformation["GridMatching"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true, helpText = "Commands are “left/right/up/down/clockwise/counter-clockwise/submit” or “l/r/u/d/cw/ccw/s”. The letter can be set by using “set d” or “'d'”. All of these can be chained, for example: “!{0} up right right clockwise 'd' submit”. You can only use one letter-setting command at a time." };

		//ZekNikZ
		ModComponentSolverInformation["booleanVennModule"] = new ModuleInformation { scoreString = "7", helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none).", DoesTheRightThing = true };
		ModComponentSolverInformation["buttonSequencesModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["ColorMorseModule"] = new ModuleInformation { scoreString = "10", DoesTheRightThing = true };
		ModComponentSolverInformation["complicatedButtonsModule"] = new ModuleInformation { scoreString = "5", helpText = "Press the top button with !{0} press top (also t, 1, etc.).", DoesTheRightThing = true };
		ModComponentSolverInformation["fizzBuzzModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["iceCreamModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };
		ModComponentSolverInformation["symbolicPasswordModule"] = new ModuleInformation { scoreString = "6", helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!", DoesTheRightThing = true };
		ModComponentSolverInformation["VaricoloredSquaresModule"] = new ModuleInformation { scoreString = "9", DoesTheRightThing = true };

		//Other modded modules not built into Twitch Plays
		ModComponentSolverInformation["aa"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 1.3" };
		ModComponentSolverInformation["BartendingModule"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["BinaryLeds"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["BooleanKeypad"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["blockStacks"] = new ModuleInformation { DoesTheRightThing = true, scoreString = "D 0.6" };
		ModComponentSolverInformation["buttonMasherNeedy"] = new ModuleInformation { scoreString = "D 0.5", moduleDisplayName = "Needy Button Masher", helpText = "Press the button 20 times with !{0} press 20", DoesTheRightThing = true, manualCode = "Button Masher" };
		ModComponentSolverInformation["combinationLock"] = new ModuleInformation { scoreString = "5", helpText = "Submit the code using !{0} submit 1 2 3.", DoesTheRightThing = false };
		ModComponentSolverInformation["DateFinder"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["EncryptedMorse"] = new ModuleInformation { scoreString = "15", DoesTheRightThing = true };
		ModComponentSolverInformation["EternitySDec"] = new ModuleInformation { DoesTheRightThing = false, scoreString = "D 1" };
		ModComponentSolverInformation["forgetUsNot"] = new ModuleInformation { scoreString = "S1", CameraPinningAlwaysAllowed = true, announceModule = true, DoesTheRightThing = false };
		ModComponentSolverInformation["groceryStore"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true, helpText = "Use !{0} add item to cart | Adds an item to the cart. Use !{0} pay and leave | Pays and leaves | Commands can be abbreviated with !{0} add & !{0} pay" };
		ModComponentSolverInformation["manometers"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["mazematics"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["meter"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["modernCipher"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["R4YNeedyFlowerMash"] = new ModuleInformation { scoreString = "D 0.5", DoesTheRightThing = true };
		ModComponentSolverInformation["Numbers"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["passportControl"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["needyPiano"] = new ModuleInformation { DoesTheRightThing = false, scoreString = "D 1" };
		ModComponentSolverInformation["safetySquare"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["SamRedButtons"] = new ModuleInformation { scoreString = "6", DoesTheRightThing = true };
		ModComponentSolverInformation["sevenWires"] = new ModuleInformation { scoreString = "4", DoesTheRightThing = true };
		ModComponentSolverInformation["Signals"] = new ModuleInformation { scoreString = "8", DoesTheRightThing = true };
		ModComponentSolverInformation["simonStores"] = new ModuleInformation { scoreString = "25", DoesTheRightThing = true };
		ModComponentSolverInformation["thinkingWiresModule"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["timezone"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["thewitness"] = new ModuleInformation { scoreString = "5", DoesTheRightThing = true };
		ModComponentSolverInformation["vigenereCipher"] = new ModuleInformation { scoreString = "7", DoesTheRightThing = true };
		ModComponentSolverInformation["X01"] = new ModuleInformation { scoreString = "12", DoesTheRightThing = true };
		ModComponentSolverInformation["mysterymodule"] = new ModuleInformation { DoesTheRightThing = false, CameraPinningAlwaysAllowed = true, announceModule = true, unclaimable = true };

		foreach (KeyValuePair<string, ModuleInformation> kvp in ModComponentSolverInformation)
		{
			ModComponentSolverInformation[kvp.Key].moduleID = kvp.Key;
			AddDefaultModuleInformation(kvp.Value);
		}
	}

	public static Dictionary<string, string> rewardBonuses = new Dictionary<string, string>();

	public static IEnumerator LoadDefaultInformation(bool reloadData = false)
	{
		UnityWebRequest www = UnityWebRequest.Get("https://spreadsheets.google.com/feeds/list/1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA/1/public/values?alt=json");

		yield return www.SendWebRequest();

		if (!www.isNetworkError && !www.isHttpError)
		{
			var displayNames = new List<string>();
			foreach (var entry in JObject.Parse(www.downloadHandler.text)["feed"]["entry"])
			{
				string scoreString = entry["gsx$tpscore"]["$t"]?.Value<string>();
				if (string.IsNullOrEmpty(scoreString))
					continue;

				string moduleName = entry["gsx$modulename"].Value<string>("$t");
				if (string.IsNullOrEmpty(moduleName) || moduleName == "NEEDIES")
					continue;

				string rewardString = entry["gsx$bombreward"].Value<string>("$t");
				// (Is allowed to be null or "")

				string normalize(string value) => value.ToLowerInvariant().Replace('’', '\'');
				bool equalNames(string nameA, string nameB) => normalize(nameA) == normalize(nameB);

				string moduleID = null;
				var bombComponent = ModManager.Instance.GetValue<Dictionary<string, BombComponent>>("loadedBombComponents").Values.FirstOrDefault(module => equalNames(module.GetModuleDisplayName(), moduleName));
				if (bombComponent?.ComponentType.EqualsAny(ComponentTypeEnum.Mod, ComponentTypeEnum.NeedyMod) == true)
				{
					moduleID = bombComponent.GetComponent<KMBombModule>()?.ModuleType ?? bombComponent.GetComponent<KMNeedyModule>()?.ModuleType;
				}

				if (moduleID == null)
					moduleID = Array.Find(GetModuleInformation(), info => equalNames(info.moduleDisplayName, moduleName))?.moduleID;

				if (moduleID == null)
				{
					displayNames.Add(moduleName);

					continue;
				}

				// The Module ID has been determined, now parse the score.
				var defaultInfo = GetDefaultInformation(moduleID);

				rewardBonuses[moduleID] = rewardString;

				// Catch any TDB modules which can't be parsed.
				if (scoreString == "TBD")
					continue;

				// UN and T is for unchanged and temporary score which are read normally.
				scoreString = Regex.Replace(scoreString, @"(?:UN )?(\d+)T?", "$1");

				defaultInfo.scoreString = scoreString;
			}

			if (displayNames.Count > 0)
			{
				DebugHelper.Log("Unable to match these modules when loading the default information:", displayNames.Join(", "));
			}
		}
		else
		{
			DebugHelper.Log("Failed to load the default module information.");
		}

		if (reloadData)
			ModuleData.LoadDataFromFile();
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
				scoreString = info.scoreString,
				scoreStringOverride = false,
				statusLightPosition = info.statusLightPosition,
				unclaimedColor = info.unclaimedColor,
				validCommands = info.validCommands,
				validCommandsOverride = false
			};
		}
	}

	private static void AddDefaultModuleInformation(string moduleType, string moduleDisplayName, string helpText, string manualCode, string[] regexList)
	{
		if (string.IsNullOrEmpty(moduleType)) return;
		AddDefaultModuleInformation(GetModuleInfo(moduleType));
		ModuleInformation info = DefaultModComponentSolverInformation[moduleType];
		info.moduleDisplayName = moduleDisplayName;
		if (!string.IsNullOrEmpty(helpText)) info.helpText = helpText;
		if (!string.IsNullOrEmpty(manualCode)) info.manualCode = manualCode;
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

		if (!info.helpTextOverride && !string.IsNullOrEmpty(defInfo.helpText))
		{
			ModuleData.DataHasChanged |= !info.helpText.TryEquals(defInfo.helpText);
			info.helpText = defInfo.helpText;
		}

		if (!info.scoreStringOverride)
		{
			ModuleData.DataHasChanged |= !info.scoreString.Equals(defInfo.scoreString);
			info.scoreString = defInfo.scoreString;
		}

		if (!info.manualCodeOverride)
		{
			ModuleData.DataHasChanged |= !info.manualCode.TryEquals(defInfo.manualCode);
			info.manualCode = defInfo.manualCode;
		}

		if (writeData && !info.builtIntoTwitchPlays)
			ModuleData.WriteDataToFile();

		return ModComponentSolverInformation[moduleType];
	}

	public static ModuleInformation GetModuleInfo(string moduleType, string helpText, string manualCode = null)
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

		defInfo.helpText = helpText;
		defInfo.manualCode = manualCode;

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

			i.scoreString = info.scoreString;
			i.announceModule = info.announceModule;
			i.unclaimable = info.unclaimable;

			i.scoreStringOverride = info.scoreStringOverride;
			i.helpTextOverride = info.helpTextOverride;
			i.manualCodeOverride = info.manualCodeOverride;
			i.statusLightPosition = info.statusLightPosition;

			if (!i.builtIntoTwitchPlays)
			{
				i.validCommandsOverride = info.validCommandsOverride;
				i.DoesTheRightThing |= info.DoesTheRightThing;
				i.validCommands = info.validCommands;
			}

			i.unclaimedColor = info.unclaimedColor;

			i.additionalNeedyTime = info.additionalNeedyTime;
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

		if (FindModuleScore(module.BombComponent, commandComponentType, out int score) && !info.scoreStringOverride)
		{
			ModuleData.DataHasChanged |= !score.Equals(info.scoreString);
			info.scoreString = score.ToString();
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

		AddDefaultModuleInformation(moduleType, displayName, help, manual, regexList);

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
				if (component == null)
					continue;

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
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not log the component types due to an exception:");
		}
	}

	private static bool FindRegexList(Component bombComponent, Type commandComponentType, out string[] validCommands)
	{
		FieldInfo candidateString = commandComponentType?.GetDeepField("TwitchValidCommands", fieldFlags);
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
		FieldInfo candidateInt = commandComponentType?.GetDeepField("TwitchModuleScore", fieldFlags);
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
		FieldInfo candidateInt = commandComponentType?.GetDeepField("TwitchStrikePenalty", fieldFlags);
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
		FieldInfo cancelField = commandComponentType?.GetDeepField("TwitchHelpMessage", fieldFlags);
		return cancelField?.FieldType == typeof(string) ? cancelField : null;
	}

	private static FieldInfo FindManualCode(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetDeepField("TwitchManualCode", fieldFlags);
		return cancelField?.FieldType == typeof(string) ? cancelField : null;
	}

	private static FieldInfo FindCancelBool(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetDeepField("TwitchShouldCancelCommand", fieldFlags);
		return cancelField?.FieldType == typeof(bool) ? cancelField : null;
	}

	private static FieldInfo FindZenModeBool(Type commandComponentType)
	{
		FieldInfo zenField = commandComponentType?.GetDeepField("TwitchZenMode", fieldFlags) ??
							commandComponentType?.GetDeepField("ZenModeActive", fieldFlags);
		return zenField?.FieldType == typeof(bool) ? zenField : null;
	}

	private static FieldInfo FindTimeModeBool(Type commandComponentType)
	{
		FieldInfo timeField = commandComponentType?.GetDeepField("TwitchTimeMode", fieldFlags) ??
							commandComponentType?.GetDeepField("TimeModeActive", fieldFlags);
		return timeField?.FieldType == typeof(bool) ? timeField : null;
	}

	private static FieldInfo FindTwitchPlaysBool(Type commandComponentType)
	{
		FieldInfo twitchPlaysActiveField = commandComponentType?.GetDeepField("TwitchPlaysActive", fieldFlags);
		return twitchPlaysActiveField?.FieldType == typeof(bool) ? twitchPlaysActiveField : null;
	}

	private static FieldInfo FindTwitchPlaysSkipTimeBool(Type commandComponentType)
	{
		FieldInfo twitchPlaysActiveField = commandComponentType?.GetDeepField("TwitchPlaysSkipTimeAllowed", fieldFlags);
		return twitchPlaysActiveField?.FieldType == typeof(bool) ? twitchPlaysActiveField : null;
	}

	private static MethodInfo FindSolveMethod(Component bombComponent, ref Type commandComponentType)
	{
		if (commandComponentType == null)
		{
			Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
			foreach (Component component in allComponents)
			{
				if (component == null)
					continue;

				Type type = component.GetType();
				MethodInfo candidateMethod = Array.Find(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x => (x.ReturnType == typeof(void) || x.ReturnType == typeof(IEnumerator)) && x.GetParameters().Length == 0 && x.Name.Equals("TwitchHandleForcedSolve"));
				if (candidateMethod == null) continue;

				commandComponentType = type;
				return candidateMethod;
			}

			return null;
		}

		return Array.Find(commandComponentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), x => (x.ReturnType == typeof(void) || x.ReturnType == typeof(IEnumerator)) && x.GetParameters().Length == 0 && x.Name.Equals("TwitchHandleForcedSolve"));
	}

	private static FieldInfo FindAbandonModuleList(Type commandComponentType)
	{
		FieldInfo cancelField = commandComponentType?.GetDeepField("TwitchAbandonModule", fieldFlags);
		return cancelField?.FieldType == typeof(List<KMBombModule>) ? cancelField : null;
	}

	private static MethodInfo FindProcessCommandMethod(Component bombComponent, out ModCommandType commandType, out Type commandComponentType)
	{
		Component[] allComponents = bombComponent.GetComponentsInChildren<Component>(true);
		foreach (Component component in allComponents)
		{
			if (component == null)
				continue;

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
