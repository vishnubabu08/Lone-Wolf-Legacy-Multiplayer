using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootableItem : MonoBehaviour
{
    public string itemName;
    public Sprite icon;
    public string tag;

    public GameObject pickupUI; // UI shown when player enters trigger
   

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pickupUI.SetActive(true);

            // Set text
            var text = pickupUI.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "Press F to pick up " + itemName;
            }

            // Set icon
            var image = pickupUI.transform.Find("IconImage")?.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = icon;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        

        if (other.tag=="Player")
        {
            pickupUI.SetActive(false);
           
        }
    }

    public void HidePickupUI()
    {
        if (pickupUI != null)
            pickupUI.SetActive(false);
    }
}
