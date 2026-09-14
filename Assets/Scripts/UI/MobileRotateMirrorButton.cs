using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileRotateMirrorButton : MonoBehaviour, IPointerDownHandler
{
    private CanvasGroup canvasGroup;
    private ReflectingMirror activeMirror; // The exact mirror we are holding

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        ReflectingMirror.OnMirrorStateChanged += OnMirrorStateChanged;
    }

    private void OnDisable()
    {
        ReflectingMirror.OnMirrorStateChanged -= OnMirrorStateChanged;
    }

    // Now receives the exact mirror that was picked up/dropped!
    private void OnMirrorStateChanged(ReflectingMirror mirror, bool isHeld)
    {
        Debug.Log($"[RotateButton] Event received for {mirror.gameObject.name}, isHeld = {isHeld}");
        
        if (isHeld)
        {
            activeMirror = mirror; // Save the exact mirror we picked up
            
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            // Only clear it if we dropped the mirror we were holding
            if (activeMirror == mirror) 
            {
                activeMirror = null;
                
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use(); // Prevent clicking through
        
        if (activeMirror != null)
        {
            Debug.Log($"[RotateButton] Calling MobileRotate() on {activeMirror.gameObject.name}");
            activeMirror.MobileRotate();
        }
        else
        {
            Debug.Log($"[RotateButton] ERROR: activeMirror is null!");
        }
    }
}
