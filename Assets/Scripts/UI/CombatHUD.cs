using ActionRPG.Combat;
using ActionRPG.Player;
using UnityEngine;

namespace ActionRPG.UI
{
    public class CombatHUD : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerBlockSystem playerBlockSystem;
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private WeaponHandler weaponHandler;

        private GUIStyle headerStyle;
        private GUIStyle barStyle;
        private GUIStyle textStyle;
        private GUIStyle warningStyle;
        private GUIStyle controlsStyle;
        private Texture2D redTex;
        private Texture2D greenTex;
        private Texture2D cyanTex;
        private Texture2D bgTex;

        private void Start()
        {
            FindPlayerReferences();
            InitStyles();
        }

        private void FindPlayerReferences()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<Health>();
                playerBlockSystem = playerObj.GetComponent<PlayerBlockSystem>();
                combatController = playerObj.GetComponent<PlayerCombatController>();
                weaponHandler = playerObj.GetComponentInChildren<WeaponHandler>();
            }
        }

        private void InitStyles()
        {
            redTex = MakeTex(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f));
            greenTex = MakeTex(2, 2, new Color(0.2f, 0.8f, 0.3f, 0.9f));
            cyanTex = MakeTex(2, 2, new Color(0.2f, 0.7f, 0.9f, 0.9f));
            bgTex = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.12f, 0.8f));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            if (playerHealth == null) FindPlayerReferences();
            if (playerHealth == null) return;

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter
                };

                warningStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.red },
                    alignment = TextAnchor.MiddleCenter
                };

                controlsStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    normal = { textColor = Color.yellow, background = bgTex },
                    alignment = TextAnchor.UpperLeft,
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            // HUD Container Box
            GUI.DrawTexture(new Rect(15, 15, 320, 180), bgTex);

            // 1. Health Bar
            GUI.Label(new Rect(25, 20, 200, 25), "PLAYER HEALTH", headerStyle);
            float hpPercent = Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth);
            GUI.DrawTexture(new Rect(25, 45, 300, 24), redTex);
            GUI.DrawTexture(new Rect(25, 45, 300 * hpPercent, 24), greenTex);
            GUI.Label(new Rect(25, 45, 300, 24), $"{Mathf.CeilToInt(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}", textStyle);

            // 2. Block Durability Bar
            if (playerBlockSystem != null && weaponHandler != null && weaponHandler.CurrentWeaponData != null)
            {
                var weapon = weaponHandler.CurrentWeaponData;
                if (weapon.CanBlock)
                {
                    GUI.Label(new Rect(25, 75, 200, 25), "BLOCK DURABILITY", headerStyle);
                    float blockPercent = Mathf.Clamp01(playerBlockSystem.CurrentBlockDurability / playerBlockSystem.MaxBlockDurability);
                    GUI.DrawTexture(new Rect(25, 100, 300, 20), redTex);
                    GUI.DrawTexture(new Rect(25, 100, 300 * blockPercent, 20), cyanTex);
                    GUI.Label(new Rect(25, 100, 300, 20), $"{Mathf.CeilToInt(playerBlockSystem.CurrentBlockDurability)} / {playerBlockSystem.MaxBlockDurability}", textStyle);

                    if (playerBlockSystem.IsGuardBroken)
                    {
                        GUI.Label(new Rect(25, 122, 300, 25), "⚠ GUARD BROKEN! ⚠", warningStyle);
                    }
                    else if (playerBlockSystem.IsBlocking)
                    {
                        GUI.Label(new Rect(25, 122, 300, 25), "🛡 BLOCKING", textStyle);
                    }
                }
                else
                {
                    GUI.Label(new Rect(25, 80, 300, 25), "[Shield Unavailable - Sword Equipped]", headerStyle);
                }
            }

            // 3. Active Weapon & Combo Info
            if (weaponHandler != null && weaponHandler.CurrentWeaponData != null)
            {
                string weaponName = weaponHandler.CurrentWeaponData.WeaponName;
                int comboStep = combatController != null ? combatController.CurrentComboIndex + 1 : 1;
                GUI.Label(new Rect(25, 150, 300, 25), $"Active Weapon: {weaponName} (Combo Step: {comboStep})", headerStyle);
            }

            // 4. Controls Guide Panel
            string controlsText = "<b>COMBAT CONTROLS:</b>\n" +
                                 "• <b>WASD</b>: Move Phantom\n" +
                                 "• <b>Mouse Cursor</b>: Facing Direction\n" +
                                 "• <b>LMB (Click)</b>: Attack Combo (3-hit chain)\n" +
                                 "• <b>RMB (Hold)</b>: Block (Sword & Shield)\n" +
                                 "• <b>Key 1</b>: Equip Sword\n" +
                                 "• <b>Key 2</b>: Equip Sword & Shield";
            GUI.Box(new Rect(Screen.width - 280, 15, 265, 155), controlsText, controlsStyle);
        }
    }
}
