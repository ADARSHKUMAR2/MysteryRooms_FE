using UnityEngine;

public class MobileTestingConfig : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableMobileTestingMode()
    {
        #if UNITY_EDITOR
        // Create a temporary object to hold the testing flag
        GameObject testConfig = new GameObject("__MobileTestingConfig__");
        DontDestroyOnLoad(testConfig);
        testConfig.AddComponent<MobileTestingFlag>();
        #endif
    }
}

public class MobileTestingFlag : MonoBehaviour
{
    public static bool ForceMobileMode { get; set; } = false;
}
