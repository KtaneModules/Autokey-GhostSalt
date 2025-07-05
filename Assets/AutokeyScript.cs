using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using wawa.DDL;
using wawa.Optionals;

public class AutokeyScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    static int _autokeyNoCounter = 1;
    int _autokeyNo = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public Text TargetTimeRend;
    public Text CurrentTimeRend;
    public Text SolvedRend;
    public Image DarkeningBackground;
    public CanvasGroup CanvasGroup;
    public Morshu Morshu;

    private Color SolvedStartingColour;
    private float StartingTime;
    private float TargetTime;
    private bool IsSolved;
    private bool SoundPlayed;
    private bool ZenModeActive;

    private Settings _Settings;
    private KMAudio.KMAudioRef Sound;

    private float EarliestTimePercent = 75f;
    private float LatestTimePercent = 25f;
    private bool DisableSound = false;
    private bool IsActivated;

    private static readonly KeyCode[] TypableKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };

    private static readonly KeyCode[] MorshuSpelling = { KeyCode.M, KeyCode.O, KeyCode.R, KeyCode.S, KeyCode.H, KeyCode.U };
    private int MorshuIx;

    class Settings
    {
        public float EarliestTimePercent = 75f;
        public float LatestTimePercent = 25f;
        public bool DisableSound = false;
    }

    void GetSettings()
    {
        var SettingsConfig = new ModConfig<Settings>("Autokey");
        _Settings = SettingsConfig.Settings; // This reads the settings from the file, or creates a new file if it does not exist
        SettingsConfig.Settings = _Settings; // This writes any updates or fixes if there's an issue with the file
    }

    void SortOutMissionDescription()
    {
        var countedMentions = 0;
        try
        {
            string description = Application.isEditor ? "[Autokey 1] soundoff\n[Autokey 1] earliest 90\n[Autokey 1] latest 90" : Missions.Description.UnwrapOr("");
            var matches = Regex.Matches(description, @"^(?:///? ?)?\[Autokey ?([0-9]+)\] (.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Groups[1].Value == _autokeyNo.ToString())
                {
                    if (countedMentions == 0)
                        Debug.LogFormat("[Autokey #{0}] Oh! The mission description says something about me, lemme see.", _moduleID);
                    else
                        Debug.LogFormat("[Autokey #{0}] Ah, there's some more here, hold on.", _moduleID);

                    countedMentions++;

                    float result = 0;
                    var secondMatches = Regex.Match(matches[i].Groups[2].Value, @"^earliest (.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    var thirdMatches = Regex.Match(matches[i].Groups[2].Value, @"^latest (.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    if (float.TryParse(secondMatches.Groups[1].Value, out result))
                    {
                        try
                        {
                            EarliestTimePercent = result;
                            Debug.LogFormat("[Autokey #{0}] It's telling me to change the earliest time I can generate to {1}% of the bomb's time limit. Okie dokie, done that.", _moduleID, result);
                        }
                        catch
                        {
                            Debug.LogFormat("[Autokey #{0}] Mm, looks like gibberish, nevermind.", _moduleID);
                        }
                    }
                    else if (float.TryParse(thirdMatches.Groups[1].Value, out result))
                    {
                        try
                        {
                            LatestTimePercent = result;
                            Debug.LogFormat("[Autokey #{0}] It's telling me to change the latest time I can generate to {1}% of the bomb's time limit. Alrighty, made that change.", _moduleID, result);
                        }
                        catch
                        {
                            Debug.LogFormat("[Autokey #{0}] Mm, looks like gibberish, nevermind.", _moduleID);
                        }
                    }
                    else if (matches[i].Groups[2].Value.ToLowerInvariant() == "soundoff" || matches[i].Groups[1].Value.ToLowerInvariant() == "sound off")
                    {
                        try
                        {
                            DisableSound = true;
                            Debug.LogFormat("[Autokey #{0}] It's telling me to disable my sound effects. Coolio, that's done.", _moduleID, result);
                        }
                        catch
                        {
                            Debug.LogFormat("[Autokey #{0}] Mm, looks like gibberish, nevermind.", _moduleID);
                        }
                    }
                    else
                        Debug.LogFormat("[Autokey #{0}] Mm, looks like gibberish, nevermind.", _moduleID);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogFormat("[Autokey #{0}] Mm, an exception occurred while reading the mission description: {1}. Aborting.", _moduleID, e);
        }
    }

    private float GetCountdownTime()
    {
        return Mathf.Abs(Bomb.GetTime() - TargetTime);
    }

    public string StringifyTime(float time)
    {
        string formattedTime = "";
        if (time >= 3600)
        {
            formattedTime += ((int)time / 3600).ToString("00");
            formattedTime += ":";
        }
        formattedTime += ((int)((time % 3600) / 60)).ToString("00");
        formattedTime += ":";
        int s = (int)time % 60;
        formattedTime += s.ToString("00");
        return formattedTime;
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        _autokeyNoCounter = 1;

        CanvasGroup.alpha = 0;
        Module.OnActivate += delegate { IsActivated = true; CanvasGroup.alpha = 1; };

        SolvedStartingColour = SolvedRend.color;
        SolvedRend.color = Color.clear;
    }

    // Use this for initialization
    void Start()
    {
        _autokeyNo = _autokeyNoCounter++;

        GetSettings();

        EarliestTimePercent = _Settings.EarliestTimePercent;
        LatestTimePercent = _Settings.LatestTimePercent;

        Debug.LogFormat("[Autokey #{0}] I've taken a look through your settings, and you'd like the earliest time I can generate to be {1}% of the bomb's time limit, the latest {2}%, and for my sound effects to be {3}. Fine by me.", _moduleID, EarliestTimePercent, LatestTimePercent, DisableSound ? "disabled" : "enabled");

        SortOutMissionDescription();

        StartingTime = Bomb.GetTime();

        if (ZenModeActive)
            TargetTime = Mathf.Floor(Rnd.Range(2 * 60f, 10 * 60f));
        else
            TargetTime = Mathf.Floor(Rnd.Range(StartingTime * (LatestTimePercent / 100), StartingTime * (EarliestTimePercent / 100)));
        TargetTimeRend.text = StringifyTime(TargetTime);

        Debug.LogFormat("[Autokey #{0}] Alrighty! I've chosen that I will solve at {1}. See you then!", _moduleID, TargetTimeRend.text);
    }

    void Update()
    {
        if (!IsSolved && IsActivated)
        {
            CurrentTimeRend.text = StringifyTime(GetCountdownTime());

            Debug.Log(Bomb.GetTime());

            if (!SoundPlayed && (!ZenModeActive && Mathf.FloorToInt(Bomb.GetTime()) - 10 <= Mathf.FloorToInt(TargetTime)) || (ZenModeActive && Mathf.CeilToInt(Bomb.GetTime()) + 10 >= Mathf.CeilToInt(TargetTime)))
            {
                Audio.PlaySoundAtTransform("warning", transform);
                SoundPlayed = true;
            }

            if (SoundPlayed && (!ZenModeActive && Mathf.FloorToInt(Bomb.GetTime()) - 10 > Mathf.FloorToInt(TargetTime)) || (ZenModeActive && Mathf.CeilToInt(Bomb.GetTime()) + 10 < Mathf.CeilToInt(TargetTime)))
                SoundPlayed = false;

            if ((!ZenModeActive && Mathf.FloorToInt(Bomb.GetTime()) <= Mathf.FloorToInt(TargetTime)) || (ZenModeActive && Mathf.CeilToInt(Bomb.GetTime()) >= Mathf.CeilToInt(TargetTime)))
                Solve();
        }

        if (Input.GetKeyDown(MorshuSpelling[MorshuIx]))
        {
            MorshuIx++;
            if (MorshuIx == 6)
            {
                MorshuIx = 0;
                if (Sound != null)
                    Sound.StopSound();
                Sound = Audio.HandlePlaySoundAtTransformWithRef("funny", transform, false);
                Morshu.Play();
            }
        }
        else foreach (KeyCode key in TypableKeys) if (Input.GetKeyDown(key))
                {
                    MorshuIx = 0;
                    break;
                }
    }

    private void Solve()
    {
        Module.HandlePass();
        IsSolved = true;
        StartCoroutine(SolveAnim());
        Audio.PlaySoundAtTransform("solve", transform);
        Debug.LogFormat("[Autokey #{0}] There we go! That's me solved. Good luck with defusing the rest of the bomb!", _moduleID);
    }

    private IEnumerator SolveAnim(float duration = 0.2f, float outDuration = 1f)
    {
        var targetTimeRendInit = TargetTimeRend.transform.localPosition.y;
        var currentTimeRendInit = CurrentTimeRend.transform.localPosition.y;

        var targetTimeColour = TargetTimeRend.color;
        var currentTimeColour = CurrentTimeRend.color;

        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;

            TargetTimeRend.transform.localPosition = Vector3.up * (targetTimeRendInit + Easing.InSine(timer, 0, -10, duration));
            CurrentTimeRend.transform.localPosition = Vector3.up * (currentTimeRendInit + Easing.InSine(timer, 0, -10, duration));

            TargetTimeRend.color = targetTimeColour * Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer / duration);
            CurrentTimeRend.color = currentTimeColour * Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer / duration);
        }

        TargetTimeRend.color = CurrentTimeRend.color = Color.clear;

        timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;

            SolvedRend.transform.localPosition = Vector3.up * Easing.OutSine(timer, 10, 0, duration);
            SolvedRend.color = SolvedStartingColour * Color.Lerp(new Color(1, 1, 1, 0), Color.white, timer / duration);
        }

        SolvedRend.transform.localPosition = Vector3.zero;
        SolvedRend.color = SolvedStartingColour;

        yield return new WaitForSeconds(2f);

        var darkeningBackgroundColour = DarkeningBackground.color;

        timer = 0;
        while (timer < outDuration)
        {
            yield return null;
            timer += Time.deltaTime;

            SolvedRend.color = SolvedStartingColour * Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer / outDuration);
            DarkeningBackground.color = darkeningBackgroundColour * Color.Lerp(Color.white, new Color(1, 1, 1, 0), timer / outDuration);
        }

        SolvedRend.color = DarkeningBackground.color = Color.clear;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} funny' to do something funny.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.ToLowerInvariant() != "funny")
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        yield return null;
        if (Sound != null)
            Sound.StopSound();
        Sound = Audio.HandlePlaySoundAtTransformWithRef("funny", transform, false);
        Morshu.Play();
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        Solve();
    }
}