using UnityEngine;
using UnityEngine.UI;

namespace MysteryRooms.UI
{
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveLayout : MonoBehaviour
    {
        [Header("Reference Resolution")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
        
        [Header("Mobile Settings")]
        [SerializeField] private float mobileScaleFactor = 1.2f;
        [SerializeField] private float tabletScaleFactor = 1.1f;
        
        [Header("Orientation")]
        [SerializeField] private bool supportPortrait = true;
        [SerializeField] private bool supportLandscape = true;
        
        [Header("Safe Area")]
        [SerializeField] private bool useSafeArea = true;
        [SerializeField] private RectTransform safeAreaRect;
        
        private CanvasScaler canvasScaler;
        private ScreenOrientation lastOrientation;
        private Rect lastSafeArea;

        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            SetupCanvasScaler();
        }

        private void Start()
        {
            ApplySafeArea();
            lastOrientation = Screen.orientation;
            lastSafeArea = Screen.safeArea;
        }

        private void Update()
        {
            // Check for orientation or safe area changes
            if (Screen.orientation != lastOrientation || Screen.safeArea != lastSafeArea)
            {
                lastOrientation = Screen.orientation;
                lastSafeArea = Screen.safeArea;
                ApplySafeArea();
            }
        }

        private void SetupCanvasScaler()
        {
            if (canvasScaler == null) return;
            
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            
            // Adjust match based on device
            if (IsMobile())
            {
                canvasScaler.matchWidthOrHeight = IsPortrait() ? 0f : 1f;
                canvasScaler.referencePixelsPerUnit = 100f * mobileScaleFactor;
            }
            else if (IsTablet())
            {
                canvasScaler.matchWidthOrHeight = 0.5f;
                canvasScaler.referencePixelsPerUnit = 100f * tabletScaleFactor;
            }
            else
            {
                canvasScaler.matchWidthOrHeight = 0.5f;
                canvasScaler.referencePixelsPerUnit = 100f;
            }
        }

        private void ApplySafeArea()
        {
            if (!useSafeArea || safeAreaRect == null) return;
            
            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            
            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
        }

        private bool IsMobile()
        {
            return Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private bool IsTablet()
        {
            #if UNITY_ANDROID || UNITY_IOS
            float screenRatio = (float)Screen.width / Screen.height;
            return screenRatio > 0.6f && screenRatio < 0.8f;
            #else
            return false;
            #endif
        }

        private bool IsPortrait()
        {
            return Screen.height > Screen.width;
        }

        public void ForceRefresh()
        {
            SetupCanvasScaler();
            ApplySafeArea();
        }
    }
}
