using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
	private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
	private static readonly Dictionary<string, ModuleInformation> ModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private static readonly Dictionary<string, ModuleInformation> DefaultModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

	static ComponentSolverFactory()
	{
		//AT_Bash Modules
		ModComponentSolverCreators["MotionSense"] = module => new MotionSenseComponentSolver(module);
		ModComponentSolverCreators["AppreciateArt"] = Module => new AppreciateArtComponentSolver(Module);
		ModComponentSolverCreators["Painting"] = Module => new PaintingShim(Module);

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
		ModComponentSolverCreators["lgndPayRespects"] = module => new PayRespectsComponentSolver(module);

		//Lone Modules
		ModComponentSolverCreators["tripleVision"] = module => new TripleVisionComponentSolver(module);
		ModComponentSolverCreators["SIHTS"] = module => new SIHTSComponentSolver(module);
		ModComponentSolverCreators["doubleMaze"] = module => new DoubleMazeComponentSolver(module);
		ModComponentSolverCreators["logicPlumbing"] = module => new LogicPlumbingComponentSolver(module);
		ModComponentSolverCreators["flashingCube"] = module => new FlashingCubeComponentSolver(module);

		//Asimir Modules
		ModComponentSolverCreators["murder"] = module => new MurderComponentSolver(module);
		ModComponentSolverCreators["SeaShells"] = module => new SeaShellsComponentSolver(module);
		ModComponentSolverCreators["shapeshift"] = module => new ShapeShiftComponentSolver(module);
		ModComponentSolverCreators["ThirdBase"] = module => new ThirdBaseComponentSolver(module);

		//Mock Army Modules
		ModComponentSolverCreators["AnagramsModule"] = module => new AnagramsComponentSolver(module);
		ModComponentSolverCreators["Emoji Math"] = module => new EmojiMathComponentSolver(module);
		ModComponentSolverCreators["Filibuster"] = module => new FilibusterComponentSolver(module);
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

		//Hockeygoalie78 Modules
		ModComponentSolverCreators["CrypticPassword"] = module => new CrypticPasswordComponentSolver(module);
		ModComponentSolverCreators["modulusManipulation"] = module => new ModulusManipulationComponentSolver(module);
		ModComponentSolverCreators["triangleButtons"] = module => new TriangleButtonsComponentSolver(module);

		//GoodHood Modules
		ModComponentSolverCreators["buttonOrder"] = module => new ButtonOrderComponentSolver(module);
		ModComponentSolverCreators["pressTheShape"] = module => new PressTheShapeComponentSolver(module);
		ModComponentSolverCreators["standardButtonMasher"] = module => new StandardButtonMasherComponentSolver(module);
		ModComponentSolverCreators["BinaryButtons"] = module => new BinaryButtonsComponentSolver(module);

		//Elias Modules
		ModComponentSolverCreators["numberNimbleness"] = module => new NumberNimblenessComponentSolver(module);
		ModComponentSolverCreators["matchmaker"] = module => new MatchmakerComponentSolver(module);

		//BakersDozenBagels Modules
		ModComponentSolverCreators["xModule"] = module => new XandYComponentSolver(module);
		ModComponentSolverCreators["yModule"] = module => new XandYComponentSolver(module);
		ModComponentSolverCreators["imbalance"] = module => new ImbalanceComponentSolver(module);
		ModComponentSolverCreators["shaker"] = module => new ShakerComponentSolver(module);

		//TheCrazyCodr Modules
		ModComponentSolverCreators["sqlBasic"] = module => new SQLBasicComponentSolver(module);
		ModComponentSolverCreators["sqlEvil"] = module => new SQLEvilComponentSolver(module);
		ModComponentSolverCreators["sqlCruel"] = module => new SQLCruelComponentSolver(module);

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
		ModComponentSolverCreators["needyHotate"] = module => new NeedyHotateComponentSolver(module);
		ModComponentSolverCreators["pinkArrows"] = module => new PinkArrowsComponentSolver(module);
		ModComponentSolverCreators["CactusPConundrum"] = module => new CactiConundrumComponentSolver(module);
		ModComponentSolverCreators["weekDays"] = module => new WeekdaysComponentSolver(module);
		ModComponentSolverCreators["draw"] = module => new DrawComponentSolver(module);
		ModComponentSolverCreators["overKilo"] = module => new OverKiloComponentSolver(module);
		ModComponentSolverCreators["gemory"] = module => new GemoryComponentSolver(module);
		ModComponentSolverCreators["parliament"] = module => new ParliamentComponentSolver(module);
		ModComponentSolverCreators["12321"] = module => new OneTwoThreeComponentSolver(module);
		ModComponentSolverCreators["TechSupport"] = module => new TechSupportComponentSolver(module);
		ModComponentSolverCreators["factoryCode"] = module => new FactoryCodeComponentSolver(module);
		ModComponentSolverCreators["SpellingBuzzed"] = module => new SpellingBuzzedComponentSolver(module);
		ModComponentSolverCreators["BackdoorHacking"] = module => new BackdoorHackingComponentSolver(module);
		ModComponentSolverCreators["forget_fractal"] = module => new ForgetFractalComponentSolver(module);
		ModComponentSolverCreators["NeedyPong"] = module => new PongComponentSolver(module);
		ModComponentSolverCreators["needycrafting"] = module => new CraftingTableComponentSolver(module);
		ModComponentSolverCreators["bigeggs"] = module => new PerspectiveEggsComponentSolver(module);
		ModComponentSolverCreators["GL_nokiaModule"] = module => new NokiaComponentSolver(module);
		ModComponentSolverCreators["lookLookAway"] = module => new LookLookAwayComponentSolver(module);

		//ZekNikZ Modules
		ModComponentSolverCreators["EdgeworkModule"] = module => new EdgeworkComponentSolver(module);
		ModComponentSolverCreators["LEGOModule"] = module => new LEGOComponentSolver(module);

		//Speakingevil Modules
		ModComponentSolverCreators["runeMatchI"] = module => new RuneMatchIComponentSolver(module);
		ModComponentSolverCreators["runeMatchII"] = module => new RuneMatchIIComponentSolver(module);
		ModComponentSolverCreators["runeMatchIII"] = module => new RuneMatchIIIComponentSolver(module);

		//StrangaDanga Modules
		ModComponentSolverCreators["keepClicking"] = module => new KeepClickingComponentSolver(module);
		ModComponentSolverCreators["sixteenCoins"] = module => new SixteenCoinsComponentSolver(module);

		//TheDarkSid3r Modules
		ModComponentSolverCreators["NotTimerModule"] = module => new NotTimerComponentSolver(module);
		ModComponentSolverCreators["TDSAmogus"] = module => new AmogusComponentSolver(module);
		ModComponentSolverCreators["TDSNya"] = module => new NyaComponentSolver(module);
		ModComponentSolverCreators["IconReveal"] = module => new IconRevealComponentSolver(module);
		ModComponentSolverCreators["FreePassword"] = module => new FreePasswordComponentSolver(module);
		ModComponentSolverCreators["LargeFreePassword"] = module => new FreePasswordComponentSolver(module);
		ModComponentSolverCreators["LargeVanillaPassword"] = module => new LargePasswordComponentSolver(module);
		ModComponentSolverCreators["TDSNeedyWires"] = module => new NeedyWiresComponentSolver(module);
		ModComponentSolverCreators["TDSDossierModifier"] = module => new DossierModifierComponentSolver(module);
		ModComponentSolverCreators["ManualCodes"] = module => new ManualCodesComponentSolver(module);
		ModComponentSolverCreators["jackboxServerModule"] = module => new JackboxTVComponentSolver(module);

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
		ModComponentSolverCreators["periodicTable"] = module => new PeriodicTableShim(module);
		ModComponentSolverCreators["vexillology"] = module => new VexillologyShim(module);
		ModComponentSolverCreators["ColorfulMadness"] = module => new ColorfulMadnessShim(module);
		ModComponentSolverCreators["ColorfulInsanity"] = module => new ColorfulInsanityShim(module);
		ModComponentSolverCreators["SueetWall"] = module => new SueetWallShim(module);
		ModComponentSolverCreators["FlashMemory"] = module => new FlashMemoryShim(module);
		ModComponentSolverCreators["ShapesBombs"] = module => new ShapesAndBombsShim(module);
		ModComponentSolverCreators["Wavetapping"] = module => new WavetappingShim(module);
		ModComponentSolverCreators["ColourFlash"] = module => new ColourFlashShim(module);
		ModComponentSolverCreators["ColourFlashPL"] = module => new ColourFlashShim(module);
		ModComponentSolverCreators["ColourFlashES"] = module => new ColourFlashESShim(module);
		ModComponentSolverCreators["Semaphore"] = module => new SemaphoreShim(module);
		//ModComponentSolverCreators["Tangrams"] = module => new TangramsShim(module);
		ModComponentSolverCreators["BinaryLeds"] = module => new BinaryLEDsShim(module);
		ModComponentSolverCreators["timezone"] = module => new TimezoneShim(module);
		ModComponentSolverCreators["quintuples"] = module => new QuintuplesShim(module);
		ModComponentSolverCreators["identityParade"] = module => new IdentityParadeShim(module);
		ModComponentSolverCreators["graffitiNumbers"] = module => new GraffitiNumbersShim(module);
		ModComponentSolverCreators["mortalKombat"] = module => new MortalKombatShim(module);
		ModComponentSolverCreators["ledGrid"] = module => new LEDGridShim(module);
		ModComponentSolverCreators["RGBSequences"] = module => new RGBSequencesShim(module);
		ModComponentSolverCreators["stars"] = module => new StarsShim(module);
		//ModComponentSolverCreators["shikaku"] = module => new ShikakuShim(module);
		ModComponentSolverCreators["osu"] = module => new OsuShim(module);
		ModComponentSolverCreators["minecraftParody"] = module => new MinecraftParodyShim(module);
		ModComponentSolverCreators["minecraftCipher"] = module => new MinecraftCipherShim(module);
		ModComponentSolverCreators["PressX"] = module => new PressXShim(module);
		ModComponentSolverCreators["iPhone"] = module => new IPhoneShim(module);
		ModComponentSolverCreators["constellations"] = module => new ConstellationsShim(module);
		ModComponentSolverCreators["giantsDrink"] = module => new GiantsDrinkShim(module);
		ModComponentSolverCreators["heraldry"] = module => new HeraldryShim(module);
		ModComponentSolverCreators["Color Decoding"] = module => new ColorDecodingShim(module);
		ModComponentSolverCreators["TableMadness"] = module => new TableMadnessShim(module);
		ModComponentSolverCreators["harmonySequence"] = module => new HarmonySequenceShim(module);
		ModComponentSolverCreators["coopharmonySequence"] = module => new CoopHarmonySequenceShim(module);
		ModComponentSolverCreators["safetySquare"] = module => new SafetySquareShim(module);
		ModComponentSolverCreators["lgndEightPages"] = module => new EightPagesShim(module);
		ModComponentSolverCreators["KritLockpickMaze"] = module => new LockpickMazeShim(module);

		// Anti-troll shims - These are specifically meant to allow the troll commands to be disabled.
		ModComponentSolverCreators["MazeV2"] = module => new AntiTrollShim(module, new Dictionary<string, string> { { "spinme", "Sorry, I am not going to waste time spinning every single pipe 360 degrees." } });
		ModComponentSolverCreators["danielDice"] = module => new AntiTrollShim(module, new Dictionary<string, string> { { "rdrts", "Sorry, the secret gambler's room is off limits to you." } });

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
				"statusLightPosition": "Default",
				"validCommandsOverride": false,
				"validCommands": null,
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
		 * Finally, validCommands, CompatibilityMode and all of the override flags will only show up in modules not built into Twitch plays.
		 * validCommandsOverride - Specifies whether the valid regular expression list should not be updated from the module.
		 * validCommands - A list of valid regular expression commands that define if the command should be passed onto the modules Twitch plays handler.
		 *      If null, the command will always be passed on.
		 *      
		 * CompatibilityMode - Set to true if the module does not 'yield return' something before interacting with any objects.
		 *      Forces the module to be focused before commands are processed at all.
		 *      This is what "DoesTheRightThing = false" used to be, but it was renamed to shake off years of cruft.
		 * 
		 * CameraPinningAlwaysAllowed - Defines if a normal user is allowed to use view pin on this module.
		 * 
		 * 
		 */

		//All of these modules are built into Twitch plays.

		//Asimir
		ModComponentSolverInformation["murder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Murder" };
		ModComponentSolverInformation["SeaShells"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Sea Shells" };
		ModComponentSolverInformation["shapeshift"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Shape Shift" };
		ModComponentSolverInformation["ThirdBase"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Third Base" };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["MotionSense"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Motion Sense" };
		ModComponentSolverInformation["AppreciateArt"] = new ModuleInformation { builtIntoTwitchPlays = true, unclaimable = true, moduleDisplayName = "Art Appreciation" };

		//Perky
		ModComponentSolverInformation["CrazyTalk"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Crazy Talk" };
		ModComponentSolverInformation["CryptModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptography" };
		ModComponentSolverInformation["ForeignExchangeRates"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Foreign Exchange Rates" };
		ModComponentSolverInformation["Listening"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Listening" };
		ModComponentSolverInformation["OrientationCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Orientation Cube" };
		ModComponentSolverInformation["Probing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Probing" };
		ModComponentSolverInformation["TurnTheKey"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Key", announceModule = true };
		ModComponentSolverInformation["TurnTheKeyAdvanced"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Turn The Keys", announceModule = true };

		//Kaneb
		ModComponentSolverInformation["TwoBits"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Two Bits" };

		//LeGeND
		ModComponentSolverInformation["lgndAlpha"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alpha" };
		ModComponentSolverInformation["lgndHyperactiveNumbers"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Hyperactive Numbers" };
		ModComponentSolverInformation["lgndMorseIdentification"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Identification" };
		ModComponentSolverInformation["lgndReflex"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Reflex" };
		ModComponentSolverInformation["lgndPayRespects"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Pay Respects" };

		//Lone
		ModComponentSolverInformation["tripleVision"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Triple Vision" };
		ModComponentSolverInformation["SIHTS"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "SI-HTS" };
		ModComponentSolverInformation["doubleMaze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Double Maze" };
		ModComponentSolverInformation["logicPlumbing"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Logic Plumbing" };
		ModComponentSolverInformation["flashingCube"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Flashing Cube" };

		//Mock Army
		ModComponentSolverInformation["AnagramsModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Anagrams" };
		ModComponentSolverInformation["Emoji Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Emoji Math" };
		ModComponentSolverInformation["Filibuster"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Filibuster", unclaimable = true };
		ModComponentSolverInformation["Needy Math"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Math" };
		ModComponentSolverInformation["WordScrambleModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Word Scramble" };

		//Royal_Flu$h
		ModComponentSolverInformation["coffeebucks"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Coffeebucks" };
		ModComponentSolverInformation["festiveJukebox"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Festive Jukebox" };
		ModComponentSolverInformation["hangover"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Hangover" };
		ModComponentSolverInformation["labyrinth"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Labyrinth" };
		ModComponentSolverInformation["matrix"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Matrix" };
		ModComponentSolverInformation["memorableButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memorable Buttons" };
		ModComponentSolverInformation["simonsOnFirst"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon's On First" };
		ModComponentSolverInformation["simonsStages"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon's Stages", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["skinnyWires"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Skinny Wires" };
		ModComponentSolverInformation["stainedGlass"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Stained Glass" };
		ModComponentSolverInformation["streetFighter"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Street Fighter" };
		ModComponentSolverInformation["troll"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Troll", announceModule = true };
		ModComponentSolverInformation["tWords"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "T-Words" };
		ModComponentSolverInformation["primeEncryption"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Prime Encryption" };
		ModComponentSolverInformation["needyMrsBob"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Mrs Bob" };
		ModComponentSolverInformation["simonSquawks"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Squawks" };
		ModComponentSolverInformation["rapidButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Rapid Buttons" };

		//Hockeygoalie78
		ModComponentSolverInformation["CrypticPassword"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cryptic Password" };
		ModComponentSolverInformation["modulusManipulation"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Modulus Manipulation" };
		ModComponentSolverInformation["triangleButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Triangle Buttons" };

		//Elias
		ModComponentSolverInformation["numberNimbleness"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Number Nimbleness", };
		ModComponentSolverInformation["matchmaker"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Matchmaker" };

		//BakersDozenBagels
		ModComponentSolverInformation["xModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "X" };
		ModComponentSolverInformation["yModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Y" };
		ModComponentSolverInformation["imbalance"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Imbalance" };
		ModComponentSolverInformation["shaker"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Shaker" };

		//TheCrazyCodr
		ModComponentSolverInformation["sqlBasic"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "SQL - Basic" };
		ModComponentSolverInformation["sqlEvil"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "SQL - Evil" };
		ModComponentSolverInformation["sqlCruel"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "SQL - Cruel" };

		//Misc
		ModComponentSolverInformation["EnglishTest"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "English Test" };
		ModComponentSolverInformation["LetterKeys"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Letter Keys" };
		ModComponentSolverInformation["Microcontroller"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Microcontroller" };
		ModComponentSolverInformation["resistors"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Resistors" };
		ModComponentSolverInformation["speakEnglish"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Speak English" };
		ModComponentSolverInformation["switchModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Switches" };
		ModComponentSolverInformation["EdgeworkModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Edgework" };
		ModComponentSolverInformation["NeedyBeer"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Refill That Beer!" };
		ModComponentSolverInformation["errorCodes"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Error Codes" };
		ModComponentSolverInformation["JuckAlchemy"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Alchemy" };
		ModComponentSolverInformation["LEGOModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "LEGOs" };
		ModComponentSolverInformation["boolMaze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Boolean Maze" };
		ModComponentSolverInformation["MorseWar"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse War" };
		ModComponentSolverInformation["necronomicon"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Necronomicon" };
		ModComponentSolverInformation["babaIsWho"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Baba Is Who?" };
		ModComponentSolverInformation["chordProgressions"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Chord Progressions" };
		ModComponentSolverInformation["rng"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Random Number Generator", additionalNeedyTime = 30 };
		ModComponentSolverInformation["needyShapeMemory"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Shape Memory" };
		ModComponentSolverInformation["caesarsMaths"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Caesar's Maths" };
		ModComponentSolverInformation["gatekeeper"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Gatekeeper" };
		ModComponentSolverInformation["stateOfAggregation"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "State of Aggregation" };
		ModComponentSolverInformation["conditionalButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Conditional Buttons" };
		ModComponentSolverInformation["strikeSolve"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Strike Solve" };
		ModComponentSolverInformation["abstractSequences"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Abstract Sequences" };
		ModComponentSolverInformation["bridge"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Bridge" };
		ModComponentSolverInformation["needyHotate"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Hotate" };
		ModComponentSolverInformation["pinkArrows"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Pink Arrows" };
		ModComponentSolverInformation["CactusPConundrum"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Cacti's Conundrum" };
		ModComponentSolverInformation["weekDays"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Weekdays" };
		ModComponentSolverInformation["draw"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Draw" };
		ModComponentSolverInformation["overKilo"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Over Kilo" };
		ModComponentSolverInformation["gemory"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Gemory", announceModule = true };
		ModComponentSolverInformation["parliament"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Parliament" };
		ModComponentSolverInformation["12321"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "1-2-3-2-1" };
		ModComponentSolverInformation["TechSupport"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Tech Support" };
		ModComponentSolverInformation["factoryCode"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Factory Code" };
		ModComponentSolverInformation["SpellingBuzzed"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Spelling Buzzed" };
		ModComponentSolverInformation["BackdoorHacking"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Backdoor Hacking" };
		ModComponentSolverInformation["forget_fractal"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Forget Fractal", announceModule = true };
		ModComponentSolverInformation["NeedyPong"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Pong" };
		ModComponentSolverInformation["needycrafting"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Crafting Table" };
		ModComponentSolverInformation["bigeggs"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "perspective eggs" };
		ModComponentSolverInformation["GL_nokiaModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Nokia" };
		ModComponentSolverInformation["lookLookAway"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Look, Look Away" };

		//GoodHood
		ModComponentSolverInformation["buttonOrder"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Button Order" };
		ModComponentSolverInformation["pressTheShape"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Press The Shape" };
		ModComponentSolverInformation["standardButtonMasher"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Standard Button Masher" };
		ModComponentSolverInformation["BinaryButtons"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Binary Buttons" };

		//Steel Crate Games (Need these in place even for the Vanilla modules)
		ModComponentSolverInformation["Wires"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wires" };
		ModComponentSolverInformation["BigButton"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Button" };
		ModComponentSolverInformation["BigButtonModified"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "The Button" };
		ModComponentSolverInformation["WireSequence"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Wire Sequence" };
		ModComponentSolverInformation["WhosOnFirst"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First" };
		ModComponentSolverInformation["Venn"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Complicated Wires" };
		ModComponentSolverInformation["Simon"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Simon Says" };
		ModComponentSolverInformation["Password"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password" };
		ModComponentSolverInformation["NeedyVentGas"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Vent Gas" };
		ModComponentSolverInformation["NeedyKnob"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Knob" };
		ModComponentSolverInformation["NeedyCapacitor"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Capacitor" };
		ModComponentSolverInformation["Morse"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code" };
		ModComponentSolverInformation["Memory"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Memory" };
		ModComponentSolverInformation["Keypad"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keypad" };
		ModComponentSolverInformation["Maze"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Maze" };

		//Speakingevil
		ModComponentSolverInformation["runeMatchI"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Rune Match I", additionalNeedyTime = 15 };
		ModComponentSolverInformation["runeMatchII"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Rune Match II" };
		ModComponentSolverInformation["runeMatchIII"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Rune Match III" };

		//StrangaDanga
		ModComponentSolverInformation["keepClicking"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Keep Clicking" };
		ModComponentSolverInformation["sixteenCoins"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "16 Coins" };

		//TheDarkSid3r
		ModComponentSolverInformation["NotTimerModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Not Timer" };
		ModComponentSolverInformation["TDSAmogus"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "amogus" };
		ModComponentSolverInformation["TDSNya"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "nya~" };
		ModComponentSolverInformation["IconReveal"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Icon Reveal" };
		ModComponentSolverInformation["FreePassword"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Free Password" };
		ModComponentSolverInformation["LargeFreePassword"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Large Free Password" };
		ModComponentSolverInformation["LargeVanillaPassword"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Large Password" };
		ModComponentSolverInformation["TDSNeedyWires"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Needy Wires" };
		ModComponentSolverInformation["TDSDossierModifier"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Dossier Modifier" };
		ModComponentSolverInformation["ManualCodes"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Manual Codes" };
		ModComponentSolverInformation["jackboxServerModule"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Jackbox.TV", unclaimable = true };

		//Translated Modules
		ModComponentSolverInformation["BigButtonTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Big Button Translated" };
		ModComponentSolverInformation["MorseCodeTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Morse Code Translated" };
		ModComponentSolverInformation["PasswordsTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Password Translated" };
		ModComponentSolverInformation["WhosOnFirstTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Who's on First Translated" };
		ModComponentSolverInformation["VentGasTranslated"] = new ModuleInformation { builtIntoTwitchPlays = true, moduleDisplayName = "Vent Gas Translated" };

		//Shim added in between Twitch Plays and module (This allows overriding a specific command, adding a new command, or for fixes such as enforcing unsubmittable penalty)
		ModComponentSolverInformation["Color Generator"] = new ModuleInformation { moduleDisplayName = "Color Generator", helpText = "Submit a color using \"!{0} press bigred 1,smallred 2,biggreen 1,smallblue 1\" !{0} press <buttonname> <amount of times to push>. If you want to be silly, you can have this module change the color of the status light when solved with \"!{0} press smallblue UseRedOnSolve\" or UseOffOnSolve. You can make this module tell a story with !{0} tellmeastory, make a needy sound with !{0} needystart or !{0} needyend, fake strike with !{0} faksestrike, and troll with !{0} troll", helpTextOverride = true };
		ModComponentSolverInformation["ExtendedPassword"] = new ModuleInformation { moduleDisplayName = "Extended Password" };
		ModComponentSolverInformation["ColourFlashES"] = new ModuleInformation { moduleDisplayName = "Colour Flash ES", helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", helpTextOverride = true };
		ModComponentSolverInformation["PressX"] = new ModuleInformation { moduleDisplayName = "Press X", helpText = "Submit button presses using !{0} press x on 1 or !{0} press y on 23 or !{0} press a on 8 28 48. Acceptable buttons are a, b, x and y.", helpTextOverride = true };
		ModComponentSolverInformation["ShapesBombs"] = new ModuleInformation { moduleDisplayName = "Shapes And Bombs", helpText = "!{0} press A1 B39 C123... (column [A to E] and row [1 to 8] to press [you can input multiple rows in the same column]) | !{0} display/disp/d 4 (displays sequence number [0 to 14]) | !{0} reset/res/r (resets initial letter) | !{0} empty/emp/e (empties lit squares) | !{0} submit/sub/s (submits current shape) | !{0} colorblind/cb (enables colorblind mode)", helpTextOverride = true };
		ModComponentSolverInformation["taxReturns"] = new ModuleInformation { moduleDisplayName = "Tax Returns", helpText = "Submit your taxes using !{0} submit <number>. Page left and right using !{0} left (number) and !{0} right (number). Briefly view the HRMC terminal to see the deadline with !{0} deadline.", helpTextOverride = true, announceModule = true };

		//These modules have troll commands built in.
		ModComponentSolverInformation["MazeV2"] = new ModuleInformation { moduleDisplayName = "Plumbing" };

		//These modules are not built into TP, but they are created by notable people.

		//AAces
		ModComponentSolverInformation["timeKeeper"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };

		//AT_Bash / Bashly / Ashthebash
		ModComponentSolverInformation["ColourFlash"] = new ModuleInformation { helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5." };
		ModComponentSolverInformation["CruelPianoKeys"] = new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true };
		ModComponentSolverInformation["FestivePianoKeys"] = new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true };
		ModComponentSolverInformation["LightsOut"] = new ModuleInformation { helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right." };
		ModComponentSolverInformation["PianoKeys"] = new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true };
		ModComponentSolverInformation["Semaphore"] = new ModuleInformation { helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok." };

		//billy_bao
		ModComponentSolverInformation["greekCalculus"] = new ModuleInformation { CompatibilityMode = true };

		//Blananas2
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };

		//clutterArranger
		ModComponentSolverInformation["graphModule"] = new ModuleInformation { helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR." }; // Connection Check
		ModComponentSolverInformation["monsplodeFight"] = new ModuleInformation { helpText = "Use a move with !{0} use splash." };
		ModComponentSolverInformation["monsplodeWho"] = new ModuleInformation { helpText = "Press either button with “!{ 0 } press left / right | Left and Right can be abbreviated to(L) & (R)" };

		//EpicToast
		ModComponentSolverInformation["brushStrokes"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["cookieJars"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["krazyTalk"] = new ModuleInformation { CompatibilityMode = true };

		//Espik
		ModComponentSolverInformation["ForgetMeNow"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, CompatibilityMode = true };

		//eXish
		ModComponentSolverInformation["organizationModule"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["timingIsEverything"] = new ModuleInformation { announceModule = true };
		ModComponentSolverInformation["blinkstopModule"] = new ModuleInformation { statusLightPosition = StatusLightPosition.TopLeft };

		//Flamanis
		ModComponentSolverInformation["ChessModule"] = new ModuleInformation { helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", CompatibilityMode = true };
		ModComponentSolverInformation["Laundry"] = new ModuleInformation { helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa" };
		ModComponentSolverInformation["ModuleAgainstHumanity"] = new ModuleInformation { helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit." };

		//Goofy
		ModComponentSolverInformation["megaMan2"] = new ModuleInformation { CompatibilityMode = true };

		//Hexicube
		ModComponentSolverInformation["MemoryV2"] = new ModuleInformation { moduleDisplayName = "Forget Me Not", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["KeypadV2"] = new ModuleInformation { moduleDisplayName = "Round Keypad" };
		ModComponentSolverInformation["ButtonV2"] = new ModuleInformation { moduleDisplayName = "Square Button" };
		ModComponentSolverInformation["SimonV2"] = new ModuleInformation { moduleDisplayName = "Simon States" };
		ModComponentSolverInformation["PasswordV2"] = new ModuleInformation { moduleDisplayName = "Safety Safe" };
		ModComponentSolverInformation["MorseV2"] = new ModuleInformation { moduleDisplayName = "Morsematics" };
		ModComponentSolverInformation["HexiEvilFMN"] = new ModuleInformation { moduleDisplayName = "Forget Everything", CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["NeedyVentV2"] = new ModuleInformation { moduleDisplayName = "Needy Answering Questions" };
		ModComponentSolverInformation["NeedyKnobV2"] = new ModuleInformation { moduleDisplayName = "Needy Rotary Phone" };

		//JerryErris
		ModComponentSolverInformation["desertBus"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["footnotes"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["forgetThis"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };

		//KingBranBran
		ModComponentSolverInformation["intervals"] = new ModuleInformation { CompatibilityMode = true };
		//Kritzy
		ModComponentSolverInformation["KritMicroModules"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["KritRadio"] = new ModuleInformation { CompatibilityMode = true };

		//Maca
		ModComponentSolverInformation["Playfair"] = new ModuleInformation { moduleDisplayName = "Playfair Cipher" };

		//McNiko67
		ModComponentSolverInformation["BigSwitch"] = new ModuleInformation { CompatibilityMode = true };

		//MrMelon
		ModComponentSolverInformation["colourcode"] = new ModuleInformation { CompatibilityMode = true };

		//MrSpekCraft
		ModComponentSolverInformation["vexillology"] = new ModuleInformation { CompatibilityMode = true };

		//NoahCoolBoy
		ModComponentSolverInformation["pigpenRotations"] = new ModuleInformation { helpTextOverride = true, helpText = "To submit abcdefhijklm use '!{0} abcdefhijklm'." };

		//Piggered
		ModComponentSolverInformation["NonogramModule"] = new ModuleInformation { CompatibilityMode = true };

		//Procyon
		ModComponentSolverInformation["alphaBits"] = new ModuleInformation { CompatibilityMode = true };

		//Qkrisi
		ModComponentSolverInformation["qkForgetPerspective"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };

		//Red Penguin
		ModComponentSolverInformation["encryptionBingo"] = new ModuleInformation { announceModule = true, CameraPinningAlwaysAllowed = true };

		//Royal_Flu$h
		ModComponentSolverInformation["christmasPresents"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["europeanTravel"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["maintenance"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["modulo"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["numberCipher"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["retirement"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["theSwan"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["wire"] = new ModuleInformation { CompatibilityMode = true };

		//Sean Obach
		ModComponentSolverInformation["forgetEnigma"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };

		//Spare Wizard
		ModComponentSolverInformation["spwiz3DMaze"] = new ModuleInformation { helpTextOverride = true, helpText = "!{0} move L F R F U [move] | !{0} walk L F R F U [walk slower] [L = left, R = right, F = forward, U = u-turn]" };
		ModComponentSolverInformation["spwizAdventureGame"] = new ModuleInformation { helpTextOverride = true, helpText = "Cycle the stats with !{0} cycle stats. Cycle the Weapons/Items with !{0} cycle items. Cycle everything with !{0} cycle all. Use weapons/Items with !{0} use potion. Use multiple items with !{0} use ticket, crystal ball, caber. (spell out the item name completely. not case sensitive)" };

		//Speakingevil
		ModComponentSolverInformation["crypticCycle"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["forgetMeLater"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["tallorderedKeys"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["veryAnnoyingButton"] = new ModuleInformation { announceModule = true };

		//TheThirdMan
		ModComponentSolverInformation["forgetThemAll"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true };
		ModComponentSolverInformation["treasureHunt"] = new ModuleInformation { CompatibilityMode = true };

		//Timwi (includes Perky/Konqi/Eluminate/Mitterdoo/Riverbui modules maintained by Timwi)
		ModComponentSolverInformation["alphabet"] = new ModuleInformation { moduleDisplayName = "Alphabet" };
		ModComponentSolverInformation["CornersModule"] = new ModuleInformation { statusLightPosition = StatusLightPosition.Center };
		ModComponentSolverInformation["DividedSquaresModule"] = new ModuleInformation { announceModule = true };
		ModComponentSolverInformation["HogwartsModule"] = new ModuleInformation { announceModule = true };
		ModComponentSolverInformation["NumberPad"] = new ModuleInformation { moduleDisplayName = "Number Pad" };
		ModComponentSolverInformation["SouvenirModule"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, unclaimable = true };

		//Trainzack
		ModComponentSolverInformation["MusicRhythms"] = new ModuleInformation { helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", CompatibilityMode = true };

		//Virepri
		ModComponentSolverInformation["BitOps"] = new ModuleInformation { helpText = "Submit the correct answer with !{0} submit 10101010.", validCommands = new[] { "^submit [0-1]{8}$" } };
		ModComponentSolverInformation["LEDEnc"] = new ModuleInformation { helpText = "Press the button with label B with !{0} press b." };

		//Windesign
		ModComponentSolverInformation["Color Decoding"] = new ModuleInformation { moduleDisplayName = "Color Decoding" };
		ModComponentSolverInformation["GridMatching"] = new ModuleInformation { helpText = "Commands are “left/right/up/down/clockwise/counter-clockwise/submit” or “l/r/u/d/cw/ccw/s”. The letter can be set by using “set d” or “'d'”. All of these can be chained, for example: “!{0} up right right clockwise 'd' submit”. You can only use one letter-setting command at a time." };

		//ZekNikZ
		ModComponentSolverInformation["booleanVennModule"] = new ModuleInformation { helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none)." };
		ModComponentSolverInformation["complicatedButtonsModule"] = new ModuleInformation { helpText = "Press the top button with !{0} press top (also t, 1, etc.)." };
		ModComponentSolverInformation["symbolicPasswordModule"] = new ModuleInformation { helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!" };

		//Other modded modules not built into Twitch Plays
		ModComponentSolverInformation["buttonMasherNeedy"] = new ModuleInformation { moduleDisplayName = "Needy Button Masher", helpText = "Press the button 20 times with !{0} press 20" };
		ModComponentSolverInformation["combinationLock"] = new ModuleInformation { helpText = "Submit the code using !{0} submit 1 2 3.", CompatibilityMode = true };
		ModComponentSolverInformation["EternitySDec"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["forgetUsNot"] = new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, CompatibilityMode = true };
		ModComponentSolverInformation["groceryStore"] = new ModuleInformation { helpText = "Use !{0} add item to cart | Adds an item to the cart. Use !{0} pay and leave | Pays and leaves | Commands can be abbreviated with !{0} add & !{0} pay" };
		ModComponentSolverInformation["needyPiano"] = new ModuleInformation { CompatibilityMode = true };
		ModComponentSolverInformation["mysterymodule"] = new ModuleInformation { CompatibilityMode = true, CameraPinningAlwaysAllowed = true, announceModule = true, unclaimable = true };

		foreach (KeyValuePair<string, ModuleInformation> kvp in ModComponentSolverInformation)
		{
			ModComponentSolverInformation[kvp.Key].moduleID = kvp.Key;
			AddDefaultModuleInformation(kvp.Value);
		}
	}

	public static Dictionary<string, string> rewardBonuses = new Dictionary<string, string>();

	public static IEnumerator LoadDefaultInformation(bool reloadData = false)
	{
		var sheet = new GoogleSheet("1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA");

		yield return sheet;

		if (sheet.Success)
		{
			var displayNames = new List<string>();
			foreach (var entry in sheet.GetRows())
			{
				string scoreString = entry["tpscore"];
				if (string.IsNullOrEmpty(scoreString))
					continue;

				string moduleName = entry["modulename"];
				if (string.IsNullOrEmpty(moduleName) || moduleName == "NEEDIES")
					continue;

				string rewardString = entry["bombreward"];
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

		// Announceable modules
		yield return Repository.LoadData();
		foreach (var item in Repository.Modules)
		{
			var defaultInfo = GetDefaultInformation(item.ModuleID);
			defaultInfo.announceModule |= item.ModuleID.IsBossMod();
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
				CompatibilityMode = info.CompatibilityMode,
				helpText = info.helpText,
				helpTextOverride = false,
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

	private static void AddDefaultModuleInformation(string moduleType, string moduleDisplayName, string helpText, string[] regexList)
	{
		if (string.IsNullOrEmpty(moduleType)) return;
		AddDefaultModuleInformation(GetModuleInfo(moduleType));
		ModuleInformation info = DefaultModComponentSolverInformation[moduleType];
		info.moduleDisplayName = moduleDisplayName;
		if (!string.IsNullOrEmpty(helpText)) info.helpText = helpText;
		info.validCommands = regexList;
	}

	public static ModuleInformation GetDefaultInformation(string moduleType, bool addIfNotExist = true)
	{
		if (!DefaultModComponentSolverInformation.ContainsKey(moduleType) && addIfNotExist)
			AddDefaultModuleInformation(new ModuleInformation { moduleID = moduleType });
		else if (!DefaultModComponentSolverInformation.ContainsKey(moduleType))
			return null;
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

		info.announceModule |= defInfo.announceModule;

		if (writeData && !info.builtIntoTwitchPlays)
			ModuleData.WriteDataToFile();

		return ModComponentSolverInformation[moduleType];
	}

	public static ModuleInformation GetModuleInfo(string moduleType, string helpText)
	{
		ModuleInformation info = GetModuleInfo(moduleType, false);
		ModuleInformation defInfo = GetDefaultInformation(moduleType);

		if (!info.helpTextOverride)
		{
			ModuleData.DataHasChanged |= !info.helpText.TryEquals(helpText);
			info.helpText = helpText;
		}

		defInfo.helpText = helpText;

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
				i.CompatibilityMode |= info.CompatibilityMode;
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

		AddDefaultModuleInformation(moduleType, displayName, help, regexList);

		if (commandComponentType == null) return null;
		ComponentSolverFields componentSolverFields = new ComponentSolverFields
		{
			CommandComponent = module.BombComponent.GetComponentInChildren(commandComponentType),
			Method = method,
			ForcedSolveMethod = forcedSolved,
			ModuleInformation = info,

			HelpMessageField = FindHelpMessage(commandComponentType),
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
