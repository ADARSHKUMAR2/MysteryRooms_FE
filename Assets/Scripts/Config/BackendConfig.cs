using UnityEngine;

namespace MysteryRooms.Config 
{
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "MysteryRooms/Backend Configuration")]
    public class BackendConfig : ScriptableObject
    {
        [Header("Local Development")]
        public string localGatewayURL = "http://localhost:8000";
        
        [Header("Production")]
        public string productionGatewayURL = "https://your-production-url.com";
        
        [Header("Environment Selection")]
        public bool useProductionEnv = false;

        public string CurrentURL => useProductionEnv ? productionGatewayURL : localGatewayURL;
    }
}
