using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class LondonUndergroundComponentSolver : ComponentSolver
{
	public LondonUndergroundComponentSolver(BombCommander bombCommander,  BombComponent bombComponent) :
		base (bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_submitButton = (KMSelectable)_submitButtonField.GetValue(_component);
		_changeLine1 = (KMSelectable)_changeLine1Field.GetValue(_component);
		_changeStation1 = (KMSelectable)_changeStation1Field.GetValue(_component);
		_changeLine2 = (KMSelectable)_changeLine2Field.GetValue(_component);
		_changeStation2 = (KMSelectable)_changeStation2Field.GetValue(_component);
		_changeLine3 = (KMSelectable)_changeLine3Field.GetValue(_component);
		_changeStation3 = (KMSelectable)_changeStation3Field.GetValue(_component);
		_lineOptions = (string[])_lineOptionsField.GetValue(_component);
		_bakerlooStations = (string[])_bakerlooStationsField.GetValue(_component);
		_centralStations = (string[])_centralStationsField.GetValue(_component);
		_circleStations = (string[])_circleStationsField.GetValue(_component);
		_districtStations = (string[])_districtStationsField.GetValue(_component);
		_hammersmithStations = (string[])_hammersmithStationsField.GetValue(_component);
		_jubileeStations = (string[])_jubileeStationsField.GetValue(_component);
		_metropolitanStations = (string[])_metropolitanStationsField.GetValue(_component);
		_northernStations = (string[])_northernStationsField.GetValue(_component);
		_piccadillyStations = (string[])_piccadillyStationsField.GetValue(_component);
		_victoriaStations = (string[])_victoriaStationsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit a line and station for the top row with !{0} top circle embankment. Use Hammersmith for the Hammersmith & City line.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Replace("’", "'");
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (commands.Length < 3 && commands[0] != "submit")
			yield break;
		else if (commands[0] == "top")
		{
			var station = inputCommand.ToLowerInvariant().Substring(commands[0].Length + commands[1].Length + 1);
			yield return null;
			bool lineCorrect = false;
			int iteration = 0;
			while (!(lineCorrect || iteration == 11))
			{
				yield return DoInteractionClick(_changeLine1);
				yield return null;
				line1LineIndex = (line1LineIndex + 1) % 11;
				iteration++;
				if ((_lineOptions[line1LineIndex].ToLowerInvariant() == commands[1]) || (commands[1] == "hammersmith" && _lineOptions[line1LineIndex].ToLowerInvariant() == "hammersmith & city"))
					lineCorrect = true;
			}
			if (!lineCorrect)
			{
				yield return string.Format("sendtochaterror The specified line {0}, does not exist.", commands[1]);
				yield break;
			} else
			{
				switch (commands[1])
				{
					case "bakerloo":
						yield return null;
						bool bakerlooStationCorrect = false;
						iteration = 0;
						while (!(bakerlooStationCorrect || iteration == _bakerlooStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								bakerlooStationCorrect = true;
						}
						if (!bakerlooStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "central":
						yield return null;
						bool centralStationCorrect = false;
						iteration = 0;
						while (!(centralStationCorrect || iteration == _centralStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								centralStationCorrect = true;
						}
						if (!centralStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "circle":
						yield return null;
						bool circleStationCorrect = false;
						iteration = 0;
						while (!(circleStationCorrect || iteration == _circleStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								circleStationCorrect = true;
						}
						if (!circleStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "district":
						yield return null;
						bool districtStationCorrect = false;
						iteration = 0;
						while (!(districtStationCorrect || iteration == _districtStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								districtStationCorrect = true;
						}
						if (!districtStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "hammersmith":
						yield return null;
						bool hammersmithStationCorrect = false;
						iteration = 0;
						while (!(hammersmithStationCorrect || iteration == _hammersmithStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								hammersmithStationCorrect = true;
						}
						if (!hammersmithStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "jubilee":
						yield return null;
						bool jubileeStationCorrect = false;
						iteration = 0;
						while (!(jubileeStationCorrect || iteration == _jubileeStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								jubileeStationCorrect = true;
						}
						if (!jubileeStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "metropolitan":
						yield return null;
						bool metropolitanStationCorrect = false;
						iteration = 0;
						while (!(metropolitanStationCorrect || iteration == _metropolitanStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								metropolitanStationCorrect = true;
						}
						if (!metropolitanStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "northern":
						yield return null;
						bool northernStationCorrect = false;
						iteration = 0;
						while (!(northernStationCorrect || iteration == _northernStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								northernStationCorrect = true;
						}
						if (!northernStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "piccadilly":
						yield return null;
						bool piccadillyStationCorrect = false;
						iteration = 0;
						while (!(piccadillyStationCorrect || iteration == _piccadillyStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								piccadillyStationCorrect = true;
						}
						if (!piccadillyStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "victoria":
						yield return null;
						bool victoriaStationCorrect = false;
						iteration = 0;
						while (!(victoriaStationCorrect || iteration == _victoriaStations.Length))
						{
							yield return DoInteractionClick(_changeStation1);
							yield return null;
							TextMesh _line1Station = (TextMesh)_line1StationField.GetValue(_component);
							iteration++;
							if (_line1Station.text.ToLowerInvariant().Trim() == station.Trim())
								victoriaStationCorrect = true;
						}
						if (!victoriaStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
				}
			}
		}
		else if (commands[0] == "middle")
		{
			var station = inputCommand.ToLowerInvariant().Substring(commands[0].Length + commands[1].Length + 1);
			yield return null;
			bool lineCorrect = false;
			int iteration = 0;
			while (!(lineCorrect || iteration == 11))
			{
				yield return DoInteractionClick(_changeLine2);
				yield return null;
				line2LineIndex = (line2LineIndex + 1) % 11;
				iteration++;
				if ((_lineOptions[line2LineIndex].ToLowerInvariant() == commands[1]) || (commands[1] == "hammersmith" && _lineOptions[line2LineIndex].ToLowerInvariant() == "hammersmith & city"))
					lineCorrect = true;
			}
			if (!lineCorrect)
			{
				yield return string.Format("sendtochaterror The specified line {0}, does not exist.", commands[1]);
				yield break;
			}
			else
			{
				switch (commands[1])
				{
					case "bakerloo":
						yield return null;
						bool bakerlooStationCorrect = false;
						iteration = 0;
						while (!(bakerlooStationCorrect || iteration == _bakerlooStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								bakerlooStationCorrect = true;
						}
						if (!bakerlooStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "central":
						yield return null;
						bool centralStationCorrect = false;
						iteration = 0;
						while (!(centralStationCorrect || iteration == _centralStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								centralStationCorrect = true;
						}
						if (!centralStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "circle":
						yield return null;
						bool circleStationCorrect = false;
						iteration = 0;
						while (!(circleStationCorrect || iteration == _circleStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								circleStationCorrect = true;
						}
						if (!circleStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "district":
						yield return null;
						bool districtStationCorrect = false;
						iteration = 0;
						while (!(districtStationCorrect || iteration == _districtStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								districtStationCorrect = true;
						}
						if (!districtStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "hammersmith":
						yield return null;
						bool hammersmithStationCorrect = false;
						iteration = 0;
						while (!(hammersmithStationCorrect || iteration == _hammersmithStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								hammersmithStationCorrect = true;
						}
						if (!hammersmithStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "jubilee":
						yield return null;
						bool jubileeStationCorrect = false;
						iteration = 0;
						while (!(jubileeStationCorrect || iteration == _jubileeStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								jubileeStationCorrect = true;
						}
						if (!jubileeStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "metropolitan":
						yield return null;
						bool metropolitanStationCorrect = false;
						iteration = 0;
						while (!(metropolitanStationCorrect || iteration == _metropolitanStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								metropolitanStationCorrect = true;
						}
						if (!metropolitanStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "northern":
						yield return null;
						bool northernStationCorrect = false;
						iteration = 0;
						while (!(northernStationCorrect || iteration == _northernStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								northernStationCorrect = true;
						}
						if (!northernStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "piccadilly":
						yield return null;
						bool piccadillyStationCorrect = false;
						iteration = 0;
						while (!(piccadillyStationCorrect || iteration == _piccadillyStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								piccadillyStationCorrect = true;
						}
						if (!piccadillyStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "victoria":
						yield return null;
						bool victoriaStationCorrect = false;
						iteration = 0;
						while (!(victoriaStationCorrect || iteration == _victoriaStations.Length))
						{
							yield return DoInteractionClick(_changeStation2);
							yield return null;
							TextMesh _line2Station = (TextMesh)_line2StationField.GetValue(_component);
							iteration++;
							if (_line2Station.text.ToLowerInvariant().Trim() == station.Trim())
								victoriaStationCorrect = true;
						}
						if (!victoriaStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
				}
			}
		}
		else if (commands[0] == "bottom")
		{
			var station = inputCommand.ToLowerInvariant().Substring(commands[0].Length + commands[1].Length + 1);
			yield return null;
			bool lineCorrect = false;
			int iteration = 0;
			while (!(lineCorrect || iteration == 11))
			{
				yield return DoInteractionClick(_changeLine3);
				yield return null;
				line3LineIndex = (line3LineIndex + 1) % 11;
				iteration++;
				if ((_lineOptions[line3LineIndex].ToLowerInvariant() == commands[1]) || (commands[1] == "hammersmith" && _lineOptions[line3LineIndex].ToLowerInvariant() == "hammersmith & city"))
					lineCorrect = true;
			}
			if (!lineCorrect)
			{
				yield return string.Format("sendtochaterror The specified line {0}, does not exist.", commands[1]);
				yield break;
			}
			else
			{
				switch (commands[1])
				{
					case "bakerloo":
						yield return null;
						bool bakerlooStationCorrect = false;
						iteration = 0;
						while (!(bakerlooStationCorrect || iteration == _bakerlooStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								bakerlooStationCorrect = true;
						}
						if (!bakerlooStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "central":
						yield return null;
						bool centralStationCorrect = false;
						iteration = 0;
						while (!(centralStationCorrect || iteration == _centralStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								centralStationCorrect = true;
						}
						if (!centralStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "circle":
						yield return null;
						bool circleStationCorrect = false;
						iteration = 0;
						while (!(circleStationCorrect || iteration == _circleStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								circleStationCorrect = true;
						}
						if (!circleStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "district":
						yield return null;
						bool districtStationCorrect = false;
						iteration = 0;
						while (!(districtStationCorrect || iteration == _districtStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								districtStationCorrect = true;
						}
						if (!districtStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "hammersmith":
						yield return null;
						bool hammersmithStationCorrect = false;
						iteration = 0;
						while (!(hammersmithStationCorrect || iteration == _hammersmithStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								hammersmithStationCorrect = true;
						}
						if (!hammersmithStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "jubilee":
						yield return null;
						bool jubileeStationCorrect = false;
						iteration = 0;
						while (!(jubileeStationCorrect || iteration == _jubileeStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								jubileeStationCorrect = true;
						}
						if (!jubileeStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "metropolitan":
						yield return null;
						bool metropolitanStationCorrect = false;
						iteration = 0;
						while (!(metropolitanStationCorrect || iteration == _metropolitanStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								metropolitanStationCorrect = true;
						}
						if (!metropolitanStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "northern":
						yield return null;
						bool northernStationCorrect = false;
						iteration = 0;
						while (!(northernStationCorrect || iteration == _northernStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								northernStationCorrect = true;
						}
						if (!northernStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "piccadilly":
						yield return null;
						bool piccadillyStationCorrect = false;
						iteration = 0;
						while (!(piccadillyStationCorrect || iteration == _piccadillyStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								piccadillyStationCorrect = true;
						}
						if (!piccadillyStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
					case "victoria":
						yield return null;
						bool victoriaStationCorrect = false;
						iteration = 0;
						while (!(victoriaStationCorrect || iteration == _victoriaStations.Length))
						{
							yield return DoInteractionClick(_changeStation3);
							yield return null;
							TextMesh _line3Station = (TextMesh)_line3StationField.GetValue(_component);
							iteration++;
							if (_line3Station.text.ToLowerInvariant().Trim() == station.Trim())
								victoriaStationCorrect = true;
						}
						if (!victoriaStationCorrect)
						{
							yield return string.Format("sendtochaterror The specified station, {0}, does not exist.", station);
							yield break;
						}
						break;
				}
			}
		} else if (commands[0] == "submit")
		{
			yield return null;
			yield return DoInteractionClick(_submitButton);
			line1LineIndex = 10;
			line2LineIndex = 10;
			line3LineIndex = 10;
		}
		else
		{
			yield break;
		}
	}

	static LondonUndergroundComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("londonUndergroundScript");
		_submitButtonField = _componentType.GetField("submitButton", BindingFlags.Public | BindingFlags.Instance);
		_changeLine1Field = _componentType.GetField("changeLine1", BindingFlags.Public | BindingFlags.Instance);
		_changeStation1Field = _componentType.GetField("changeStation1", BindingFlags.Public | BindingFlags.Instance);
		_changeLine2Field = _componentType.GetField("changeLine2", BindingFlags.Public | BindingFlags.Instance);
		_changeStation2Field = _componentType.GetField("changeStation2", BindingFlags.Public | BindingFlags.Instance);
		_changeLine3Field = _componentType.GetField("changeLine3", BindingFlags.Public | BindingFlags.Instance);
		_changeStation3Field = _componentType.GetField("changeStation3", BindingFlags.Public | BindingFlags.Instance);
		_lineOptionsField = _componentType.GetField("lineOptions", BindingFlags.Public | BindingFlags.Instance);
		_bakerlooStationsField = _componentType.GetField("bakerlooStations", BindingFlags.Public | BindingFlags.Instance);
		_centralStationsField = _componentType.GetField("centralStations", BindingFlags.Public | BindingFlags.Instance);
		_circleStationsField = _componentType.GetField("circleStations", BindingFlags.Public | BindingFlags.Instance);
		_districtStationsField = _componentType.GetField("districtStations", BindingFlags.Public | BindingFlags.Instance);
		_hammersmithStationsField = _componentType.GetField("hammersmithStations", BindingFlags.Public | BindingFlags.Instance);
		_jubileeStationsField = _componentType.GetField("jubileeStations", BindingFlags.Public | BindingFlags.Instance);
		_metropolitanStationsField = _componentType.GetField("metropolitanStations", BindingFlags.Public | BindingFlags.Instance);
		_northernStationsField = _componentType.GetField("northernStations", BindingFlags.Public | BindingFlags.Instance);
		_piccadillyStationsField = _componentType.GetField("piccadillyStations", BindingFlags.Public | BindingFlags.Instance);
		_victoriaStationsField = _componentType.GetField("victoriaStations", BindingFlags.Public | BindingFlags.Instance);
		_line1StationField = _componentType.GetField("line1Station", BindingFlags.Public | BindingFlags.Instance);
		_line2StationField = _componentType.GetField("line2Station", BindingFlags.Public | BindingFlags.Instance);
		_line3StationField = _componentType.GetField("line3Station", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _submitButtonField = null;
	private static FieldInfo _changeLine1Field = null;
	private static FieldInfo _changeStation1Field = null;
	private static FieldInfo _changeLine2Field = null;
	private static FieldInfo _changeStation2Field = null;
	private static FieldInfo _changeLine3Field = null;
	private static FieldInfo _changeStation3Field = null;
	private static FieldInfo _lineOptionsField = null;
	private static FieldInfo _bakerlooStationsField = null;
	private static FieldInfo _centralStationsField = null;
	private static FieldInfo _circleStationsField = null;
	private static FieldInfo _districtStationsField = null;
	private static FieldInfo _hammersmithStationsField = null;
	private static FieldInfo _jubileeStationsField = null;
	private static FieldInfo _metropolitanStationsField = null;
	private static FieldInfo _northernStationsField = null;
	private static FieldInfo _piccadillyStationsField = null;
	private static FieldInfo _victoriaStationsField = null;
	private static FieldInfo _line1StationField = null;
	private static FieldInfo _line2StationField = null;
	private static FieldInfo _line3StationField = null;

	private int line1LineIndex = 10;
	private int line2LineIndex = 10;
	private int line3LineIndex = 10;

	private readonly object _component = null;
	private readonly KMSelectable _submitButton = null;
	private readonly KMSelectable _changeLine1 = null;
	private readonly KMSelectable _changeStation1 = null;
	private readonly KMSelectable _changeLine2 = null;
	private readonly KMSelectable _changeStation2 = null;
	private readonly KMSelectable _changeLine3 = null;
	private readonly KMSelectable _changeStation3 = null;
	private readonly string[] _lineOptions = null;
	private readonly string[] _bakerlooStations = null;
	private readonly string[] _centralStations = null;
	private readonly string[] _circleStations = null;
	private readonly string[] _districtStations = null;
	private readonly string[] _hammersmithStations = null;
	private readonly string[] _jubileeStations = null;
	private readonly string[] _metropolitanStations = null;
	private readonly string[] _northernStations = null;
	private readonly string[] _piccadillyStations = null;
	private readonly string[] _victoriaStations = null;
}