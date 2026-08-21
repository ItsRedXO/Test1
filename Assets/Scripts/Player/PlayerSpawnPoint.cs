using UnityEngine;

namespace ActionRPG.Player
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        public static PlayerSpawnPoint Active { get; private set; }

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Debug.LogWarning("[Player] Multiple PlayerSpawnPoint components found. Using the first active spawn point.", this);
                return;
            }

            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }
    }
}
