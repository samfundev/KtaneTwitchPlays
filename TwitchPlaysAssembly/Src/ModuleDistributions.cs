using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TwitchPlays.ScoreMethods;
using UnityEngine;

[Serializable]
public sealed class DistributionPool : ISerializable
{
	public enum PoolType
	{
		// Default pool type or error, gives a warning if attempted to run
		Invalid,

		// Adds a pool of all enabled solvable modules
		// Examples:
		//     "AllSolvable" (fair mix)
		//     "AllSolvable: Mods" (mods only)
		AllSolvable,

		// Adds a pool of all enabled needy modules
		// Examples:
		//     "AllNeedy" (fair mix)
		//     "AllNeedy: Mods" (mods only)
		AllNeedy,

		// Adds a pool of all enabled modules, both solvable and needy
		// Examples:
		//     "AllModules" (fair mix including needies)
		//     "AllModules: Mods" (as above, but mods only)
		AllModules,

		// Adds a pool containing all solvable modules that fit specific score constraints
		// Ignores boss modules, or modules that don't have a base score
		// TODO: Needs some way of accounting for dynamic scoring?
		// Examples:
		//     "Score: = 10" (modules with a base score of exactly 10)
		//     "Score: < 7" (base score of 6 or less)
		//     "Score: >= 7, <= 13" (base score between 7 and 13)
		Score,

		// Adds a pool containing all solvable modules enabled by an expert profile
		// Examples:
		//     EnabledBy: MyExpertProfile
		//     EnabledBy: FooProfile, BarProfile (modules enabled by both)
		ProfileEnables,

		// Adds a pool containing all solvable modules disabled by a defuser profile
		// Examples:
		//     DisabledBy: NoBossModules
		//     DisabledBy: NoColCipher, NoDream (modules disabled by both)
		ProfileDisables,

		// Adds a fixed pool of module IDs, works like a typical mission pool, meaning:
		//  - The pool may contain duplicates to make one module more likely to show up
		//  - All modules in a fixed pool must be enabled
		// Examples:
		//     Fixed: spwizTetris (just one module)
		//     Fixed: brainf, HexiEvilFMN (pick between two)
		//     Fixed: Wires, Wires, Wires, Wires, Venn (80% Wires, 20% Complicated Wires)
		Fixed,
	}

	private readonly int? RewardPerModule;
	private readonly int? TimePerModule;
	public readonly float Weight;

	private PoolType Type;
	private List<string> Arguments;
	private Dictionary<string, int> PoolSettings;

	private string __poolDef;
	public string PoolDefinition
	{
		get => __poolDef;
		private set
		{
			__poolDef = value;
			Type = PoolType.Invalid;
			Arguments = __poolDef.Split(new char[] {',', ':'}).Select(str => str.Trim()).ToList();
			PoolSettings = new Dictionary<string, int>();

			string mode = Arguments[0].ToUpperInvariant();
			Arguments.RemoveAt(0);

			// Don't change Type until the end, so modes can immediately return to delcare a pool invalid
			PoolType? FinalType = null;

			switch (mode)
			{
				case "SOLVABLE":
				case "ALLSOLVABLE":
				case "ALL_SOLVABLE":
					FinalType = FinalType ?? PoolType.AllSolvable;

					int ComponentSource = (int) KMComponentPool.ComponentSource.Mods | (int) KMComponentPool.ComponentSource.Base;
					if (Arguments.Count == 1)
					{
						if (Arguments[0].ToUpperInvariant().Equals("MODS"))
							ComponentSource = (int) KMComponentPool.ComponentSource.Mods;
						else if (Arguments[0].ToUpperInvariant().Equals("BASE"))
							ComponentSource = (int) KMComponentPool.ComponentSource.Base;
						else
							return; // Not valid
					}
					else if (Arguments.Count >= 2)
						return; // Also not valid

					PoolSettings["Component Source"] = ComponentSource;
					break;

				case "NEEDY":
				case "ALLNEEDY":
				case "ALL_NEEDY":
					FinalType = PoolType.AllNeedy;
					goto case "ALL_SOLVABLE";

				case "ALLMODULES":
				case "ALL_MODULES":
					FinalType = PoolType.AllModules;
					goto case "ALL_SOLVABLE";

				case "SCORE":
					FinalType = PoolType.Score;

					Match mt;
					int ScoreMin = int.MinValue, ScoreMax = int.MaxValue;
					if (Arguments.Count == 0 || Arguments.Count > 2)
						return; // Not valid

					if (Arguments.Count == 1 && (mt = Regex.Match(Arguments[0], @"^= *(\d+)$")).Success)
					{
						if (!int.TryParse(mt.Groups[1].ToString(), out ScoreMin))
							return; // Invalid number - unlikely, but possible
						ScoreMax = ScoreMin;
					}
					else
						foreach (string arg in Arguments)
						{
							if ((mt = Regex.Match(arg, @"^([<>]=?) *(\d+)$")).Success)
							{
								if (!int.TryParse(mt.Groups[2].ToString(), out int temp))
									return; // Invalid number - unlikely, but possible
								switch (mt.Groups[1].ToString())
								{
									case ">": ScoreMin = temp + 1; break;
									case "<": ScoreMax = temp - 1; break;
									case ">=": ScoreMin = temp; break;
									case "<=": ScoreMax = temp; break;
									default: return;
								}
							}
							else
								return; // Invalid - bad form
						}

					if (Arguments.Count == 2 && (ScoreMin == int.MinValue || ScoreMax == int.MaxValue))
						return; // Don't allow conflicting constraints like "< 10, < 50"

					PoolSettings["Score Min"] = ScoreMin;
					PoolSettings["Score Max"] = ScoreMax;
					break;

				case "ENABLEDBY":
				case "ENABLED_BY":
				case "PROFILE_ENABLED_BY":
					FinalType = PoolType.ProfileEnables;
					goto case "FIXED";

				case "DISABLEDBY":
				case "DISABLED_BY":
				case "PROFILE_DISABLED_BY":
					FinalType = PoolType.ProfileDisables;
					goto case "FIXED";

				case "FIXED":
					FinalType = FinalType ?? PoolType.Fixed;
					if (Arguments.Count == 0)
						return; // Not valid
					// All cases here use the argument list when generating pools
					break;
			}
			Type = FinalType ?? PoolType.Invalid;
		}
	}

