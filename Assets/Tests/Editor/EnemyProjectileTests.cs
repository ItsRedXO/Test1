using ActionRPG.Combat;
using ActionRPG.Enemy;
using NUnit.Framework;
using UnityEngine;

public class EnemyProjectileTests
{
    [Test]
    public void BuildDamageInfoMarksProjectileDamageBlockable()
    {
        GameObject attacker = new GameObject("RangedEnemy");

        try
        {
            DamageInfo damage = EnemyProjectile.BuildDamageInfo(
                attacker,
                12f,
                Vector3.forward,
                0.25f,
                Vector3.one
            );

            Assert.That(damage.Amount, Is.EqualTo(12f));
            Assert.That(damage.Attacker, Is.EqualTo(attacker));
            Assert.That(damage.IsBlockable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(attacker);
        }
    }

    [Test]
    public void LaunchPositionIsRaisedAboveFloor()
    {
        Vector3 launchPosition = EnemyProjectile.GetLaunchPosition(Vector3.zero);

        Assert.That(launchPosition, Is.EqualTo(new Vector3(0f, 0.75f, 0f)));
    }

    [Test]
    public void AimPointTargetsUpperBodyInsteadOfFeet()
    {
        Vector3 aimPoint = EnemyProjectile.GetAimPoint(Vector3.zero);

        Assert.That(aimPoint, Is.EqualTo(new Vector3(0f, 1f, 0f)));
    }
}
