using UnityEngine;

public class DebugFlag : MonoBehaviour
{
    public SpotNavigator nav;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) nav.SetFlag("wifiBroken");
    }
}