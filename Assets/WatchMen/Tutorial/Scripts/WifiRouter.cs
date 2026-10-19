using UnityEngine;
using UnityEngine.Events;

public class WifiRouter : MonoBehaviour
{
    public SpotNavigator nav;
    public AudioSource voice;
    public AudioClip workingFineLine;   // "wifi's fine..."
    public UnityEvent onFixed;          // start the chase from here

    public void OnRouterClicked()
    {
        if (!nav.HasFlag("wifiBroken"))
        {
            voice.PlayOneShot(workingFineLine);
            return;
        }

        nav.SetFlag("wifiBroken", false);
        nav.SetFlag("wifiFixed");
        onFixed.Invoke();
    }
}