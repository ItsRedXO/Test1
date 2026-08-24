#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace ActionRPG.Encounters.Tests
{
    public class RoomLayoutPlannerTests
    {
        [Test]
        public void SecondRoomPosition_IsBeyondFirstGate()
        {
            float firstGateZ = 10f;
            float roomDepth = 14f;

            float secondRoomCenterZ = RoomLayoutPlanner.GetNextRoomCenterZ(firstGateZ, roomDepth);

            Assert.Greater(secondRoomCenterZ, firstGateZ);
        }
    }
}
#endif
