using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WifiTuner : MonoBehaviour
{
    public WifiPanel wifiPanel;          // the script from earlier
    public Slider dial;                  // the interactive slider, Min 0, Max 1
    public Slider signalBar;             // a second slider, Interactable OFF, shows signal strength
    public TMP_Text statusText;          // "Searching...", "Weak signal", etc.

    [Header("Audio")]
    public AudioSource staticLoop;       // looping static, Play On Awake off
    public AudioSource lockTone;         // optional looping tone for a strong signal

    [Header("Tuning")]
    public float tolerance = 0.04f;      // how close counts as "locked"
    public float falloff = 0.35f;        // how far away you still get some signal
    public float holdTime = 1.5f;        // seconds to hold it steady
    public float minTarget = 0.15f, maxTarget = 0.85f;

    float target, lockTimer;
    bool done;

    void OnEnable()
    {
        target = Random.Range(minTarget, maxTarget);
        // start the dial far from the target so it can't be solved instantly
        dial.value = target < 0.5f ? 0.95f : 0.05f;
        lockTimer = 0f;
        done = false;
        if (staticLoop) staticLoop.Play();
        if (lockTone) lockTone.Play();
        Refresh();
    }

    void OnDisable()
    {
        if (staticLoop) staticLoop.Stop();
        if (lockTone) lockTone.Stop();
    }

    void Update()
    {
        if (done) return;

        float dist = Mathf.Abs(dial.value - target);
        float signal = 1f - Mathf.Clamp01(dist / falloff);   // 0 = far, 1 = perfect

        if (staticLoop) staticLoop.volume = 1f - signal;
        if (lockTone) lockTone.volume = signal * signal;     // only gets loud when close
        signalBar.value = signal;

        if (dist <= tolerance) lockTimer += Time.deltaTime;
        else lockTimer = Mathf.Max(0f, lockTimer - Time.deltaTime * 2f);

        if (lockTimer >= holdTime)
        {
            done = true;
            statusText.text = "CONNECTED";
            Invoke(nameof(Finish), 0.8f);
            return;
        }

        Refresh(signal);
    }

    void Refresh(float signal = 0f)
    {
        if (done) return;
        if (signal > 0.85f) statusText.text = "Hold steady...";
        else if (signal > 0.4f) statusText.text = "Weak signal";
        else statusText.text = "Searching...";
    }

    void Finish() => wifiPanel.Complete();
}