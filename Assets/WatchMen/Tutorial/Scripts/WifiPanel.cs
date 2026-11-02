using UnityEngine;
using UnityEngine.Events;

public class WifiPanel : MonoBehaviour
{
    public SpotNavigator nav;
    public GameObject panel;              // the wifi minigame UI
    public AudioSource voice;
    public AudioClip workingFineLine;
    public UnityEvent onFixed;

    void Start() => panel.SetActive(false);

    public void OnRouterClicked()         // the router button calls this
    {
        if (!nav.HasFlag("wifiBroken"))
        {
            voice.PlayOneShot(workingFineLine);
            return;
        }
        nav.Freeze();
        panel.SetActive(true);
    }

    public void Close()                   // a close/back button on the panel calls this
    {
        panel.SetActive(false);
        nav.Unfreeze();
    }

    public void Complete()                // WifiTuner calls this when you connect
    {
        nav.SetFlag("wifiBroken", false);
        nav.SetFlag("wifiFixed");
        panel.SetActive(false);
        nav.Unfreeze();
        onFixed.Invoke();
    }
}