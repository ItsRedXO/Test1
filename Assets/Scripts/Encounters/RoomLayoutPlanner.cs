using UnityEngine;

namespace ActionRPG.Encounters
{
    public static class RoomLayoutPlanner
    {
        public static float GetNextRoomCenterZ(float firstGateZ, float roomDepth)
        {
            return firstGateZ + Mathf.Max(6f, roomDepth * 0.5f + 3f);
        }
    }
}
