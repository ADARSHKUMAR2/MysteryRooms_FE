using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ElementalCylinderData : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshPro fireText;
    public TextMeshPro leafText;
    public TextMeshPro waterText;
    public TextMeshPro sunText;

    /// <summary>
    /// Call this from the CombinationLockPuzzle when data is received from backend
    /// </summary>
    public void SetMappings(Dictionary<string, int> mappings)
    {
        if (mappings == null) return;

        if (fireText != null && mappings.ContainsKey("Fire"))
            fireText.text = $"FIRE\n{mappings["Fire"]}";

        if (leafText != null && mappings.ContainsKey("Leaf"))
            leafText.text = $"LEAF\n{mappings["Leaf"]}";

        if (waterText != null && mappings.ContainsKey("Water"))
            waterText.text = $"WATER\n{mappings["Water"]}";

        if (sunText != null && mappings.ContainsKey("Sun"))
            sunText.text = $"SUN\n{mappings["Sun"]}";
            
        Debug.Log("🔮 Elemental Cylinder mappings updated from backend!");
    }
}
