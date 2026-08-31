using UnityEngine;
using System.Collections.Generic;

public class ElementalScalesData : MonoBehaviour
{
    [Header("Scale Pans (Where weights go)")]
    public Transform fireScalePan;
    public Transform waterScalePan;
    public Transform leafScalePan;
    public Transform sunScalePan;

    [Header("Prefabs")]
    public GameObject ironWeightPrefab;

    public void SetMappings(Dictionary<string, int> mappings)
    {
        if (mappings == null || ironWeightPrefab == null) return;

        if (mappings.ContainsKey("Fire")) SpawnWeights(fireScalePan, mappings["Fire"]);
        if (mappings.ContainsKey("Water")) SpawnWeights(waterScalePan, mappings["Water"]);
        if (mappings.ContainsKey("Leaf")) SpawnWeights(leafScalePan, mappings["Leaf"]);
        if (mappings.ContainsKey("Sun")) SpawnWeights(sunScalePan, mappings["Sun"]);
        
        Debug.Log("⚖️ Elemental Scales weights spawned based on backend data!");
    }

    private void SpawnWeights(Transform pan, int count)
    {
        if (pan == null) return;

        // Clear existing weights if any
        foreach (Transform child in pan) Destroy(child.gameObject);

        // Spawn weights in a neat little pile
        for (int i = 0; i < count; i++)
        {
            GameObject weight = Instantiate(ironWeightPrefab, pan);
            
            // Randomize position slightly so they pile up naturally
            float offsetX = Random.Range(-0.15f, 0.15f);
            float offsetZ = Random.Range(-0.15f, 0.15f);
            
            // Stack them up if there are many
            float offsetY = (i / 4) * 0.15f; 
            
            weight.transform.localPosition = new Vector3(offsetX, offsetY, offsetZ);
            
            // Random slight rotation for realism
            weight.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        }
    }
}