	// Serialization
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("Definition", __poolDef, typeof(string));
		info.AddValue("Weight", Weight, typeof(float));
		if (RewardPerModule != null)
			info.AddValue("Reward", RewardPerModule, typeof(int));
		if (TimePerModule != null)
			info.AddValue("Time", TimePerModule, typeof(int));
	}

	// Deserialization
	private DistributionPool(SerializationInfo info, StreamingContext context)
	{
		PoolDefinition = (string) info.GetValue("Definition", typeof(string));

		Weight = (float) info.GetValue("Weight", typeof(float));

		// May not be present, and if so leaves RewardPerModule at default
		try { RewardPerModule = (int) info.GetValue("Reward", typeof(int)); }
		catch (SerializationException) { }

		// May not be present, and if so leaves TimePerModule at default
		try { TimePerModule = (int) info.GetValue("Time", typeof(int)); }
		catch (SerializationException) { }
	}

	public DistributionPool(float weight, string def, int? reward = null, int? time = null)
	{
		Weight = weight;
		RewardPerModule = reward;
		TimePerModule = time;
		PoolDefinition = def;
	}

	private static string GetTwitchPlaysID(KMGameInfo.KMModuleInfo info) => info.IsMod ? info.ModuleId : info.ModuleType.ToString();

	public KMComponentPool ToComponentPool(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException("Count cannot be negative");

		KMGameInfo gi = TwitchPlaysService.Instance.GetComponent<KMGameInfo>();
		List<KMGameInfo.KMModuleInfo> AllModules = gi.GetAvailableModuleInfo().ToList();

		// Place a list of KMModuleInfos into this variable, and they'll be added into a ComponentPool at the end.
		List<KMGameInfo.KMModuleInfo> ModulePool = null;

		switch (Type)
		{
			case PoolType.AllSolvable:
			case PoolType.AllNeedy:
				// Use the game's built in ALL_SOLVABLE/ALL_NEEDY types.
				return new KMComponentPool()
				{
					SpecialComponentType = (Type == PoolType.AllSolvable
						? KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE
						: KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY),
					AllowedSources = (KMComponentPool.ComponentSource) PoolSettings["Component Source"],
					Count = count
				};
	
			case PoolType.AllModules:
				// This one doesn't exist in the game, so we have to make it ourselves.
				ModulePool = AllModules.Where(Module => {
					if (Module.IsMod) return (PoolSettings["Component Source"] & (int)KMComponentPool.ComponentSource.Mods) != 0;
					else              return (PoolSettings["Component Source"] & (int)KMComponentPool.ComponentSource.Base) != 0;
				}).ToList();
				break;

			case PoolType.Score:
				ModulePool = AllModules.Where(x =>
				{
					if (x.IsNeedy)
						return false;

					ModuleInformation info = ComponentSolverFactory.GetModuleInfo(GetTwitchPlaysID(x), false);
					var baseMethod = info.GetScoreMethod<BaseScore>();
					if (baseMethod == null || info.announceModule)
						return false;

					var baseScore = baseMethod.Points;
					return baseScore >= PoolSettings["Score Min"] && baseScore <= PoolSettings["Score Max"];
				}).ToList();
				break;

			case PoolType.ProfileEnables:
			case PoolType.ProfileDisables:
				HashSet<string> relevantMods = new HashSet<string>();

				foreach (string ProfileName in Arguments)
				{
					ProfileHelper.Profile data;
					try { data = ProfileHelper.GetProfile(ProfileName); }
					catch (FileNotFoundException) { throw new InvalidOperationException($"Profile {ProfileName} doesn't exist."); }

					relevantMods.UnionWith((Type == PoolType.ProfileEnables) ? data.EnabledList : data.DisabledList);
				}

				// Profiles cannot contain info about vanilla modules, so a profile pool is always mods only.
				ModulePool = AllModules.Where(x => x.IsMod && !x.IsNeedy && relevantMods.Contains(x.ModuleId)).ToList();
				break;

			case PoolType.Fixed:
				// We treat a fixed pool like a regualar pool in a mission; i.e. we allow duplicates for the purposes of
				// making some modules more likely than others, and if any module is missing then we bail out.
				Dictionary<string, KMGameInfo.KMModuleInfo> reverseLookup = AllModules.ToDictionary(x => GetTwitchPlaysID(x), x => x);

				ModulePool = Arguments.Select(Module => {
					if (!reverseLookup.ContainsKey(Module))
						throw new InvalidOperationException($"This distribution contains a fixed pool, and at least one of the modules in that pool ({Module}) is not enabled.");
					return reverseLookup[Module];
				}).ToList();
				break;

			default:
				throw new InvalidOperationException($"The following pool definition is invalid: \"{__poolDef}\"");
		}

		if ((ModulePool?.Count ?? 0) == 0) // Anti-softlock
			throw new InvalidOperationException($"No enabled modules fit the requirements of the following pool: \"{__poolDef}\"");
		return new KMComponentPool()
		{
			ComponentTypes = ModulePool.Where(x => !x.IsMod).Select(x => x.ModuleType).ToList(),
			ModTypes = ModulePool.Where(x => x.IsMod).Select(x => x.ModuleId).ToList(),
			Count = count
		};
	}

	private bool IsVanillaPool() => Type == PoolType.AllSolvable && PoolSettings["Component Source"] == (int) KMComponentPool.ComponentSource.Base;

	public int RewardPointsGiven(int count) => (RewardPerModule ?? (IsVanillaPool() ? 2 : 5)) * count;
	public int TimeGiven(int count) => (TimePerModule ?? (IsVanillaPool() ? 60 : TwitchPlaySettings.data.NormalModeSecondsPerModule)) * count;
}

