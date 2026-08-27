using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking;
using Unity.Netcode;
using MysteryRooms.Game.Data;

namespace MysteryRooms.UI
{
    public class GameUIController : MonoBehaviour
    {
        public static GameUIController Instance { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI interactionPromptText;

        [Header("Inventory HUD")]
        [Tooltip("The parent object holding inventory icons")]
        public Transform inventoryContainer; 
        [Tooltip("Prefab for a single inventory item (Image + Text)")]
        public GameObject inventoryItemPrefab; 
        
        [Header("Objectives HUD")]
        public TextMeshProUGUI objectiveTitleText;
        public TextMeshProUGUI puzzleProgressText; // E.g., "Puzzles Solved: 2/5"
        public TextMeshProUGUI recentActionText;   // E.g., "You unlocked: secret_passage"

        [Header("Multiplayer Scoreboard")]
        public Transform scoreboardContainer;
        public GameObject playerCardPrefab; // Prefab showing Player Name & Score

        // Dictionary to track instantiated UI elements
        private Dictionary<ItemType, GameObject> spawnedInventoryItems = new Dictionary<ItemType, GameObject>();
        private Dictionary<ulong, TextMeshProUGUI> playerScoreCards = new Dictionary<ulong, TextMeshProUGUI>();


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


        // --- INTERACTION ---
        public void ShowInteractionPrompt(string text)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = text;
                interactionPromptText.gameObject.SetActive(true);
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
        }

        // --- INVENTORY ---
        public void AddItemToHUD(ItemType item, Sprite icon = null)
        {
            Debug.Log($"Attempting to add {item} to HUD...");

            if (inventoryContainer == null || inventoryItemPrefab == null) return;
            if (spawnedInventoryItems.ContainsKey(item)) return;

            GameObject newIcon = Instantiate(inventoryItemPrefab, inventoryContainer);

            newIcon.SetActive(true); 
            
            // Set text name
            TextMeshProUGUI label = newIcon.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = item.ToString();

            // Set image if provided
            UnityEngine.UI.Image img = newIcon.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null && icon != null) img.sprite = icon;

            spawnedInventoryItems.Add(item, newIcon);
            ShowRecentAction($"Picked up: {item.ToString()}");
        }

        public void RemoveItemFromHUD(ItemType item)
        {
            if (spawnedInventoryItems.ContainsKey(item))
            {
                Destroy(spawnedInventoryItems[item]);
                spawnedInventoryItems.Remove(item);
            }
        }

        // --- OBJECTIVES ---
        public void SetObjectiveTitle(string title)
        {
            if (objectiveTitleText != null) objectiveTitleText.text = $"Objective: {title}";
        }

        public void UpdatePuzzleProgress(int solved, int total)
        {
            if (puzzleProgressText != null) puzzleProgressText.text = $"Puzzles Solved: {solved} / {total}";
        }

        public void ShowRecentAction(string actionText)
        {
            if (recentActionText != null)
            {
                recentActionText.text = actionText;
                recentActionText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideRecentAction));
                Invoke(nameof(HideRecentAction), 3f); // Hide after 3 seconds
            }
        }

        private void HideRecentAction()
        {
            if (recentActionText != null) recentActionText.gameObject.SetActive(false);
        }

        // --- SCOREBOARD ---
        public void UpdatePlayerScore(ulong clientId, string playerName, int puzzlesSolved)
        {
            Debug.Log($"[HUD] UpdatePlayerScore called -> clientId: {clientId}, name: {playerName}, solved: {puzzlesSolved}");
            Debug.Log($"[HUD] scoreboardContainer is {(scoreboardContainer == null ? "NULL ❌" : "assigned ✅")}, playerCardPrefab is {(playerCardPrefab == null ? "NULL ❌" : "assigned ✅")}");

            if (scoreboardContainer == null || playerCardPrefab == null)
            {
                Debug.LogError($"[HUD] ❌ ABORTING - Cannot spawn score card! scoreboardContainer: {scoreboardContainer}, playerCardPrefab: {playerCardPrefab}");
                return;
            }

            if (!playerScoreCards.ContainsKey(clientId))
            {
                Debug.Log($"[HUD] 🆕 Spawning new player card for clientId: {clientId}");
                GameObject newCard = Instantiate(playerCardPrefab, scoreboardContainer);
                newCard.SetActive(true);

                var tmpText = newCard.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText == null)
                {
                    Debug.LogError($"[HUD] ❌ Spawned card has NO TextMeshProUGUI child! Card name: {newCard.name}");
                }
                playerScoreCards[clientId] = tmpText;
            }

            Debug.Log($"[HUD] ✅ Updating card -> {playerName}: {puzzlesSolved} Solved");
            playerScoreCards[clientId].text = $"{playerName}: {puzzlesSolved} Solved";
        }


    }
}
