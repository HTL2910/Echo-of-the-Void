using System.Collections.Generic;
using UnityEngine;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;

namespace EchoOfTheVoid.UI
{
    public class RealityUIIndicator : MonoBehaviour
    {
        private RealmType _currentRealm = RealmType.Prime;
        private PlayerStats _playerStats;
        private PlayerController _playerController;
        private PlayerCombat _playerCombat;

        private GUIStyle _headerStyle;
        private GUIStyle _subStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _barBgStyle;
        private GUIStyle _deflectStyle;

        private string _lastCombatNotice = "";
        private float _noticeTimer = 0f;
        private bool _isLastDeflected = false;

        private void Start()
        {
            FindPlayerReferences();
            RealityEventBus.OnRealmSwitched += HandleRealmSwitch;
        }

        private void OnDestroy()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitch;
            if (_playerCombat != null)
            {
                _playerCombat.OnEnemyHit -= HandleEnemyHit;
            }
        }

        private void FindPlayerReferences()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerStats = playerObj.GetComponent<PlayerStats>();
                _playerController = playerObj.GetComponent<PlayerController>();
                _playerCombat = playerObj.GetComponent<PlayerCombat>();
                if (_playerCombat != null)
                {
                    _playerCombat.OnEnemyHit += HandleEnemyHit;
                }
            }
        }

        private void HandleEnemyHit(bool isDeflected, int damage)
        {
            _isLastDeflected = isDeflected;
            if (isDeflected)
            {
                _lastCombatNotice = $"DEFLECT! (Lệch Hệ: {damage} DMG - Đổi Realm ngay!)";
            }
            else
            {
                _lastCombatNotice = $"HIT CLEAN! (Đúng Hệ: {damage} DMG)";
            }
            _noticeTimer = 1.8f;
        }

        private void HandleRealmSwitch(RealmType newRealm)
        {
            _currentRealm = newRealm;
        }

        private void Update()
        {
            if (_noticeTimer > 0f) _noticeTimer -= Time.deltaTime;
            if (_playerStats == null) FindPlayerReferences();
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Status Panel (Top Left)
            GUILayout.BeginArea(new Rect(20, 20, 480, 220), _boxStyle);

            string realmName = (_currentRealm == RealmType.Prime) ? "PRIME REALM (Hiện Sinh)" : "ECHO REALM (Nghịch Ảnh)";
            Color realmColor = (_currentRealm == RealmType.Prime) 
                ? new Color(0.0f, 0.9f, 1.0f, 1.0f)   // Cyan
                : new Color(0.85f, 0.25f, 1.0f, 1.0f); // Purple

            _headerStyle.normal.textColor = realmColor;
            GUILayout.Label($"REALM: {realmName}", _headerStyle);

            // HP Bar
            int hp = (_playerStats != null) ? _playerStats.CurrentHealth : 100;
            int maxHp = (_playerStats != null) ? _playerStats.MaxHealth : 100;
            DrawBar("HP", hp, maxHp, new Color(0.9f, 0.25f, 0.25f, 1f));

            // CE Bar
            float ce = (_playerStats != null) ? _playerStats.CurrentEnergy : 100f;
            int maxCe = (_playerStats != null) ? _playerStats.MaxEnergy : 100;
            DrawBar("CE", Mathf.RoundToInt(ce), maxCe, new Color(0.2f, 0.8f, 1.0f, 1f));

            // Player State
            string stateName = (_playerController != null) ? _playerController.CurrentStateName : "Idle";
            GUILayout.Label($"State: {stateName} | Combo: {(_playerCombat != null ? _playerCombat.ComboStep : 0)}", _subStyle);

            // Controls Summary
            GUILayout.Space(4);
            GUILayout.Label("• [A/D] Chạy  |  [Space] Nhảy & Wall Jump  |  [Shift] Đổi Thực Tại", _subStyle);
            GUILayout.Label("• [K / Ctrl] Phase Dash  |  [J] Chém Combo  |  [L] Resonance Strike (50 CE)", _subStyle);

            GUILayout.EndArea();

            // 2. Combat Deflect / Hit Notice (Center Top)
            if (_noticeTimer > 0f && !string.IsNullOrEmpty(_lastCombatNotice))
            {
                _deflectStyle.normal.textColor = _isLastDeflected ? new Color(1f, 0.85f, 0.1f, 1f) : new Color(0.2f, 1f, 0.4f, 1f);
                GUI.Label(new Rect(Screen.width * 0.5f - 250, 40, 500, 40), _lastCombatNotice, _deflectStyle);
            }
        }

        private void DrawBar(string label, int current, int max, Color fillColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {current}/{max}", _subStyle, GUILayout.Width(100));

            Rect barRect = GUILayoutUtility.GetRect(280, 16);
            GUI.Box(barRect, GUIContent.none, _barBgStyle);

            float fillWidth = (max > 0) ? (barRect.width * Mathf.Clamp01((float)current / max)) : 0f;
            Rect fillRect = new Rect(barRect.x, barRect.y, fillWidth, barRect.height);

            Color prev = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = prev;

            GUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 10, 10)
            };

            _barBgStyle = new GUIStyle(GUI.skin.box);

            _deflectStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
