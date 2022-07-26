using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using TwitchPlays.ScoreMethods;

[Serializable]
public sealed class DistributionPool : ISerializable
{
	private readonly int RewardPerModule;
	private readonly int TimePerModule;
	public readonly float Weight;

	public enum PoolType
	{
		Invalid,
		AllSolvable,
		AllNeedy,
		AllModules,
		Score,
		Fixed,
	}
	private PoolType Type;
	private List<int> ExtraData;

	private string __poolDef;
	public string PoolDefinition
	{
		get => __poolDef;
		private set
		{
			__poolDef = value;

			Type = PoolType.Invalid;
			ExtraData = new List<int>();

			List<string> arguments = __poolDef.Split(',').Select(str => str.Trim().ToUpperInvariant()).ToList();
			string mode = arguments[0];
			arguments.RemoveAt(0);

			bool IsNeedy = false;
			switch (mode)
			{
				// Examples:
				//     ALL_SOLVABLE (fair mix)
				//     ALL_SOLVABLE, MODS (mods only)
				case "SOLVABLE":
				case "ALLSOLVABLE":
				case "ALL_SOLVABLE":
					int ComponentSource = (int) KMComponentPool.ComponentSource.Mods | (int) KMComponentPool.ComponentSource.Base;
					if (arguments.Count == 1)
					{
						if (arguments[0].Equals("MODS"))
							ComponentSource = (int) KMComponentPool.ComponentSource.Mods;
						else if (arguments[0].Equals("BASE"))
							ComponentSource = (int) KMComponentPool.ComponentSource.Base;
						else
							return; // Not valid
					}
					else if (arguments.Count >= 2)
						return; // Also not valid

					Type = IsNeedy ? PoolType.AllNeedy : PoolType.AllSolvable;
					ExtraData.Add(ComponentSource);
					break;

				// Examples:
				//     ALL_NEEDY (fair mix)
				//     ALL_NEEDY, MODS (mods only)
				case "NEEDY":
				case "ALLNEEDY":
				case "ALL_NEEDY":
					IsNeedy = true;
					goto case "ALL_SOLVABLE";

				// Examples:
				//     ALL_MODULES (fair mix including needies)
				case "ALL_MODULES":
				case "ALLMODULES":
					ComponentSource = (int) KMComponentPool.ComponentSource.Mods | (int) KMComponentPool.ComponentSource.Base;
					Type = PoolType.AllModules;
					ExtraData.Add(ComponentSource);
					break;

				// Examples:
				//     SCORE, =10
				//     SCORE, <7
				//     SCORE, >=7, <=13
				case "SCORE":
					Match mt;
					int ScoreMin = int.MinValue, ScoreMax = int.MaxValue;
					if (arguments.Count == 0 || arguments.Count > 2)
						return; // Not valid

					if (arguments.Count == 1 && (mt = Regex.Match(arguments[0], @"^= *(\d+)$")).Success)
					{
						if (!int.TryParse(mt.Groups[1].ToString(), out ScoreMin))
							return;
						ScoreMax = ScoreMin;
					}
					else
						foreach (string arg in arguments)
						{
							if ((mt = Regex.Match(arg, @"^([<>]=?) *(\d+)$")).Success)
							{
								if (!int.TryParse(mt.Groups[2].ToString(), out int temp))
									return;
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
								return;
						}

					if (arguments.Count == 2 && (ScoreMin == int.MinValue || ScoreMax == int.MaxValue))
						return;

					Type = PoolType.Score;
					ExtraData.Add(ScoreMin);
					ExtraData.Add(ScoreMax);
					break;

				// Examples:
				//     FIXED, spwizTetris
				//     FIXED, brainf, HexiEvilFMN
				case "FIXED":
					if (arguments.Count == 0)
						return; // Not valid

					// We don't really get to do much, since we're given the entire pool.
					Type = PoolType.Fixed;
					break;
			}
		}
	}

	// Serialization
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("Definition", __poolDef, typeof(string));
		info.AddValue("Weight", Weight, typeof(float));
		if (RewardPerModule >= 0)
			info.AddValue("Reward", RewardPerModule, typeof(int));
		if (TimePerModule >= 0)
			info.AddValue("Time", TimePerModule, typeof(int));
	}

