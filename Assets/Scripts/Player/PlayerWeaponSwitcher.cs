using ActionRPG.Combat;
using ActionRPG.Input;
using UnityEngine;

namespace ActionRPG.Player
{
    [RequireComponent(typeof(PlayerCombatController))]
    public class PlayerWeaponSwitcher : MonoBehaviour
    {
        [Header("Weapon Assets")]
        [SerializeField] private WeaponData swordData;
        [SerializeField] private WeaponData swordAndShieldData;

        [Header("References")]
        [SerializeField] private WeaponHandler weaponHandler;

        private PlayerCombatController combatController;

        private void Awake()
        {
            combatController = GetComponent<PlayerCombatController>();
            if (weaponHandler == null) weaponHandler = GetComponentInChildren<WeaponHandler>();
        }

        private void Update()
        {
            if (InputHandler.Instance == null || weaponHandler == null) return;

            if (InputHandler.Instance.Weapon1Pressed && swordData != null)
            {
                SwitchToWeapon(swordData);
            }
            else if (InputHandler.Instance.Weapon2Pressed && swordAndShieldData != null)
            {
                SwitchToWeapon(swordAndShieldData);
            }
        }

        public void SwitchToWeapon(WeaponData newWeapon)
        {
            if (weaponHandler.CurrentWeaponData == newWeapon) return;

            combatController.ResetCombo();
            weaponHandler.SetWeapon(newWeapon);
            Debug.Log($"[Combat] Switched weapon to: {newWeapon.WeaponName}");
        }

        public bool TryEquipBlockingWeapon()
        {
            if (swordAndShieldData == null || !swordAndShieldData.CanBlock || weaponHandler == null) return false;

            SwitchToWeapon(swordAndShieldData);
            return true;
        }

        public void SetWeaponDataAssets(WeaponData sword, WeaponData shield)
        {
            swordData = sword;
            swordAndShieldData = shield;
        }
    }
}
