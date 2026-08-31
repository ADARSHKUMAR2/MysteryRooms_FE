using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class MapCoordinatesPuzzle : BasePuzzle, IInteractable
{
    [Header("Backend Data")]
    private string correctLatitude;
    private string correctLongitude;
    
    [Header("UI References - Full Screen Map")]
    [Tooltip("The Canvas Panel that shows the full-screen map")]
    [SerializeField] private GameObject fullScreenMapPanel;
    [Tooltip("The text on the map that displays the coordinates")]
    [SerializeField] private TextMeshProUGUI mapCoordinatesText;
    
    [Header("UI References - Astrolabe Input")]
    [Tooltip("The Canvas Panel where the player inputs the code")]
    [SerializeField] private GameObject astrolabeInputPanel;
    [SerializeField] private TMP_InputField latInputField;
    [SerializeField] private TMP_InputField longInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    protected override void Start()
    {
        base.Start();
        
        if (fullScreenMapPanel != null) fullScreenMapPanel.SetActive(false);
        if (astrolabeInputPanel != null) astrolabeInputPanel.SetActive(false);
        
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (closeButton != null) closeButton.onClick.AddListener(CloseAstrolabeUI);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        if (config.config != null)
        {
            correctLatitude = config.config.latitude;
            correctLongitude = config.config.longitude;
            
            if (mapCoordinatesText != null)
            {
                mapCoordinatesText.text = $"{correctLatitude}\n{correctLongitude}";
            }
            
            Debug.Log($"🌍 [{puzzleID}] Astrolabe configured. Lat: {correctLatitude}, Long: {correctLongitude}");
        }
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) return "Astrolabe Aligned ✓";
        if (isLockedByDependencies) return "The gears are rusted shut.";
        return "Press E to input coordinates into Astrolabe";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();
        
        if (astrolabeInputPanel != null)
        {
            astrolabeInputPanel.SetActive(true);
            
            if (latInputField != null) latInputField.text = "";
            if (longInputField != null) longInputField.text = "";
            if (feedbackText != null) feedbackText.text = "Awaiting Alignment...";
        }
    }

    public void OpenMap()
    {
        if (fullScreenMapPanel != null) fullScreenMapPanel.SetActive(true);
    }

    public void CloseMap()
    {
        if (fullScreenMapPanel != null) fullScreenMapPanel.SetActive(false);
    }
    
    public void CloseAstrolabeUI()
    {
        if (astrolabeInputPanel != null) astrolabeInputPanel.SetActive(false);
    }

    private void OnSubmitClicked()
    {
        if (latInputField == null || longInputField == null) return;
        
        string inputLat = latInputField.text.Trim().ToUpper();
        string inputLong = longInputField.text.Trim().ToUpper();
        
        if (string.IsNullOrEmpty(inputLat) || string.IsNullOrEmpty(inputLong)) return;

        if (IsSpawned)
        {
            SubmitCoordinatesServerRpc(inputLat, inputLong);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCoordinatesServerRpc(string inputLat, string inputLong, ServerRpcParams rpcParams = default)
    {
        if (isSolvedNet.Value) return;

        if (correctLatitude == null || correctLongitude == null) return;

        if (inputLat == correctLatitude.Trim().ToUpper() && inputLong == correctLongitude.Trim().ToUpper())
        {
            isSolvedNet.Value = true;
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
        else
        {
            WrongCoordinatesClientRpc();
        }
    }

    [ClientRpc]
    private void WrongCoordinatesClientRpc()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "<color=red>Gears grinding... Invalid Coordinates!</color>";
            feedbackText.gameObject.SetActive(true);
        }
    }

    private void OnSolvedStateChanged(bool prev, bool current)
    {
        if (current)
        {
            Debug.Log($"🎉 [{puzzleID}] Astrolabe aligned!");
            
            if (feedbackText != null)
            {
                feedbackText.text = "<color=green>Coordinates Accepted. Mechanism Unlocked!</color>";
            }
            
            Invoke(nameof(CloseAstrolabeUI), 2f);
        }
    }

    protected override bool CheckSolution() { return isSolvedNet.Value; }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer) isSolvedNet.Value = false;
        
        if (feedbackText != null) feedbackText.text = "";
        if (latInputField != null) latInputField.text = "";
        if (longInputField != null) longInputField.text = "";
    }
}
