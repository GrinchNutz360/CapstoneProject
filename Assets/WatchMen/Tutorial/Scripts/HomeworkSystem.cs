using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class HomeworkSystem : MonoBehaviour
{
    public SpotNavigator nav;
    public TutorialDirector director;

    [Header("UI")]
    public GameObject homeworkPanel;
    public TMP_Text titleText, codeText, statusText;

    [Header("Audio (all optional)")]
    public AudioSource sfx;
    public AudioClip keyClip, wrongClip, doneClip;

    public UnityEvent onLocked;        // the "not yet" voice line

    [TextArea(2, 6)]
    public string[] challenges;        // one entry per assignment

    public float nextDelay = 1.5f;   // add this field, and delete the old closeDelay

    int challengeIndex, typed, mistakes;
    bool isOpen, finishing;

    void Start() => homeworkPanel.SetActive(false);

    // The homework icon on the desktop calls this
    public void Open()
    {
        if (!nav.HasFlag("homeworkUnlocked")) { onLocked.Invoke(); return; }
        isOpen = true;
        finishing = false;
        homeworkPanel.SetActive(true);
        Refresh();
    }

    // The panel's back button calls this. Also call it from the Computer spot's On Leave.
    public void Close()
    {
        StopAllCoroutines();
        isOpen = false;
        finishing = false;
        homeworkPanel.SetActive(false);
    }

    void Update()
    {
        if (!isOpen || finishing || challengeIndex >= challenges.Length) return;

        string target = Target();

        foreach (char raw in Input.inputString)
        {
            SkipIndent(target);
            char c = raw == '\r' ? '\n' : raw;
            if (c == '\b') continue;

            if (c == target[typed])
            {
                typed++;
                if (sfx && keyClip) sfx.PlayOneShot(keyClip);
                if (typed >= target.Length) { StartCoroutine(Finish()); break; }
            }
            else
            {
                mistakes++;
                if (sfx && wrongClip) sfx.PlayOneShot(wrongClip);
            }
        }

        if (!finishing) Refresh();
    }

    IEnumerator Finish()
    {
        finishing = true;
        if (sfx && doneClip) sfx.PlayOneShot(doneClip);
        codeText.text = "Assignment submitted.";
        statusText.text = "COMPLETE";

        challengeIndex++;
        typed = 0;
        mistakes = 0;                    // optional: fresh mistake count per assignment
        director.NotifyChallengeDone();

        yield return new WaitForSeconds(nextDelay);

        finishing = false;
        Refresh();                       // shows the next assignment, or "All assignments submitted."
    }

    string Target() => challenges[challengeIndex].Replace("\r", "").TrimEnd();

    // Leading indentation is skipped, so players only type the actual code
    void SkipIndent(string t)
    {
        while (typed < t.Length && t[typed] == ' ' && AtLineStart(t, typed)) typed++;
    }

    bool AtLineStart(string t, int i)
    {
        int j = i - 1;
        while (j >= 0 && t[j] == ' ') j--;
        return j < 0 || t[j] == '\n';
    }

    void Refresh()
    {
        titleText.text = "HOMEWORK " + Mathf.Min(challengeIndex + 1, challenges.Length) + "/" + challenges.Length;

        if (challengeIndex >= challenges.Length)
        {
            codeText.text = "All assignments submitted.";
            statusText.text = "";
            return;
        }

        string t = Target();
        SkipIndent(t);
        string done = t.Substring(0, typed);
        string rest = t.Substring(typed);

        // <noparse> keeps symbols like < and > in code from breaking the colors
        codeText.text = "<color=#66FF66><noparse>" + done + "</noparse></color>" +
                        "<color=#888888><noparse>" + rest + "</noparse></color>";
        statusText.text = "Mistakes: " + mistakes;
    }
}