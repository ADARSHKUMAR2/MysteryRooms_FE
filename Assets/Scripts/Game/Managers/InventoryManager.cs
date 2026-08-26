using UnityEngine;
using System.Collections.Generic;
using MysteryRooms.Game.Data;

namespace MysteryRooms.Game.Managers
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        // A list of string IDs representing items the player has picked up
        public HashSet<ItemType> collectedItems = new HashSet<ItemType>();


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddItem(ItemType itemID)
        {
            if (!collectedItems.Contains(itemID))
            {
                collectedItems.Add(itemID);
                Debug.Log($"[Inventory] Picked up: {itemID}");
                
                // TODO: You can tell your UI manager to show an icon on screen here!
            }
        }

        public bool HasItem(ItemType item)
        {
            return collectedItems.Contains(item);
        }

        public void ClearInventory()
        {
            collectedItems.Clear();
        }
    }
}
