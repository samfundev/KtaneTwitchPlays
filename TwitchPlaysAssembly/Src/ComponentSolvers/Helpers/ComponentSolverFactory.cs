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

	private static readonly Dictionary<string, Type> ModComponentSolverTypes = new Dictionary<string, Type>();
	private static readonly Dictionary<string, ModuleInformation> ModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private static readonly Dictionary<string, ModuleInformation> DefaultModComponentSolverInformation = new Dictionary<string, ModuleInformation>();
	private const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

	static ComponentSolverFactory()
	{
		var attributes = ReflectionHelper.GetTypesWithAttribute<ModuleIDAttribute>(typeof(ModuleIDAttribute));
		ModComponentSolverTypes = attributes.ToDictionary(pair => pair.Value.ModuleID, pair => pair.Key);

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

		ModComponentSolverInformation = new Dictionary<string, ModuleInformation>()
		{

		//All of these modules are built into Twitch plays.

			// AT_Bash / Bashly / Ashthebash
			{ "AppreciateArt", new ModuleInformation { unclaimable = true } },

			// Perky
			{ "TurnTheKey", new ModuleInformation { announceModule = true } },
			{ "TurnTheKeyAdvanced", new ModuleInformation { announceModule = true } },

			// MaddyMoos
			{ "gemory", new ModuleInformation { announceModule = true } },

			// Mock Army
			{ "Filibuster", new ModuleInformation { unclaimable = true } },

			// Royal_Flu$h
			{ "simonsStages", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "troll", new ModuleInformation { announceModule = true } },

			// Misc
			{ "rng", new ModuleInformation { additionalNeedyTime = 30 } },
			{ "forget_fractal", new ModuleInformation { announceModule = true } },
			{ "redLightGreenLight", new ModuleInformation { announceModule = true, unclaimable = true } },
			{ "threeSentenceHorror", new ModuleInformation { announceModule = true, unclaimable = true } },

			// Speakingevil
			{ "runeMatchI", new ModuleInformation { additionalNeedyTime = 15 } },

			// TheDarkSid3r
			{ "jackboxServerModule", new ModuleInformation { unclaimable = true } },

			// Shim added in between Twitch Plays and module (This allows overriding a specific command, adding a new command, or for fixes such as enforcing unsubmittable penalty)
			{ "Color Generator", new ModuleInformation { helpText = "Submit a color using \"!{0} press bigred 1,smallred 2,biggreen 1,smallblue 1\" !{0} press <buttonname> <amount of times to push>. If you want to be silly, you can have this module change the color of the status light when solved with \"!{0} press smallblue UseRedOnSolve\" or UseOffOnSolve. You can make this module tell a story with !{0} tellmeastory, make a needy sound with !{0} needystart or !{0} needyend, fake strike with !{0} faksestrike, and troll with !{0} troll", helpTextOverride = true } },
			{ "ColourFlashES", new ModuleInformation { helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.", helpTextOverride = true } },
			{ "PressX", new ModuleInformation { helpText = "Submit button presses using !{0} press x on 1 or !{0} press y on 23 or !{0} press a on 8 28 48. Acceptable buttons are a, b, x and y.", helpTextOverride = true } },
			{ "ShapesBombs", new ModuleInformation { helpText = "!{0} press A1 B39 C123... (column [A to E] and row [1 to 8] to press [you can input multiple rows in the same column]) | !{0} display/disp/d 4 (displays sequence number [0 to 14]) | !{0} reset/res/r (resets initial letter) | !{0} empty/emp/e (empties lit squares) | !{0} submit/sub/s (submits current shape) | !{0} colorblind/cb (enables colorblind mode)", helpTextOverride = true } },
			{ "taxReturns", new ModuleInformation { helpText = "Submit your taxes using !{0} submit <number>. Page left and right using !{0} left (number) and !{0} right (number). Briefly view the HRMC terminal to see the deadline with !{0} deadline.", helpTextOverride = true, announceModule = true } },

			// These modules are not built into TP, but they are created by notable people.

			// AAces
			{ "timeKeeper", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },

			// AnAverageArceus
			{ "dontTouchAnything", new ModuleInformation { unclaimable = true } },

			// AT_Bash / Bashly / Ashthebash
			{ "ColourFlash", new ModuleInformation { helpText = "Submit the correct response with !{0} press yes 3, or !{0} press no 5." } },
			{ "CruelPianoKeys", new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true } },
			{ "FestivePianoKeys", new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true } },
			{ "LightsOut", new ModuleInformation { helpText = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right." } },
			{ "PianoKeys", new ModuleInformation { helpText = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.", CompatibilityMode = true } },
			{ "Semaphore", new ModuleInformation { helpText = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left. Submit with !{0} press ok." } },

			// billy_bao
			{ "greekCalculus", new ModuleInformation { CompatibilityMode = true } },

			// Blananas2
			{ "timingIsEverything", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "triskaideka", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft } },

			// clutterArranger
			{ "graphModule", new ModuleInformation { helpText = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR." } }, // Connection Check
			{ "monsplodeFight", new ModuleInformation { helpText = "Use a move with !{0} use splash." } },
			{ "monsplodeWho", new ModuleInformation { helpText = "Press either button with “!{ 0 } press left / right | Left and Right can be abbreviated to(L) & (R)" } },

			// Deaf
			{ "WAR", new ModuleInformation { unclaimable = true } },
			{ "BoardWalk", new ModuleInformation { announceModule = true, autosolvePriority = 10 } },

			// EpicToast
			{ "brushStrokes", new ModuleInformation { CompatibilityMode = true } },
			{ "cookieJars", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 10 } },
			{ "krazyTalk", new ModuleInformation { CompatibilityMode = true } },

			// Espik
			{ "ForgetMeNow", new ModuleInformation { CameraPinningAlwaysAllowed = true, CompatibilityMode = true } },

			// eXish
			{ "organizationModule", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 40 } },
			{ "blinkstopModule", new ModuleInformation { statusLightPosition = StatusLightPosition.TopLeft } },
			{ "widgetry", new ModuleInformation { announceModule = true } },

			// Flamanis
			{ "ChessModule", new ModuleInformation { helpText = "Cycle the positions with !{0} cycle. Submit the safe spot with !{0} press C2.", CompatibilityMode = true } },
			{ "Laundry", new ModuleInformation { helpText = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning. Set just washing with !{0} set wash 40C. Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa" } },
			{ "ModuleAgainstHumanity", new ModuleInformation { helpText = "Reset the module with !{0} press reset. Move the black card +2 with !{0} move black 2. Move the white card -3 with !{0} move white -3. Submit with !{0} press submit." } },

			// GeekYiwen
			{ "encryptedHangman", new ModuleInformation { announceModule = true, autosolvePriority = 10 } },

			// GhostSalt
			{ "GSAccessCodes", new ModuleInformation { announceModule = true, autosolvePriority = 50 } },
			{ "GSYellowFace", new ModuleInformation { unclaimable = true } },

			// Goofy
			{ "megaMan2", new ModuleInformation { CompatibilityMode = true } },

			// Hexicube
			{ "MemoryV2", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "HexiEvilFMN", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },

			// JerryErris
			{ "desertBus", new ModuleInformation { CompatibilityMode = true } },
			{ "footnotes", new ModuleInformation { CompatibilityMode = true } },
			{ "forgetThis", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },

			// JyGein
			{ "wireTesting", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomRight } },

			// Katarina
			{ "kataZenerCards", new ModuleInformation { announceModule = true } },

			// KingBranBran
			{ "intervals", new ModuleInformation { CompatibilityMode = true } },

			// Kritzy
			{ "KritMicroModules", new ModuleInformation { CompatibilityMode = true } },
			{ "KritRadio", new ModuleInformation { CompatibilityMode = true } },

			// ktane1
			{ "schulteTable", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomRight } },
			{ "cruelSchulteTable", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomRight } },

			// MaddyMoos
			{ "top10nums", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomRight } },

			// MAXANGE2B
			{ "colorPong", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft } },

			// McNiko67
			{ "BigSwitch", new ModuleInformation { CompatibilityMode = true } },

			// MrMelon
			{ "colourcode", new ModuleInformation { CompatibilityMode = true } },

			// MrSpekCraft
			{ "vexillology", new ModuleInformation { CompatibilityMode = true } },

			// Nimsay Ramsey
			{ "solveShift", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft, announceModule = true, autosolvePriority = 10 } },

			// NoahCoolBoy
			{ "pigpenRotations", new ModuleInformation { helpTextOverride = true, helpText = "To submit abcdefhijklm use '!{0} abcdefhijklm'." } },

			// Obvious
			{ "hearthur", new ModuleInformation { unclaimable = true } },

			// Piggered
			{ "NonogramModule", new ModuleInformation { CompatibilityMode = true } },
			{ "bigBean", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft } },

			// Play
			{ "dandysFloors", new ModuleInformation { announceModule = true, CameraPinningAlwaysAllowed = true, autosolvePriority = 10 } },

			// Procyon
			{ "alphaBits", new ModuleInformation { CompatibilityMode = true } },

			// Qkrisi
			{ "qkForgetPerspective", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },

			// Red Penguin
			{ "encryptionBingo", new ModuleInformation { announceModule = true, CameraPinningAlwaysAllowed = true } },
			{ "needyPiano", new ModuleInformation { CompatibilityMode = true } },

			// Royal_Flu$h
			{ "christmasPresents", new ModuleInformation { CompatibilityMode = true } },
			{ "europeanTravel", new ModuleInformation { CompatibilityMode = true } },
			{ "maintenance", new ModuleInformation { CompatibilityMode = true } },
			{ "modulo", new ModuleInformation { CompatibilityMode = true } },
			{ "numberCipher", new ModuleInformation { CompatibilityMode = true } },
			{ "retirement", new ModuleInformation { CompatibilityMode = true } },
			{ "theSwan", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "wire", new ModuleInformation { CompatibilityMode = true } },

			// Sean Obach
			{ "forgetEnigma", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },

			// Spare Wizard
			{ "spwiz3DMaze", new ModuleInformation { helpTextOverride = true, helpText = "!{0} move L F R F U [move] | !{0} walk L F R F U [walk slower] [L = left, R = right, F = forward, U = u-turn]" } },
			{ "spwizAdventureGame", new ModuleInformation { helpTextOverride = true, helpText = "Cycle the stats with !{0} cycle stats. Cycle the Weapons/Items with !{0} cycle items. Cycle everything with !{0} cycle all. Use weapons/Items with !{0} use potion. Use multiple items with !{0} use ticket, crystal ball, caber. (spell out the item name completely. not case sensitive)" } },

			// Speakingevil
			{ "crypticCycle", new ModuleInformation { CompatibilityMode = true } },
			{ "forgetMeLater", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 30 } },
			{ "tallorderedKeys", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "veryAnnoyingButton", new ModuleInformation { announceModule = true, CameraPinningAlwaysAllowed = true } },
			{ "doomsdayButton", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft, announceModule = true } },

			// That one kid
			{ "peeky", new ModuleInformation { announceModule = true } },

			// TheThirdMan
			{ "forgetThemAll", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true } },
			{ "treasureHunt", new ModuleInformation { CompatibilityMode = true } },
			{ "oldFogey", new ModuleInformation { statusLightPosition = StatusLightPosition.BottomLeft } },

			// Timwi (includes Perky/Konqi/Eluminate/Mitterdoo/Riverbui modules maintained by Timwi)
			{ "CornersModule", new ModuleInformation { statusLightPosition = StatusLightPosition.Center } },
			{ "DividedSquaresModule", new ModuleInformation { announceModule = true } },
			{ "HogwartsModule", new ModuleInformation { announceModule = true } },
			{ "SouvenirModule", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, unclaimable = true } },

			// Trainzack
			{ "MusicRhythms", new ModuleInformation { helpText = "Press a button using !{0} press 1. Hold a button for a certain duration using !{0} hold 1 for 2. Mash all the buttons using !{0} mash. Buttons can be specified using the text on the button, a number in reading order or using letters like tl.", CompatibilityMode = true } },

			// VFlyer (includes a module maintained by NickLatkovich)
			{ "tetrahedron", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 30 } },
			{ "pointerPointerModule", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 30 } },
			{ "clearanceCodeModule", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 30 } },

			// Virepri
			{ "BitOps", new ModuleInformation { helpText = "Submit the correct answer with !{0} submit 10101010.", validCommands = new[] { "^submit [0-1]{8}$" } } },
			{ "LEDEnc", new ModuleInformation { helpText = "Press the button with label B with !{0} press b." } },

			// Windesign
			{ "GridMatching", new ModuleInformation { helpText = "Commands are “left/right/up/down/clockwise/counter-clockwise/submit” or “l/r/u/d/cw/ccw/s”. The letter can be set by using “set d” or “'d'”. All of these can be chained, for example: “!{0} up right right clockwise 'd' submit”. You can only use one letter-setting command at a time." } },

			// ZekNikZ
			{ "booleanVennModule", new ModuleInformation { helpText = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none)." } },
			{ "complicatedButtonsModule", new ModuleInformation { helpText = "Press the top button with !{0} press top (also t, 1, etc.)." } },
			{ "symbolicPasswordModule", new ModuleInformation { helpText = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!" } },

			// Other modded modules not built into Twitch Plays
			{ "buttonMasherNeedy", new ModuleInformation { helpText = "Press the button 20 times with !{0} press 20" } },
			{ "combinationLock", new ModuleInformation { helpText = "Submit the code using !{0} submit 1 2 3.", CompatibilityMode = true } },
			{ "EternitySDec", new ModuleInformation { CompatibilityMode = true } },
			{ "forgetUsNot", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, CompatibilityMode = true } },
			{ "groceryStore", new ModuleInformation { helpText = "Use !{0} add item to cart | Adds an item to the cart. Use !{0} pay and leave | Pays and leaves | Commands can be abbreviated with !{0} add & !{0} pay" } },
			{ "mysterymodule", new ModuleInformation { CompatibilityMode = true, CameraPinningAlwaysAllowed = true, announceModule = true, unclaimable = true } },
			{ "theFan", new ModuleInformation { CameraPinningAlwaysAllowed = true, announceModule = true, autosolvePriority = 30 } },
			{ "Kuro", new ModuleInformation { autosolvePriority = 50 } },
			{ "SecurityCouncil", new ModuleInformation { autosolvePriority = 50 } },
		};

		foreach (KeyValuePair<string, ModuleInformation> kvp in ModComponentSolverInformation)
		{
			ModComponentSolverInformation[kvp.Key].moduleID = kvp.Key;
			AddDefaultModuleInformation(kvp.Value);
		}
	}

	public static Dictionary<string, string> rewardBonuses = new Dictionary<string, string>();

	public static IEnumerator LoadDefaultInformation(bool reloadData = false)
	{
		var sheet = new GoogleSheet(TwitchPlaySettings.data.ScoringSheetId);

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

				// Catch any TBD modules which can't be parsed.
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
			defaultInfo.announceModule |= item.ModuleID.ModHasQuirk("NeedsImmediateAttention");
			defaultInfo.announceModule |= item.ModuleID.ModHasQuirk("PseudoNeedy");
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
	private static ComponentSolver CreateModComponentSolver(TwitchModule module, string moduleType, string displayName) => ModComponentSolverTypes.TryGetValue(moduleType, out Type solverType)
			? Activator.CreateInstance(solverType, new[] { module }) as ComponentSolver
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
		FieldInfo helpField = commandComponentType?.GetDeepField("TwitchHelpMessage", fieldFlags);
		return helpField?.FieldType == typeof(string) ? helpField : null;
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

	public static bool IsBuiltIn(string moduleID)
	{
		ModComponentSolverTypes.TryGetValue(moduleID, out Type type);
		return type != null && !type.IsSubclassOf(typeof(ComponentSolverShim));
	}
}
