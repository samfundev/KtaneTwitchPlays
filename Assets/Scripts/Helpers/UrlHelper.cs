using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class UrlHelper : MonoBehaviour
{
    private bool shortMode = false;

	public void ChangeMode(bool toShort)
	{
        shortMode = toShort;
	}

    public bool ToggleMode()
    {
        shortMode = !shortMode;
        return shortMode;
    }

    private string Escape(string toEscape)
    {
        return Regex.Replace(toEscape, @"[^\w%]", m => "%" + ((int)m.Value[0]).ToString("X2"));
    }

    public string LogAnalyser
    {
        get
        {
            return "https://ktane.timwi.de/More/Logfile%20Analyzer.html";
        }
    }

    public string LogAnalyserFor(string url)
    {
        return string.Format(LogAnalyser + "#url={0}", url);
    }

    public string Reference(bool modded)
    {
        return (modded) ? ModdedReference : VanillaReference;
    }

    public string CommandReference
    {
        get
        {
            return (shortMode) ? "http://bombch.us/CeEz" : "https://github.com/ashbash1987/ktanemod-twitchplays/wiki/Pre-Game-Commands";
        }
    }

    public string VanillaReference
    {
        get
        {
            return (shortMode) ? "http://bombch.us/CdqJ" : "https://github.com/ashbash1987/ktanemod-twitchplays/wiki/Vanilla-Module-Commands"; // todo send this to the right short url
        }
    }

    public string ModdedReference
    {
        get
        {
            return (shortMode) ? "http://bombch.us/CdqJ" : "https://github.com/ashbash1987/ktanemod-twitchplays/wiki/Mod-Module-Commands";
        }
    }

    public string ManualFor(string module, string type = "html")
    {
        return string.Format(Repository + "{0}/{1}.{2}", type.ToUpper(), Escape(module), type);
    }

    public string VanillaManual = "http://www.bombmanual.com/";
    public string Repository = "https://ktane.timwi.de/";
}
