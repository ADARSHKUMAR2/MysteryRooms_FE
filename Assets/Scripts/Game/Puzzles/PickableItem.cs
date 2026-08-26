using UnityEngine;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Data;

public class PickableItem : MonoBehaviour, IInteractable
{
    [Tooltip("Select which item this physical object represents")]
    public ItemType itemType = ItemType.TombKey;
    
    [Tooltip("What the prompt says (e.g., 'Ancient Key')")]
    public string itemName = "Ancient Key";

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }

    public void Interact()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemType);
        }
        else
        {
            Debug.LogError("No InventoryManager found in scene!");
        }

        Destroy(gameObject);
    }
}
