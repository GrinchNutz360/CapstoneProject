using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialDirector : MonoBehaviour
{
    [Serializable] public class Line { public string key; public AudioClip clip; [TextArea] public string subtitle; }
    [Serializable] public class Feed { public string camId; public VideoClip idle; }

    [Header("Core")]
    public SpotNavigator nav;
    public VideoManager video;

    [Header("Dialogue")]
    public AudioSource voice;
    public TMP_Text subtitleText;
    public List<Line> lines = new List<Line>();

    [Header("UI (overlays need Raycast Target OFF)")]
    public CanvasGroup flashOverlay;     // white
    public CanvasGroup blackOverlay;     // black
    public GameObject flashPrompt, runPrompt, qtePanel, deathPanel, endCard;
    public Slider qteBar;
    public TMP_Text deathText;

    [Header("Cameras")]
    public string gateCamId = "Gate";
    public string packageCamId = "Front";
    public List<Feed> feeds = new List<Feed>();   // idle loops (include GateIdle)

    [Header("Clips")]
    public VideoClip gateBreakingIn, gateCaught, gateIdle;
    public VideoClip studentScare;
    public VideoClip packageInterrupted, packageCaught, packageRob, killerTurnLeft, killerGrab;
    public VideoClip runUpStairs, dontRunDeath, keepingDoorClosed;
    public VideoClip walkBackToDesk, sitAtDesk;
    

    [Header("Tuning")]
    public float packageFlashWindow = 8f;
    public float runWindow = 4f;
    public float qtePerPress = 0.07f, qteDecay = 0.22f, qteGrace = 1f;
    public int totalChallenges = 3;

    string currentCam, forcedCam;
    VideoClip forcedClip;
    int challengesDone;

    bool flashWindowOpen, flashed; string flashCam;
    bool runWindowOpen, ran;
    bool qteActive; float qteMeter;


    // ---------- hooks other scripts call ----------
    public void NotifyCameraOpened(string camId) { currentCam = camId; PlayFeed(camId); }
    public void NotifyCameraClosed() => currentCam = null;
    public void NotifyChallengeDone() => challengesDone++;
    public void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    bool InComputerArea()
    {
        string id = nav.CurrentId;
        return id == "Computer" || (id != null && id.StartsWith("Cam"));
    }
    void Start() => StartCoroutine(RunTutorial());

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (flashWindowOpen && currentCam == flashCam) flashed = true;
            if (runWindowOpen) ran = true;
            if (qteActive) qteMeter += qtePerPress;
        }
    }

    bool dead;

    // ---------- the story ----------
    IEnumerator RunTutorial()
    {
        yield return null;                                   // let the Navigator finish Start
        int s = PlayerPrefs.GetInt("debugStage", 1);
        ApplyStartState(s);

        if (s <= 1) { yield return Stage1_Call(); if (dead) yield break; }
        if (s <= 2) { yield return Stage2_Gate(); if (dead) yield break; }
        if (s <= 3) { yield return Stage3_Package(); if (dead) yield break; }
        if (s <= 4) { yield return Stage4_Delivery(); if (dead) yield break; }
        if (s <= 5) { yield return Stage5_Wifi(); if (dead) yield break; }
        if (s <= 6) { yield return Stage6_RunDoor(); if (dead) yield break; }
        yield return Stage7_HideEnd();
    }

    void ApplyStartState(int s)
    {
        if (s >= 2) { nav.SetFlag("homeworkUnlocked"); nav.SetFlag("camerasUnlocked"); challengesDone = 1; }
        if (s >= 4) { nav.SetFlag("packageGrabbed"); }
        if (s >= 5) { nav.SetFlag("packageGrabbed", false); nav.SetFlag("packageDelivered"); nav.SetFlag("cardChecked"); }
        if (s >= 6) { nav.SetFlag("wifiFixed"); nav.PlayThenGoTo(null, "MiddleStairsKiller"); }
    }


    IEnumerator Stage1_Call()
    {
        // 1. Call + homework (player can roam the whole time)
        yield return new WaitForSeconds(1.5f);
        yield return Say("call_01");
        yield return Say("call_02");
        yield return Say("call_homework");
        nav.SetFlag("homeworkUnlocked");
        yield return new WaitUntil(() => challengesDone >= 1);
        yield return Say("homework_great");
    }

    IEnumerator Stage2_Gate()
    { // 2. Gate
        nav.SetFlag("camerasUnlocked");
        forcedCam = gateCamId; forcedClip = gateBreakingIn;
        yield return Say("check_camera");
        yield return new WaitUntil(() => currentCam == gateCamId);
        yield return Say("gate_flash_him");
        nav.Freeze();
        yield return WaitForFlash(gateCamId, 0f, "gate_reminder", null);
        yield return FlashThenPlay(gateCaught);
        forcedCam = null;
        video.Play(gateIdle, true);
        nav.Unfreeze();
        yield return Say("gate_exit_computer");
        yield return WaitForExitComputer();
    }

    IEnumerator Stage3_Package()
    {  // 3. Student
        nav.Freeze();
        yield return PlayClip(studentScare);
        yield return Say("student_package");
        nav.PlayThenGoTo(null, "Desk");
        nav.Unfreeze();

        // 3b. Package
        forcedCam = packageCamId; forcedClip = packageInterrupted;
        yield return Say("check_packages");
        yield return new WaitUntil(() => currentCam == packageCamId);
        yield return Say("package_flash");
        nav.Freeze();
        bool ok = false;
        yield return WaitForFlash(packageCamId, packageFlashWindow, "package_reminder", r => ok = r);

        if (ok)
        {
            yield return FlashThenPlay(packageCaught);
            forcedCam = null;
            nav.SetFlag("packageAtDoor");
            nav.Unfreeze();
            yield return Say("package_great");
            yield return WaitFlag("packageGrabbed", "grab_hint", 15f);
            nav.SetFlag("packageAtDoor", false);
        }
        else
        {
            yield return PlayClip(packageRob);        // can't be stopped now
            forcedCam = null;
            nav.SetFlag("killerAtDoor");
            nav.Unfreeze();
            float waited = 0f;
            while (nav.CurrentId != "DeskKiller" && waited < 3f) { waited += Time.deltaTime; yield return null; }
            nav.Freeze();
            if (nav.CurrentId != "DeskKiller") nav.PlayThenGoTo(null, "DeskKiller");
            yield return Say("killer_grabbed");       // "should I grab it?"
            yield return PlayClip(killerTurnLeft);
            yield return Say("killer_rude");          // "it would be rude not to"
            yield return Die(killerGrab, "death_package");
            dead = true;
            yield break;
        }
    }

    IEnumerator Stage4_Delivery()
    { // 4. delivery
        yield return WaitForSpot("Desk");
        yield return Say("package_where");       // "Where does this go? Check the sticker (Tab)"
        yield return WaitFlag("cardChecked", "card_hint", 15f);
        yield return Say("package_elevator");    // "11th floor, take the elevator"
        yield return WaitFlag("packageDelivered", "deliver_hint", 25f);
        yield return WaitForSpot("ElevatorFloor11");
        yield return Say("delivery_paid");
    }

    IEnumerator Stage5_Wifi()
    { // 5. Wifi
        yield return WaitForSpot("Computer");
        yield return Say("wifi_out");
        nav.SetFlag("wifiBroken");
        yield return WaitForSpot("WifiDoor");
        yield return Say("wifi_how");                 // friend explains the minigame
        yield return new WaitUntil(() => nav.HasFlag("wifiFixed"));
        yield return Say("wifi_fixed");
        yield return WaitForSpot("MiddleStairsKiller");     // walked back, killer is waiting
    }

    IEnumerator Stage6_RunDoor()
    {  // 6. Run
        Speak("run_now");
        runPrompt.SetActive(true);
        float t = 0f;
        while (!nav.HasFlag("ranUp") && t < runWindow) { t += Time.deltaTime; yield return null; }
        runPrompt.SetActive(false);

        if (!nav.HasFlag("ranUp")) { yield return Die(dontRunDeath, "death_stairs"); dead = true;  yield break; }


        yield return WaitForSpot("DoorHold");   // RunUpStairs finished, door loop is playing
        nav.Freeze();
        bool held = false;
        yield return DoorQte(r => held = r);
        if (!held) { yield return Die(null, "death_door"); dead = true; yield break; }
    }

    IEnumerator Stage7_HideEnd()
    { // 7. Back to the desk, hide tutorial, finish homework
        yield return PlayClip(walkBackToDesk);
        yield return PlayClip(sitAtDesk);
        nav.PlayThenGoTo(null, "Desk");
        nav.Unfreeze();
        yield return Say("nearly_died");
        yield return Say("hide_ask");
        yield return WaitForSpot("UnderDesk");
        yield return Say("hide_tutorial");
        yield return WaitForSpot("Desk");
        yield return Say("finish_homework");
        yield return new WaitUntil(() => challengesDone >= totalChallenges);
        yield return Say("night_over");
        yield return Fade(blackOverlay, 1f, 1.5f);
        endCard.SetActive(true);
    }

    // ---------- helpers ----------
    void PlayFeed(string camId)
    {
        if (camId == forcedCam && forcedClip != null) { video.Play(forcedClip, true); return; }
        var f = feeds.Find(x => x.camId == camId);
        if (f != null && f.idle != null) video.Play(f.idle, true);
    }

    Line Find(string key) => lines.Find(l => l.key == key);
    public void Speak(string key) => StartCoroutine(Say(key));

    IEnumerator Say(string key)
    {
        var l = Find(key);
        if (l == null) { Debug.LogWarning("Missing line: " + key); yield break; }
        subtitleText.text = l.subtitle;
        float len = 2f + l.subtitle.Length * 0.04f;
        if (l.clip != null) { voice.PlayOneShot(l.clip); len = l.clip.length + 0.3f; }
        yield return new WaitForSeconds(len);
        subtitleText.text = "";
    }

    IEnumerator PlayClip(VideoClip clip)
    {
        bool done = false;
        video.Play(clip, false, () => done = true);
        while (!done) yield return null;
    }

    IEnumerator Fade(CanvasGroup g, float to, float time)
    {
        float from = g.alpha, t = 0f;
        while (t < time) { t += Time.deltaTime; g.alpha = Mathf.Lerp(from, to, t / time); yield return null; }
        g.alpha = to;
    }

    IEnumerator FlashThenPlay(VideoClip clip)
    {
        yield return Fade(flashOverlay, 1f, 0.08f);
        bool done = false;
        video.Play(clip, false, () => done = true);
        yield return new WaitForSeconds(0.35f);       // keep white until the swap happens
        yield return Fade(flashOverlay, 0f, 0.3f);
        while (!done) yield return null;
    }

    IEnumerator WaitForFlash(string camId, float timeout, string reminderKey, Action<bool> result)
    {
        flashed = false; flashCam = camId; flashWindowOpen = true; flashPrompt.SetActive(true);
        float t = 0f, nextReminder = 6f;
        while (!flashed && (timeout <= 0f || t < timeout))
        {
            t += Time.deltaTime;
            if (t > nextReminder) { nextReminder += 8f; Speak(reminderKey); }
            yield return null;
        }
        flashWindowOpen = false; flashPrompt.SetActive(false);
        result?.Invoke(flashed);
    }

    IEnumerator WaitForSpot(string id) { while (nav.CurrentId != id) yield return null; }

    IEnumerator WaitForExitComputer()
    {
        while (!InComputerArea()) yield return null;
        while (InComputerArea()) yield return null;
    }

    IEnumerator WaitFlag(string flag, string hintKey, float every)
    {
        float t = 0f;
        while (!nav.HasFlag(flag))
        {
            t += Time.deltaTime;
            if (t >= every) { t = 0f; Speak(hintKey); }
            yield return null;
        }
    }

    IEnumerator DoorQte(Action<bool> result)
    {
        qtePanel.SetActive(true);
        qteMeter = 0.4f; qteActive = true;
        float grace = qteGrace;
        while (true)
        {
            if (grace > 0f) grace -= Time.deltaTime; else qteMeter -= qteDecay * Time.deltaTime;
            qteBar.value = qteMeter;
            if (qteMeter >= 1f) { result(true); break; }
            if (qteMeter <= 0f) { result(false); break; }
            yield return null;
        }
        qteActive = false; qtePanel.SetActive(false);
    }

    IEnumerator Die(VideoClip clip, string key)
    {
        nav.Freeze();
        if (clip != null) yield return PlayClip(clip);
        yield return Fade(blackOverlay, 1f, 0.15f);
        var l = Find(key);
        deathText.text = l != null ? l.subtitle : "You died.";
        deathPanel.SetActive(true);
    }
}