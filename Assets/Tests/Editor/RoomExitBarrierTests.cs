using NUnit.Framework;
using ActionRPG.Encounters;
using UnityEngine;

namespace ActionRPG.Tests.Editor
{
    public class RoomExitBarrierTests
    {
        [Test]
        public void OpenBarrier_DisablesBlockingColliderAndVisual()
        {
            var root = new GameObject("BarrierTest");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(root.transform);
            var barrier = root.AddComponent<RoomExitBarrier>();
            barrier.ConfigureRuntime(visual.GetComponent<Collider>(), visual.GetComponent<Renderer>());

            barrier.OpenBarrier();

            Assert.That(visual.GetComponent<Collider>().enabled, Is.False);
            Assert.That(visual.GetComponent<Renderer>().enabled, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}