	// Deserialization
	private DistributionPool(SerializationInfo info, StreamingContext context)
	{
		PoolDefinition = (string) info.GetValue("Definition", typeof(string));

		Weight = (float) info.GetValue("Weight", typeof(float));

		// May not be present, and if so leaves RewardPerModule at default
		try { RewardPerModule = (int) info.GetValue("Reward", typeof(int)); }
		catch (SerializationException) { RewardPerModule = -1; }

		// May not be present, and if so leaves TimePerModule at default
		try { TimePerModule = (int) info.GetValue("Time", typeof(int)); }
		catch (SerializationException) { TimePerModule = -1; }
	}

	public DistributionPool(float weight, string def)
	{
		Weight = weight;
		RewardPerModule = -1;
		TimePerModule = -1;
		PoolDefinition = def;
	}

	public DistributionPool(float weight, int reward, int time, string def)
	{
		Weight = weight;
		RewardPerModule = reward;
		TimePerModule = time;
		PoolDefinition = def;
	}

	private static string GetTwitchPlaysID(KMGameInfo.KMModuleInfo moduleInfo)
	{
		if (moduleInfo.IsMod)
			return moduleInfo.ModuleId;
		switch (moduleInfo.ModuleType)
		{
			case KMComponentPool.ComponentTypeEnum.Wires:
				return "WireSetComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Keypad:
				return "KeypadComponentSolver";
			case KMComponentPool.ComponentTypeEnum.BigButton:
				return "ButtonComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Memory:
				return "MemoryComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Simon:
				return "SimonComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Venn:
				return "VennWireComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Morse:
				return "MorseCodeComponentSolver";
			case KMComponentPool.ComponentTypeEnum.WireSequence:
				return "WireSequenceComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Password:
				return "PasswordComponentSolver";
			case KMComponentPool.ComponentTypeEnum.Maze:
				return "InvisibleWallsComponentSolver";
			case KMComponentPool.ComponentTypeEnum.WhosOnFirst:
				return "WhosOnFirstComponentSolver";
			case KMComponentPool.ComponentTypeEnum.NeedyVentGas:
				return "NeedyVentComponentSolver";
			case KMComponentPool.ComponentTypeEnum.NeedyCapacitor:
				return "NeedyDischargeComponentSolver";
			case KMComponentPool.ComponentTypeEnum.NeedyKnob:
				return "NeedyKnobComponentSolver";
			default:
				return moduleInfo.ModuleId;
		}
	}

