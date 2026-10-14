using UnityEngine;
using UnityEngine.Video;

public class DeskScene : MonoBehaviour
{
    public VideoPlayer player;
    public VideoClip standUpClip;
    public VideoClip hallwayIdleLoop;

    public void OnMonitorClicked()
    {
        player.isLooping = false;
        player.clip = standUpClip;
        player.Play();
        player.loopPointReached += OnStandUpDone;
    }

    void OnStandUpDone(VideoPlayer vp)
    {
        player.loopPointReached -= OnStandUpDone;
        player.isLooping = true;
        player.clip = hallwayIdleLoop;
        player.Play();
    }
}