public sealed class ModuleDistributions
{
	public string DisplayName;
	public List<DistributionPool> Pools;
	public int MinModules = 1;
	public int MaxModules = 101;
	public bool Enabled = true;
	public bool Hidden = false;

	private int[] ModulesPerPool(int numModules)
	{
		// Before assigning: Any pools with weight <= 0 are single force spawns
		int[] modCount = Pools.Select(pool => pool.Weight <= 0f ? 1 : 0).ToArray();
		int numNonForcedModules = numModules - modCount.Sum();

		for (int i = 0; i < Pools.Count; ++i)
		{
			if (Pools[i].Weight > 0f)
				modCount[i] = Mathf.FloorToInt(Pools[i].Weight * numNonForcedModules);
		}

		// Okay, that might have left us with less than numModules accounted for.
		// Divvy up the remainder to the first non-forced pools in the list.
		for (int i = 0; i < Pools.Count && modCount.Sum() < numModules; ++i)
		{
			if (Pools[i].Weight > 0f)
				++modCount[i];
		}

		if (modCount.Sum() != numModules) // Usually means the weights don't add up to 1
			throw new InvalidOperationException($"Please contact a developer; tried to generate a {numModules} module bomb, instead got {modCount.Sum()} modules.");
		return modCount;
	}

	private List<KMComponentPool> GeneratePools(int[] modsPerPool)
	{
		// Generate KMComponentPools from our DistributionPools.
		return Pools.Select((pool, i) => pool.ToComponentPool(modsPerPool[i])).ToList();
	}

	private int RewardPoints(int[] modsPerPool) => Math.Max(0, Pools.Select((pool, i) => pool.RewardPointsGiven(modsPerPool[i])).Sum());
	private int StartingTime(int[] modsPerPool) => Math.Max(60, Pools.Select((pool, i) => pool.TimeGiven(modsPerPool[i])).Sum());

	public KMGeneratorSetting GenerateMission(int moduleCount, bool timeMode, out int rewardPoints)
	{
		int[] modsPerPool = ModulesPerPool(moduleCount);
		rewardPoints = RewardPoints(modsPerPool);

		return timeMode ?
			new KMGeneratorSetting()
			{
				ComponentPools = GeneratePools(modsPerPool),
				TimeLimit = TwitchPlaySettings.data.TimeModeStartingTime * 60,
				NumStrikes = 9
			}
			:
			new KMGeneratorSetting()
			{
				ComponentPools = GeneratePools(modsPerPool),
				TimeLimit = StartingTime(modsPerPool),
				NumStrikes = Math.Max(3, moduleCount / TwitchPlaySettings.data.ModuleToStrikeRatio)
			};
	}
}
