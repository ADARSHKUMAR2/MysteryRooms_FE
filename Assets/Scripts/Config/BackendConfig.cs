using UnityEngine;

namespace MysteryRooms.Config 
{
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "MysteryRooms/Backend Configuration")]
    public class BackendConfig : ScriptableObject
    {
        [Header("Local Development")]
        public string localGatewayURL = "http://localhost:8000";
        public string localAuthURL = "http://localhost:8001";
        public string localGameURL = "http://localhost:8002";
        
        [Header("Production")]
        public string productionGatewayURL = "https://your-production-gateway-url.com";
        public string productionAuthURL = "https://mysteryrooms-auth-ppljpeuuvq-el.a.run.app";
        public string productionGameURL = "https://mysteryrooms-game-ppljpeuuvq-el.a.run.app";
        
        [Header("Environment Selection")]
        public bool useProductionEnv = false;

        // For services that use the gateway (when available)
        public string CurrentURL => useProductionEnv ? productionGatewayURL : localGatewayURL;
        
        // For direct auth service access
        public string CurrentAuthURL => useProductionEnv ? productionAuthURL : localAuthURL;
        
        // For direct game service access
        public string CurrentGameURL => useProductionEnv ? productionGameURL : localGameURL;
    }
}