	public KMComponentPool ToComponentPool(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException("Count cannot be negative");

		KMGameInfo gi = TwitchPlaysService.Instance.GetComponent<KMGameInfo>();

		switch (Type)
		{
			case PoolType.AllSolvable:
			case PoolType.AllNeedy:
				return new KMComponentPool()
				{
					SpecialComponentType = (Type == PoolType.AllSolvable
						? KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE
						: KMComponentPool.SpecialComponentTypeEnum.ALL_NEEDY),
					AllowedSources = (KMComponentPool.ComponentSource) ExtraData[0],
					Count = count
				};
			case PoolType.AllModules:
				List<KMGameInfo.KMModuleInfo> allModules = gi.GetAvailableModuleInfo().ToList();
				return new KMComponentPool()
				{
					ComponentTypes = allModules.Where(x => !x.IsMod).Select(x => x.ModuleType).ToList(),
					ModTypes = allModules.Where(x => x.IsMod).Select(x => x.ModuleId).ToList(),
					Count = count
				};
			case PoolType.Score:
				List<KMGameInfo.KMModuleInfo> scoredModules = gi.GetAvailableModuleInfo().Where(x =>
				{
					if (x.IsNeedy)
						return false;

					ModuleInformation info = ComponentSolverFactory.GetModuleInfo(GetTwitchPlaysID(x), false);
					var baseMethod = info.GetScoreMethod<BaseScore>();
					if (baseMethod == null || info.announceModule)
						return false;

					var baseScore = baseMethod.Points;
					return baseScore >= ExtraData[0] && baseScore <= ExtraData[1];
				}).ToList();

				if (scoredModules.Count == 0)
					throw new InvalidOperationException("There are no enabled modules that fit the score requirements.");

				return new KMComponentPool()
				{
					ComponentTypes = scoredModules.Where(x => !x.IsMod).Select(x => x.ModuleType).ToList(),
					ModTypes = scoredModules.Where(x => x.IsMod).Select(x => x.ModuleId).ToList(),
					Count = count
				};
			case PoolType.Fixed:
				List<string> moduleIDs = __poolDef.Split(',').Select(str => str.Trim()).ToList();
				moduleIDs.RemoveAt(0); // Remove "FIXED"

				List<string> availableMods = gi.GetAvailableModuleInfo().Where(x => x.IsMod).Select(x => x.ModuleId).ToList();
				List<KMComponentPool.ComponentTypeEnum> poolVanillas = new List<KMComponentPool.ComponentTypeEnum>();
				List<string> poolMods = new List<string>();

				foreach (string module in moduleIDs)
				{
					if (Enum.GetNames(typeof(KMComponentPool.ComponentTypeEnum)).Contains(module))
						poolVanillas.Add((KMComponentPool.ComponentTypeEnum) Enum.Parse(typeof(KMComponentPool.ComponentTypeEnum), module));
					else
					{
						if (!availableMods.Contains(module))
							throw new InvalidOperationException($"This distribution contains a fixed pool, and at least one of the modules in that pool ({module}) is not enabled.");
						poolMods.Add(module);
					}
				}
				return new KMComponentPool()
				{
					ComponentTypes = poolVanillas,
					ModTypes = poolMods,
					Count = count
				};
			default:
				throw new InvalidOperationException("One of the pools in this distribution is not set up properly.");
		}
	}

	public int RewardPointsGiven(int count)
	{
		if (RewardPerModule != -1)
			return RewardPerModule * count;

		if (Type == PoolType.AllSolvable && ExtraData[0] == (int) KMComponentPool.ComponentSource.Base)
			return 2 * count;
		return 5 * count;
	}

	public int TimeGiven(int count)
	{
		if (TimePerModule != -1)
			return TimePerModule * count;

		if (Type == PoolType.AllSolvable && ExtraData[0] == (int) KMComponentPool.ComponentSource.Base)
			return 60 * count;
		return 120 * count;
	}
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
		int i = 0;
		// Before assigning: Any pools with weight <= 0 are single force spawns
		int[] modCount = Pools.Select(pool => pool.Weight <= 0f ? 1 : 0).ToArray();
		int numNonForcedModules = numModules - modCount.Sum();

		for (; i < Pools.Count; ++i)
		{
			if (Pools[i].Weight > 0f)
				modCount[i] = Mathf.FloorToInt(Pools[i].Weight * numNonForcedModules);
		}

		// Okay, that might have left us with less than numModules accounted for.
		// Divvy up the remainder to the first non-forced pools in the list.
		int modulesLeft = numModules - modCount.Sum();
		i = 0;
		while (modCount.Sum() < numModules)
		{
			if (Pools[i].Weight > 0f)
				++modCount[i];
			++i;
		}
		return modCount;
	}

	private int RewardPoints(int[] modsPerPool)
	{
		return Pools.Select((pool, i) => pool.RewardPointsGiven(modsPerPool[i])).Sum();
	}

	private int StartingTime(int[] modsPerPool)
	{
		return Pools.Select((pool, i) => pool.TimeGiven(modsPerPool[i])).Sum();
	}

	private List<KMComponentPool> GeneratePools(int[] modsPerPool)
	{
		// Generate KMComponentPools from our DistributionPools.
		return Pools.Select((pool, i) => pool.ToComponentPool(modsPerPool[i])).ToList();
	}

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
