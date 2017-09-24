using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ComponentSolverFactory
{
    private delegate ComponentSolver ModComponentSolverDelegate(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller);
    private static readonly Dictionary<string, ModComponentSolverDelegate> ModComponentSolverCreators;
    private static readonly Dictionary<string, string> ModComponentSolverHelpMessages;
    private static readonly Dictionary<string, string> ModComponentSolverManualCodes;
    private static readonly Dictionary<string, bool> ModComponentSolverStatusLightLeft;
    private static readonly Dictionary<string, bool> ModComponentSolverStatusLightBottom;
    private static readonly Dictionary<string, float> ModComponentSolverChatRotation;

    private enum ModCommandType
    {
        Simple,
        Coroutine
    }

    static ComponentSolverFactory()
    {
        ModComponentSolverCreators = new Dictionary<string, ModComponentSolverDelegate>();
        ModComponentSolverHelpMessages = new Dictionary<string, string>();
        ModComponentSolverManualCodes = new Dictionary<string, string>();
        ModComponentSolverStatusLightLeft = new Dictionary<string, bool>();
        ModComponentSolverStatusLightBottom = new Dictionary<string, bool>();
        ModComponentSolverChatRotation = new Dictionary<string, float>();

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
		ModComponentSolverCreators["NumberPad"] = (bombCommander, bombComponent, ircConnection, canceller) => new NumberPadComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		ModComponentSolverCreators["switchModule"] = (bombCommander, bombComponent, ircConnection, canceller) => new SwitchesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		ModComponentSolverCreators["resistors"] = (bombCommander, bombComponent, ircConnection, canceller) => new ResistorsComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		ModComponentSolverCreators["Microcontroller"] = (bombCommander, bombComponent, ircConnection, canceller) => new MicrocontrollerComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		ModComponentSolverCreators["ChordQualities"] = (bombCommander, bombComponent, ircConnection, canceller) => new ChordQualitiesComponentSolver(bombCommander, bombComponent, ircConnection, canceller);
		
		//Help Messages
		//ModComponentSolverHelpMessages["spwiz3DMaze"] = "";
		ModComponentSolverHelpMessages["AdjacentLettersModule"] = "Set the Letters with !{0} set W D J S.  (warning, this will unset ALL letters not specified.)  Submit your answer with !{0} submit.";
        ModComponentSolverHelpMessages["spwizAdventureGame"] = "Cycle the stats with !{0} cycle stats.  Cycle the Weapons/Items with !{0} cycle items. Use weapons/Items with !{0} use potion. (spell out the item name completely. not case sensitive)";
        //ModComponentSolverHelpMessages["alphabet"] = "";
        //ModComponentSolverHelpMessages["AnagramsModule"] = "";
        ModComponentSolverHelpMessages["spwizAstrology"] = "Press good on 3 with !{0} press good on 3.  Press bad on 2 with !{0} press bad on 2. No Omen is !{0} press no";
        ModComponentSolverHelpMessages["BattleshipModule"] = "Scan the safe spots with !{0} scan A2 B3 E5. Mark the spots as water with !{0} miss A1 A3 B4.  Mark the spots as ships with !{0} hit E3 E4. Fill in the rows with !{0} row 3 4. Fill in columns with !{0} col B D";
        ModComponentSolverHelpMessages["BitmapsModule"] = "Submit the correct answer with !{0} press 2.";
        ModComponentSolverHelpMessages["BitOps"] = "Submit the correct answer with !{0} submit 10101010.";
        ModComponentSolverHelpMessages["BlindAlleyModule"] = "Hit the correct spots with !{0} press bl mm tm tl.  (Locations are tl, tm, ml, mm, mr, bl, bm, br)";
        ModComponentSolverHelpMessages["booleanVennModule"] = "Select parts of the diagram with !{0} a bc abc. Options are A, AB, ABC, AC, B, BC, C, O (none).";
        ModComponentSolverHelpMessages["BrokenButtonsModule"] = "Press the button by name with !{0} press \"this\".  Press the button in column 2 row 3 with !{0} press 2 3. Press the right submit button with !{0} submit right.";
        ModComponentSolverHelpMessages["CaesarCipherModule"] = "Press the correct cipher text with !{0} press K B Q I S.";
        ModComponentSolverHelpMessages["CheapCheckoutModule"] = "Cycle the items with !{0} items. Get customers to pay the correct amount with !{0} submit.  Return the proper change with !{0} submit 3.24.";
        ModComponentSolverHelpMessages["ChessModule"] = "Cycle the positions with !{0} cycle.  Submit the safe spot with !{0} press C2.";
        //ModComponentSolverHelpMessages["ChordQualities"] = "";
        ModComponentSolverHelpMessages["colormath"] = "Set the correct number with !{0} set a,k,m,y.  Submit your set answer with !{0} submit. colors are Red, Orange, Yellow, Green, Blue, Purple, Magenta, White, grAy, blackK. (note what letter is capitalized in each color.)";
        ModComponentSolverHelpMessages["ColoredSquaresModule"] = "Press the desired squares with !{0} red, !{0} green, !{0} blue, !{0} yellow, !{0} magenta, !{0} row, or !{0} col.";
        ModComponentSolverHelpMessages["ColoredSwitchesModule"] = "Flip the first switch with !{0} toggle 1.  Flip multiple switches with !{0} toggle 4 3 2 5.";
        ModComponentSolverHelpMessages["ColourFlash"] = "Submit the correct response with !{0} press yes 3, or !{0} press no 5.";
        //ModComponentSolverHelpMessages["combinationLock"] = "";
        ModComponentSolverHelpMessages["complicatedButtonsModule"] = "Press the top button with !{0} press top (also t, 1, etc.).";
        ModComponentSolverHelpMessages["CruelPianoKeys"] = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.";
        ModComponentSolverHelpMessages["graphModule"] = "Submit an answer with !{0} submit green red true false. Order is TL, TR, BL, BR."; // Connection Check
        ModComponentSolverHelpMessages["CoordinatesModule"] = "Cycle the options with !{0} cycle.  Submit your answer with !{0} submit <3,2>.  Partial answers are acceptable. To do chinese numbers, its !{0} submit chinese 12.";
        ModComponentSolverHelpMessages["CreationModule"] = "Combine two elements with !{0} combine water fire.";
        ModComponentSolverHelpMessages["DoubleOhModule"] = "Cycle the buttons with !{0} cycle. (Cycle presses each button 3 times, in the order of vert1, horiz1, horiz2, vert2, submit.)  Submit your answer with !{0} press vert1 horiz1 horiz2 vert2 submit.";
        //ModComponentSolverHelpMessages["EdgeworkModule"] = "";
        //ModComponentSolverHelpMessages["Emoji Math"] = "";
        //ModComponentSolverHelpMessages["EnglishTest"] = "";
        ModComponentSolverHelpMessages["fastMath"] = "Start the timer with !{0} go. Submit an answer with !{0} submit 12.";
        //ModComponentSolverHelpMessages["Filibuster"] = "";
        ModComponentSolverHelpMessages["fizzBuzzModule"] = "Press the top button with !{0} press top (also t, 1, etc.). Submit with !{0} press submit.";
        ModComponentSolverHelpMessages["FollowTheLeaderModule"] = "Cut the wires in the order specified with !{0} cut 12 10 8 7 6 5 3 1. (note that order was the Lit CLR rule.)";
        ModComponentSolverHelpMessages["FriendshipModule"] = "Submit the desired friendship element with !{0} submit Fairness Conscientiousness Kindness Authenticity.";
        ModComponentSolverHelpMessages["GridlockModule"] = "Go to next page with !{0} press next, submit answer of D3 with !{0} press D3, reset to start with !{0} reset.";
        ModComponentSolverHelpMessages["HexamazeModule"] = "Move towards the exit with !{0} move 12 10 6 6 6 2, or with !{0} move N NW S S S NE.  (clockface or cardinal)";
        ModComponentSolverHelpMessages["http"] = "Submit the response with !{0} resp 123.";
        ModComponentSolverHelpMessages["iceCreamModule"] = "Move left/right with !{0} left and !{0} right. Cycle through all options with !{0} cycle. Sell with !{0} sell.";
        ModComponentSolverHelpMessages["Laundry"] = "Set all of the options with !{0} set all 30C,2 dot,110C,Wet Cleaning.  Set just washing with !{0} set wash 40C.  Submit with !{0} insert coin. ...pray for that 4 in 2 & lit BOB Kappa";
        ModComponentSolverHelpMessages["LEDEnc"] = "Press the button with label B with !{0} press b.";
        //ModComponentSolverHelpMessages["LetterKeys"] = "";
        ModComponentSolverHelpMessages["LightCycleModule"] = "Submit your answer with !{0} B R W M G Y. (note, this module WILL try to input any answer you put into it. Don't do !{0} claim or !{0} mine here.)";
        ModComponentSolverHelpMessages["LightsOut"] = "Press the buttons with !{0} press 1 2 3. Buttons ordered from top to bottom, then left to right.";
        ModComponentSolverHelpMessages["Logic"] = "Logic is answered with !{0} submit F T.";
        //ModComponentSolverHelpMessages["MazeV2"] = "";
        //ModComponentSolverHelpMessages["Microcontroller"] = "";
        ModComponentSolverHelpMessages["MinesweeperModule"] = "Clear the initial colour with !{0} dig blue. Clear the square on column 1 row 2 with !{0} dig 1 2. Flag the square on column 3 row 4 with !{0} flag 3 4. Separate multiple squares with a semicolon to interact with all of them.";
        ModComponentSolverHelpMessages["ModuleAgainstHumanity"] = "Reset the module with !{0} press reset.  Move the black card +2 with !{0} move black 2.  Move the white card -3 with !{0} move white -3. Submit with !{0} press submit.";
        ModComponentSolverHelpMessages["monsplodeFight"] = "Use a move with !{0} use explode.";
        //ModComponentSolverHelpMessages["monsplodeWho"] = "";
        //ModComponentSolverHelpMessages["MorseV2"] = "";
        ModComponentSolverHelpMessages["MouseInTheMaze"] = "Move with !{0} forward back. Turn with !{0} left right u-turn. The first letter only can be used instead. Submit with !{0} submit.";
        //ModComponentSolverHelpMessages["murder"] = "";
        ModComponentSolverHelpMessages["MusicRhythms"] = "Tap buttons with !{0} tap 1,2. Hold down the first button for three beeps with !{0} hold 1 3. Buttons are numbered in reading order.";
        ModComponentSolverHelpMessages["MysticSquareModule"] = "Move the numbers around with !{0} press 1 3 2 1 3 4 6 8.";
        //ModComponentSolverHelpMessages["Needy Math"] = "";
        ModComponentSolverHelpMessages["neutralization"] = "Select a base with !{0} base NaOH. Turn the filter on/off with !{0} filter. Set drop count with !{0} conc set 48. Submit with !{0} titrate.";
        //ModComponentSolverHelpMessages["NumberPad"] = "";
        ModComponentSolverHelpMessages["OnlyConnectModule"] = "Press a button by position with !{0} press tm or !{0} press 2. Round 1 also accepts symbol names (e.g. reeds, eye, flax, lion, water, viper).";
        //ModComponentSolverHelpMessages["spwizPerspectivePegs"] = "";
        ModComponentSolverHelpMessages["PianoKeys"] = "Submit your answer with !{0} press Bb Bb Bb Bb Gb Ab Bb Ab Bb.";
        //ModComponentSolverHelpMessages["resistors"] = "";
        ModComponentSolverHelpMessages["RockPaperScissorsLizardSpockModule"] = "Submit your answer with !{0} press scissors lizard.";
        ModComponentSolverHelpMessages["RubiksCubeModule"] = "View the colors on all sides with !{0} rotate. Reset the cube to starting state with !{0} reset. Solve the Cube with !{0} r' d u f' r' d' u b' u' f";
        ModComponentSolverHelpMessages["screw"] = "Screw with !{0} screw tr or !{0} screw 3. Options are TL, TM, TR, BL, BM, BR. Press a button with !{0} press b or !{0} press 2.";
        //ModComponentSolverHelpMessages["SeaShells"] = "";
        ModComponentSolverHelpMessages["Semaphore"] = "Move to the next flag with !{0} move right or !{0} press right. Move to previous flag with !{0} move left or !{0} press left.  Submit with !{0} press ok.";
        //ModComponentSolverHelpMessages["shapeshift"] = "";
        ModComponentSolverHelpMessages["SillySlots"] = "Keep the slots with !{0} keep.  Pull the slots with !{0} pull.";
        ModComponentSolverHelpMessages["SimonScreamsModule"] = "Press the correct colors for each round with !{0} press B O Y.";
        ModComponentSolverHelpMessages["SkewedSlotsModule"] = "Submit the correct response with !{0} submit 1 2 3.";
        ModComponentSolverHelpMessages["SouvenirModule"] = "Submit the correct response with !{0} answer 3. Order is from top to bottom, then left to right.";
        //ModComponentSolverHelpMessages["switchModule"] = "";
        ModComponentSolverHelpMessages["symbolicPasswordModule"] = "Cycle a row with cycle t l. Cycle a column with cycle m. Submit with !{0} submit. Rows are TL/TR/BL/BR, columns are L/R/M. Spaces are important!";
        //ModComponentSolverHelpMessages["spwizTetris"] = "";
        ModComponentSolverHelpMessages["TextField"] = "Press the button in Row 2 column 3 and Row 3 Column 4 with !{0} press 3,2 4,3.";
        ModComponentSolverHelpMessages["TheBulbModule"] = "Press O with !{0} press O.  Press I with !{0} press I. Unscrew the bulb with !{0} unscrew.  Screw in the bulb with !{0} screw.";
        ModComponentSolverHelpMessages["TheClockModule"] = "Submit a time with !{0} set 12:34 am. Command must include a 12-hour time followed by AM/PM.";
        ModComponentSolverHelpMessages["TheGamepadModule"] = "Submit your answer with !{0} submit l r u d a b.";
        ModComponentSolverHelpMessages["ThirdBase"] = "Press a button with !{0} z0s8. Word must match the button as it would appear if the module was the right way up. Not case sensitive.";
        ModComponentSolverHelpMessages["TicTacToeModule"] = "Press a button with !{0} tl. Buttons are tl, tm, tr, ml, mm, mr, bl, bm, br.";
        ModComponentSolverHelpMessages["webDesign"] = "Accept the design with !{0} acc.  Consider the design with !{0} con.  Reject the design with !{0} reject.";
        ModComponentSolverHelpMessages["WirePlacementModule"] = "Cut the correct wires with !{0} cut A2 B4 D3.";
        //ModComponentSolverHelpMessages["WordScrambleModule"] = "";
        ModComponentSolverHelpMessages["WordSearchModule"] = "Select the word starting at column B row 3, and ending at column C row 4, with !{0} select B3 C4.";
        ModComponentSolverHelpMessages["YahtzeeModule"] = "Roll the dice with !{0} roll. Keep some dice with !{0} keep white,purple,blue,yellow,black. Roll the remaining dice until a 3 appears with !{0} roll until 3.";
        ModComponentSolverHelpMessages["ZooModule"] = "!{0} press animal, animal, ...; for example: !{0} press Koala, Eagle, Kangaroo, Camel, Hyena. The module will open the door and automatically press the animals that are there. Acceptable animal names are found at: https://ktane.timwi.de/HTML/Zoo%20names%20(samfun123).html";

        //Manual Codes
        ModComponentSolverManualCodes["ColourFlash"] = "Color Flash";
        ModComponentSolverManualCodes["RockPaperScissorsLizardSpockModule"] = "Rock-Paper-Scissors-Lizard-Spock";
        ModComponentSolverManualCodes["TicTacToeModule"] = "Tic-Tac-Toe";
        ModComponentSolverManualCodes["BitOps"] = "Bitwise Operators";
        ModComponentSolverManualCodes["RubiksCubeModule"] = "Rubik%E2%80%99s Cube";

		//Status Light Locations.
		//For most modules, the Status light is in the Top Right corner.  However, there is the odd module where the status
		//light might be in the Top left, Bottom right, or Bottom left corner.  In these cases, the ID number for multi-decker
		//should be moved accordingly.  //Use this only in cases where the location detection code results in incorrect placement
		//of the ID location.
		/*ModComponentSolverStatusLightLeft["ThirdBase"] = true;
        ModComponentSolverStatusLightBottom["ThirdBase"] = true;*/
		ModComponentSolverStatusLightLeft["ModuleAgainstHumanity"] = false;
		ModComponentSolverStatusLightBottom["ModuleAgainstHumanity"] = false;

		ModComponentSolverStatusLightLeft["DoubleOhModule"] = false;
		ModComponentSolverStatusLightBottom["DoubleOhModule"] = false;

		//Chat Rotation
		//Most modules behave correctly, and have NOT rotated the StatusLightParent needlessly.  There are a few that have done exactly that.

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
                return CreateModComponentSolver(bombCommander, bombComponent, ircConnection, canceller, solvableModule.ModuleType);                

            case ComponentTypeEnum.NeedyMod:
                KMNeedyModule needyModule = bombComponent.GetComponent<KMNeedyModule>();
                return CreateModComponentSolver(bombCommander, bombComponent, ircConnection, canceller, needyModule.ModuleType);

            default:
                throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays'.", (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(bombComponent, null)));
        }
    }

    private static ComponentSolver CreateModComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller, string moduleType)
    {
        if (ModComponentSolverCreators.ContainsKey(moduleType))
        {
            return ModComponentSolverCreators[moduleType](bombCommander, bombComponent, ircConnection, canceller);
        }

        Debug.LogFormat("Attempting to find a valid process command method to respond with on component {0}...", moduleType);

        ModComponentSolverDelegate modComponentSolverCreator = GenerateModComponentSolverCreator(bombComponent, moduleType);
        if (modComponentSolverCreator == null)
        {
            throw new NotSupportedException(string.Format("Currently {0} is not supported by 'Twitch Plays' - Could not generate a valid componentsolver for the mod component!", (string)CommonReflectedTypeInfo.ModuleDisplayNameField.Invoke(bombComponent, null)));
        }

        ModComponentSolverCreators[moduleType] = modComponentSolverCreator;

        return modComponentSolverCreator(bombCommander, bombComponent, ircConnection, canceller);
    }

    private static ModComponentSolverDelegate GenerateModComponentSolverCreator(MonoBehaviour bombComponent, string moduleType)
    {
        ModCommandType commandType = ModCommandType.Simple;
        Type commandComponentType = null;
        MethodInfo method = FindProcessCommandMethod(bombComponent, out commandType, out commandComponentType);
        string help = FindHelpMessage(bombComponent);
        string manual = FindManualCode(bombComponent);
        bool statusBottom = false;
        float rotation = 0;
        bool statusLeft = FindStatusLightPosition(bombComponent, out statusBottom, out rotation);
        

        if (help == null && ModComponentSolverHelpMessages.ContainsKey(moduleType))
            help = ModComponentSolverHelpMessages[moduleType];

        if (manual == null && ModComponentSolverManualCodes.ContainsKey(moduleType))
            manual = ModComponentSolverManualCodes[moduleType];

        if (ModComponentSolverStatusLightLeft.ContainsKey(moduleType))
            statusLeft = ModComponentSolverStatusLightLeft[moduleType];

        if (ModComponentSolverStatusLightBottom.ContainsKey(moduleType))
            statusBottom = ModComponentSolverStatusLightBottom[moduleType];

        if (method != null)
        {
            switch (commandType)
            {
                case ModCommandType.Simple:
                    return delegate (BombCommander _bombCommander, MonoBehaviour _bombComponent, IRCConnection _ircConnection, CoroutineCanceller _canceller)
                    {
                        Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
                        return new SimpleModComponentSolver(_bombCommander, _bombComponent, _ircConnection, _canceller, method, commandComponent, manual, help, statusLeft, statusBottom, rotation);
                    };
                case ModCommandType.Coroutine:
                    FieldInfo cancelfield;
                    Type canceltype;
                    FindCancelBool(bombComponent, out cancelfield, out canceltype);
                    return delegate (BombCommander _bombCommander, MonoBehaviour _bombComponent, IRCConnection _ircConnection, CoroutineCanceller _canceller)
                    {
                        Component commandComponent = _bombComponent.GetComponentInChildren(commandComponentType);
                        return new CoroutineModComponentSolver(_bombCommander, _bombComponent, _ircConnection, _canceller, method, commandComponent, manual, help, cancelfield, canceltype, statusLeft, statusBottom, rotation);
                    };

                default:
                    break;
            }
        }

        return null;
    }

    private static bool FindStatusLightPosition(MonoBehaviour bombComponent, out bool StatusLightBottom, out float Rotation)
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
                StatusLightBottom = (component.transform.localPosition.z < 0);
                Rotation = component.transform.localEulerAngles.y;
                return (component.transform.localPosition.x < 0);
            }
        }
        Debug.Log("StatusLightParent not found :(");
        StatusLightBottom = false;
        Rotation = 0;
        return false;
    }

    private static string FindManualCode(MonoBehaviour bombComponent)
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
                return (string)candidateString.GetValue(bombComponent.GetComponent(type));
        }
        return null;
    }

    private static string FindHelpMessage(MonoBehaviour bombComponent)
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
                return (string)candidateString.GetValue(bombComponent.GetComponent(type));
        }
        return null;
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

