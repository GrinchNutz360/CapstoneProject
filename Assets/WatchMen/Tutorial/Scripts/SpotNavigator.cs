using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

public class SpotNavigator : MonoBehaviour
{
    [Serializable]
    public class Exit
    {
        public string label;           // just for you in the Inspector
        public Button button;          // invisible click zone
        public VideoClip transition;   // empty = instant cut
        public string toSpot;
        public string requiresFlag;    // optional, e.g. "camerasUnlocked"
        public UnityEvent onBlocked;   // plays the "not now" line
        public string overrideFlag;            // if this flag is set, use the override below
        public VideoClip overrideTransition;   // leave empty for an instant cut
        public string overrideToSpot;
        public UnityEvent onUsed;
        public bool overrideFade;
    }

    [Serializable]
    public class Spot
    {
        public string id;
        public VideoClip idleLoop;     // optional
        public Texture2D idleStill;    // optional
        public UnityEvent onEnter;
        public UnityEvent onLeave;
        public List<Exit> exits = new List<Exit>();
    }

    public VideoManager video;
    public string startSpot = "Desk";
    public List<Spot> spots = new List<Spot>();
    public CanvasGroup blackOverlay;

    public string CurrentId => current != null ? current.id : null;
    public void RaiseFlag(string flag) => SetFlag(flag, true);   // usable from Inspector events

    readonly Dictionary<string, Spot> lookup = new Dictionary<string, Spot>();
    readonly HashSet<string> flags = new HashSet<string>();
    Spot current;
    bool busy;      // a transition is playing
    bool frozen;    // an event has taken over

    void Awake()
    {
        foreach (var spot in spots)
        {
            lookup[spot.id] = spot;
            foreach (var exit in spot.exits)
            {
                var e = exit; // capture for the lambda
                if (e.button != null)
                    e.button.onClick.AddListener(() => OnExitClicked(e));
            }
        }
        HideAllButtons();
    }

    void Start() => EnterSpot(startSpot);

    // ---- called by the Director and event scripts ----
    public void SetFlag(string flag, bool on = true)
    {
        if (on) flags.Add(flag); else flags.Remove(flag);
    }

    public bool HasFlag(string flag) => flags.Contains(flag);

    public void Freeze() { frozen = true; HideAllButtons(); }
    public void Unfreeze() { frozen = false; if (current != null && !busy) ShowButtons(current); }

    public void PlayThenGoTo(VideoClip clip, string spotId)
    {
        busy = true;
        HideAllButtons();
        current?.onLeave.Invoke();
        if (clip != null) video.Play(clip, false, () => EnterSpot(spotId));
        else EnterSpot(spotId);
    }

    // ---- internals ----
    // Replace OnExitClicked with:
    void OnExitClicked(Exit exit)
    {
        if (busy || frozen) return;

        if (!string.IsNullOrEmpty(exit.requiresFlag) && !flags.Contains(exit.requiresFlag))
        {
            exit.onBlocked?.Invoke();
            return;
        }

        var clip = exit.transition;
        var dest = exit.toSpot;
        bool fade = false;
        if (!string.IsNullOrEmpty(exit.overrideFlag) && flags.Contains(exit.overrideFlag))
        {
            clip = exit.overrideTransition;
            dest = exit.overrideToSpot;
            fade = exit.overrideFade;
        }

        exit.onUsed?.Invoke();
        if (fade && blackOverlay != null) StartCoroutine(FadeGo(dest));
        else PlayThenGoTo(clip, dest);
    }

    IEnumerator FadeGo(string spotId)
    {
        busy = true;
        HideAllButtons();
        current?.onLeave.Invoke();
        yield return FadeTo(1f, 0.25f);
        EnterSpot(spotId);
        yield return FadeTo(0f, 0.4f);
    }

    IEnumerator FadeTo(float target, float time)
    {
        float start = blackOverlay.alpha, t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            blackOverlay.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        blackOverlay.alpha = target;
    }

    void EnterSpot(string id)
    {
        if (!lookup.TryGetValue(id, out var spot))
        {
            Debug.LogError("No spot named " + id);
            return;
        }
        current = spot;

        if (spot.idleLoop != null) video.Play(spot.idleLoop, true);
        else if (spot.idleStill != null) video.ShowStill(spot.idleStill);
        // else: the last frame of the transition is already on screen

        busy = false;
        if (!frozen) ShowButtons(spot);
        spot.onEnter.Invoke();
    }

    void ShowButtons(Spot spot)
    {
        HideAllButtons();
        foreach (var e in spot.exits)
            if (e.button != null) e.button.gameObject.SetActive(true);
    }

    void HideAllButtons()
    {
        foreach (var s in spots)
            foreach (var e in s.exits)
                if (e.button != null) e.button.gameObject.SetActive(false);
    }
}