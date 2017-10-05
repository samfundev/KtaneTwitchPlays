using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class PianoKeysComponentSolver : ComponentSolver
{
	public PianoKeysComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_component = bombComponent.GetComponent(_componentType);
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

    public class Note
    {
        public Note(Semitone semitone, int octave, float duration = 0.125f)
        {
            Semitone = semitone;
            Octave = octave;
            Duration = duration;
        }

        public Semitone Semitone;
        public int Octave;
        public float Duration;
    }

    public class Melody
    {
        public string Name;
        public Note[] Notes;
        public int Tempo = 130;

        public override string ToString()
        {
            return Name;
        }
    }

    public static class MelodyDatabase
    {
        #region Note Lengths

        private static float Minim = 0.5f;
        private static float Crotchet = 0.25f;
        private static float Quaver = 0.125f;
        private static float Semiquaver = 0.0625f;
        private static float Triplet = Crotchet / 3;

        #endregion

        #region Regular Melodies

        public static readonly Melody FinalFantasyVictory = new Melody()
        {
            Name = "Final Fantasy Victory Fanfare",
            Tempo = 140,
            Notes = new Note[]
            {
                new Note(Semitone.ASharp, 3, Triplet),
                new Note(Semitone.ASharp, 3, Triplet),
                new Note(Semitone.ASharp, 3, Triplet),
                new Note(Semitone.ASharp, 3, Crotchet),
                new Note(Semitone.FSharp, 3, Crotchet),
                new Note(Semitone.GSharp, 3, Crotchet),
                new Note(Semitone.ASharp, 3, Triplet * 2),
                new Note(Semitone.GSharp, 3, Triplet),
                new Note(Semitone.ASharp, 3)
            }
        };

        public static readonly Melody GuilesTheme = new Melody()
        {
            Name = "Guile's Theme",
            Tempo = 121,
            Notes = new Note[]
            {
                new Note(Semitone.DSharp, 3, Quaver),
                new Note(Semitone.DSharp, 3, Semiquaver),
                new Note(Semitone.D, 3, Quaver),
                new Note(Semitone.D, 3, Semiquaver),
                new Note(Semitone.DSharp, 3, Semiquaver * 7),
                new Note(Semitone.DSharp, 3, Semiquaver),
                new Note(Semitone.D, 3, Quaver),
                new Note(Semitone.DSharp, 3, Quaver),
                new Note(Semitone.DSharp, 3, Semiquaver),
                new Note(Semitone.D, 3, Quaver),
                new Note(Semitone.D, 3, Semiquaver),
                new Note(Semitone.DSharp, 3)
            }
        };

        public static readonly Melody JamesBond = new Melody()
        {
            Name = "James Bond Theme",
            Tempo = 138,
            Notes = new Note[]
            {
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.FSharp, 3, Semiquaver),
                new Note(Semitone.FSharp, 3, Semiquaver),
                new Note(Semitone.FSharp, 3, Quaver),
                new Note(Semitone.FSharp, 3, Crotchet),
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.E, 3)
            }
        };

        public static readonly Melody JurrasicPark = new Melody()
        {
            Name = "Jurassic Park Theme",
            Tempo = 105,
            Notes = new Note[]
            {
                new Note(Semitone.ASharp, 3, Quaver),
                new Note(Semitone.A, 3, Quaver),
                new Note(Semitone.ASharp, 3, Crotchet),
                new Note(Semitone.F, 3, Crotchet),
                new Note(Semitone.DSharp, 3, Crotchet),
                new Note(Semitone.ASharp, 3, Quaver),
                new Note(Semitone.A, 3, Quaver),
                new Note(Semitone.ASharp, 3, Crotchet),
                new Note(Semitone.F, 3, Crotchet),
                new Note(Semitone.DSharp, 3)
            }
        };

        public static readonly Melody SuperMarioBros = new Melody()
        {
            Name = "Mario Bros. Overworld Theme",
            Tempo = 200,
            Notes = new Note[]
            {
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.E, 3, Crotchet),
                new Note(Semitone.E, 3, Crotchet),
                new Note(Semitone.C, 3, Quaver),
                new Note(Semitone.E, 3, Crotchet),
                new Note(Semitone.G, 3, Minim),
                new Note(Semitone.G, 2)
            }
        };

        public static readonly Melody PinkPanther = new Melody()
        {
            Name = "The Pink Panther Theme",
            Tempo = 120,
            Notes = new Note[]
            {
                new Note(Semitone.CSharp, 3, Semiquaver),
                new Note(Semitone.D, 3, Semiquaver * 5),
                new Note(Semitone.E, 3, Semiquaver),
                new Note(Semitone.F, 3, Semiquaver * 5),
                new Note(Semitone.CSharp, 3, Semiquaver),
                new Note(Semitone.D, 3, Quaver),
                new Note(Semitone.E, 3, Semiquaver),
                new Note(Semitone.F, 3, Quaver),
                new Note(Semitone.ASharp, 3, Semiquaver),
                new Note(Semitone.A, 3)
            }
        };

        public static readonly Melody Superman = new Melody()
        {
            Name = "Superman Theme",
            Tempo = 112,
            Notes = new Note[]
            {
                new Note(Semitone.G, 3, Quaver * 3),
                new Note(Semitone.G, 3, Quaver),
                new Note(Semitone.C, 3, Quaver),
                new Note(Semitone.G, 3, Quaver),
                new Note(Semitone.G, 3, Quaver * 6),
                new Note(Semitone.C, 4, Quaver * 3),
                new Note(Semitone.G, 3, Quaver * 3),
                new Note(Semitone.C, 3)
            }
        };

        public static readonly Melody TetrisA = new Melody()
        {
            Name = "Tetris Mode-A Theme",
            Tempo = 140,
            Notes = new Note[]
            {
                new Note(Semitone.A, 3, Crotchet),
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.F, 3, Quaver),
                new Note(Semitone.G, 3, Crotchet),
                new Note(Semitone.F, 3, Quaver),
                new Note(Semitone.E, 3, Quaver),
                new Note(Semitone.D, 3, Crotchet),
                new Note(Semitone.D, 3, Quaver),
                new Note(Semitone.F, 3, Quaver),
                new Note(Semitone.A, 3)
            }
        };

        public static readonly Melody EmpireStrikesBack = new Melody()
        {
            Name = "The Empire Strikes Back Theme",
            Tempo = 108,
            Notes = new Note[]
            {
                new Note(Semitone.G, 3, Crotchet),
                new Note(Semitone.G, 3, Crotchet),
                new Note(Semitone.G, 3, Crotchet),
                new Note(Semitone.DSharp, 3, Triplet * 2),
                new Note(Semitone.ASharp, 3, Triplet),
                new Note(Semitone.G, 3, Crotchet),
                new Note(Semitone.DSharp, 3, Triplet * 2),
                new Note(Semitone.ASharp, 3, Triplet),
                new Note(Semitone.G, 3)
            }
        };

        public static readonly Melody ZeldasLullaby = new Melody()
        {
            Name = "Zelda's Lullaby Theme",
            Tempo = 110,
            Notes = new Note[]
            {
                new Note(Semitone.B, 3, Minim),
                new Note(Semitone.D, 4, Crotchet),
                new Note(Semitone.A, 3, Minim),
                new Note(Semitone.G, 3, Quaver),
                new Note(Semitone.A, 3, Quaver),
                new Note(Semitone.B, 3, Minim),
                new Note(Semitone.D, 4, Crotchet),
                new Note(Semitone.A, 3)
            }
        };

        public static readonly Melody[] Melodies =
        {
            FinalFantasyVictory,GuilesTheme,JamesBond,JurrasicPark,SuperMarioBros,PinkPanther,Superman,TetrisA,EmpireStrikesBack,ZeldasLullaby
        };

        #endregion
    }

    protected override IEnumerator RespondToCommandInternal(string command)
    {
        KMSelectable[] NotesToPress = (KMSelectable[])_ProcessCommandMethod.Invoke(_component, new object[] {command});
        if (NotesToPress == null)
        {
            yield break;
        }

	    command = command.Substring(5);

	    string[] sequence = command.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

	    List<Semitone> toPress = new List<Semitone>();

	    foreach (string buttonString in sequence)
	    {
	        switch (buttonString.ToLowerInvariant())
	        {
	            case "c":
	                toPress.Add(Semitone.C);
	                break;
	            case "c#":
	            case "c♯":
	            case "db":
	            case "d♭":
	                toPress.Add(Semitone.CSharp);
	                break;
	            case "d":
	                toPress.Add(Semitone.D);
	                break;
	            case "d#":
	            case "d♯":
	            case "eb":
	            case "e♭":
	                toPress.Add(Semitone.DSharp);
	                break;
	            case "e":
	                toPress.Add(Semitone.E);
	                break;
	            case "f":
	                toPress.Add(Semitone.F);
	                break;
	            case "f#":
	            case "f♯":
	            case "gb":
	            case "g♭":
	                toPress.Add(Semitone.FSharp);
	                break;
	            case "g":
	                toPress.Add(Semitone.G);
	                break;
	            case "g#":
	            case "g♯":
	            case "ab":
	            case "a♭":
	                toPress.Add(Semitone.GSharp);
	                break;
	            case "a":
	                toPress.Add(Semitone.A);
	                break;
	            case "a#":
	            case "a♯":
	            case "bb":
	            case "b♭":
	                toPress.Add(Semitone.ASharp);
	                break;
	            case "b":
	                toPress.Add(Semitone.B);
	                break;

	            default:
	                break;
	        }
	    }

	    float tempoMultiplier = 1.45f; // To make up for yield-related slowness, and speed it up a little in general
	    int tempo;

	    // Attempt to find a matching melody in the database
	    Semitone[] inputSemitones = toPress.ToArray();

        Melody song = (from melody in MelodyDatabase.Melodies let melodySemitones = melody.Notes.Select((x) => x.Semitone).ToArray() where inputSemitones.SequenceEqual(melodySemitones) select melody).FirstOrDefault();
        if (song == null)
        {
            yield return null;
            foreach (KMSelectable note in NotesToPress)
            {
                yield return DoInteractionClick(note);
            }
            yield break;
        }

        tempo = song.Tempo;
        for (int i = 0; i < inputSemitones.Length; i++)
        {
            float duration = song.Notes[i].Duration / tempo * 240 / tempoMultiplier;
            DoInteractionClick(NotesToPress[i]);
            yield return new WaitForSeconds(duration);
        }
    }

	static PianoKeysComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("PianoKeysModule");
		_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.Instance);
    }

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;

	private object _component = null;

    public enum Semitone
    {
        [Description("C")]
        C,
        [Description("C#/Db")]
        CSharp,
        [Description("D")]
        D,
        [Description("D#/Eb")]
        DSharp,
        [Description("E")]
        E,
        [Description("F")]
        F,
        [Description("F#/Gb")]
        FSharp,
        [Description("G")]
        G,
        [Description("G#/Ab")]
        GSharp,
        [Description("A")]
        A,
        [Description("A#/Bb")]
        ASharp,
        [Description("B")]
        B
    }
}
