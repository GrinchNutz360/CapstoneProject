using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [Header("Two players, each with its OWN Render Texture")]
    public VideoPlayer playerA;
    public VideoPlayer playerB;
    public RawImage imageA;      // shows playerA's texture
    public RawImage imageB;      // shows playerB's texture (above A in the hierarchy)
    public RawImage stillImage;  // above both, for photo idles

    VideoPlayer current, next;
    RawImage currentImage, nextImage;
    Coroutine routine;
    VideoPlayer handlerPlayer;
    VideoPlayer.EventHandler pendingHandler;

    void Awake()
    {
        current = playerA; currentImage = imageA;
        next = playerB; nextImage = imageB;

        playerA.playOnAwake = false;
        playerB.playOnAwake = false;

        imageA.enabled = true;
        imageB.enabled = false;
        if (stillImage != null) stillImage.enabled = false;
    }

    public void Play(VideoClip clip, bool loop = false, Action onFinished = null)
    {
        if (routine != null) StopCoroutine(routine);
        ClearPendingHandler();
        routine = StartCoroutine(PlayRoutine(clip, loop, onFinished));
    }

    public void ShowStill(Texture2D tex)
    {
        if (routine != null) StopCoroutine(routine);
        ClearPendingHandler();
        playerA.Stop();
        playerB.Stop();
        stillImage.texture = tex;
        stillImage.enabled = true;
    }

    IEnumerator PlayRoutine(VideoClip clip, bool loop, Action onFinished)
    {
        next.Stop();
        next.clip = clip;
        next.isLooping = loop;
        next.Prepare();
        while (!next.isPrepared) yield return null;

        next.Play();
        yield return null;                    // let the first frame render

        nextImage.enabled = true;             // show the new clip
        currentImage.enabled = false;         // hide the old one
        if (stillImage != null) stillImage.enabled = false;
        current.Stop();

        (current, next) = (next, current);
        (currentImage, nextImage) = (nextImage, currentImage);

        if (!loop && onFinished != null)
        {
            handlerPlayer = current;
            pendingHandler = vp => { ClearPendingHandler(); onFinished(); };
            current.loopPointReached += pendingHandler;
        }
        routine = null;
    }

    void ClearPendingHandler()
    {
        if (handlerPlayer != null && pendingHandler != null)
            handlerPlayer.loopPointReached -= pendingHandler;
        handlerPlayer = null;
        pendingHandler = null;
    }
}