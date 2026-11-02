using UnityEngine;
using UnityEngine.Events;

public class WifiPanel : MonoBehaviour
{
    public SpotNavigator nav;
    public GameObject panel;           // WifiPanelRoot
    public GameObject tunerRoot;       // has the WifiTuner script
    public GameObject okScreen;        // your OkText object
    public AudioSource voice;          // optional
    public AudioClip workingFineLine;  // optional
    public UnityEvent onFixed;

    void Start() => panel.SetActive(false);

    // WifiDoor spot > On Enter
    public void Open()
    {
        bool broken = nav.HasFlag("wifiBroken");
        panel.SetActive(true);
        tunerRoot.SetActive(broken);
        okScreen.SetActive(!broken);
        if (!broken && voice != null && workingFineLine != null)
            voice.PlayOneShot(workingFineLine);
    }

    // WifiDoor spot > On Leave
    public void Hide() => panel.SetActive(false);

    // WifiTuner calls this when you connect
    public void Complete()
    {
        nav.SetFlag("wifiBroken", false);
        nav.SetFlag("wifiFixed");
        panel.SetActive(false);
        onFixed.Invoke();
    }
}