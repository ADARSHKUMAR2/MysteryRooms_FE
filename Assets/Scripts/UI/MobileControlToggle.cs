using UnityEngine;

public class MobileControlToggle : MonoBehaviour
{
    void Awake()
    {
        // Check if we are on a mobile platform
        bool isMobile = Application.platform == RuntimePlatform.Android || 
                        Application.platform == RuntimePlatform.IPhonePlayer;

        // If we are in the editor, you can force them ON for testing by uncommenting this:
        isMobile = true;  // ← UNCOMMENT THIS TO TEST MOBILE CONTROLS IN EDITOR

        gameObject.SetActive(isMobile);
    }
}
