using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public string name;
        public Sprite icon;
        public string tag;
    }

    public List<Item> items = new List<Item>();
    public Transform inventoryPanel;
    public GameObject inventorySlotPrefab;
    

    public void AddItem(string itemName, Sprite icon,string tag="")
    {
        Item newItem = new Item { name = itemName, icon = icon,tag = tag };
        items.Add(newItem);

        GameObject slot = Instantiate(inventorySlotPrefab, inventoryPanel);
        slot.transform.Find("IconImage").GetComponent<Image>().sprite = icon;
        slot.GetComponentInChildren<TextMeshProUGUI>().text = itemName;

    }
    
    public bool HasGunInInventory()
    {
        foreach (var item in items)
        {
            if (item.tag == "Gun")
                return true;
        }
        return false;
    } 
    
    
    public bool HasMagInInventory()
    {
        foreach (var item in items)
        {
            if (item.tag == "Mag")
                return true;
        }
        return false;
    }
   
    public int GetGunCount()
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item.tag == "Gun")
                count++;
        }
        return count;
    }

}
