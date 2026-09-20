using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.UI
{
    public class RealityUIIndicator : MonoBehaviour
    {
        private RealmType _currentRealm = RealmType.Prime;
        private GUIStyle _headerStyle;
        private GUIStyle _subStyle;
        private GUIStyle _boxStyle;

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitch;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitch;
        }

        private void HandleRealmSwitch(RealmType newRealm)
        {
            _currentRealm = newRealm;
        }

        private void OnGUI()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };

                _subStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
                };

                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(15, 15, 10, 10)
                };
            }

            GUILayout.BeginArea(new Rect(20, 20, 480, 170), _boxStyle);

            string realmName = (_currentRealm == RealmType.Prime) ? "PRIME REALM (Thực Tại)" : "ECHO REALM (Hư Ảnh)";
            Color realmColor = (_currentRealm == RealmType.Prime) 
                ? new Color(0.0f, 0.9f, 1.0f, 1.0f)   // Cyan
                : new Color(0.8f, 0.2f, 1.0f, 1.0f);  // Purple

            _headerStyle.normal.textColor = realmColor;
            GUILayout.Label($"REALM: {realmName}", _headerStyle);

            GUILayout.Space(8);
            GUILayout.Label("• [A / D] Di chuyển (12 tiles/s)", _subStyle);
            GUILayout.Label("• [Space] Nhảy (Coyote Time + Jump Buffer)", _subStyle);
            GUILayout.Label("• [Left Shift] Đổi Thực Tại (Reality Shift)", _subStyle);
            GUILayout.Label("• [J / K] Phase Dash (Lướt 4 tiles - Bất tử)", _subStyle);

            GUILayout.EndArea();
        }
    }
}
