using TMPro;
using UnityEngine;

public class PackageCard : MonoBehaviour
{
    public SpotNavigator nav;
    public GameObject card;
    public GameObject cornerButton;   // optional, can stay empty
    public TMP_Text stickerText;      // optional
    public KeyCode key = KeyCode.Tab;

    void Start()
    {
        card.SetActive(false);
        if (cornerButton != null) cornerButton.SetActive(false);
    }

    void Update()
    {
        bool holding = nav.HasFlag("packageGrabbed") && !nav.HasFlag("packageDelivered");

        if (cornerButton != null && cornerButton.activeSelf != holding)
            cornerButton.SetActive(holding);
        if (!holding && card.activeSelf) card.SetActive(false);

        if (Input.GetKeyDown(key))
        {
            Debug.Log("Tab pressed. Holding package: " + holding);   // remove later
            if (holding) Toggle();
        }
    }

    public void Toggle()
    {
        card.SetActive(!card.activeSelf);
        if (card.activeSelf) nav.SetFlag("cardChecked");
    }

    public void SetSticker(int floor, int door)
    {
        if (stickerText != null) stickerText.text = "FLOOR " + floor + "\nDOOR " + door;
    }
}