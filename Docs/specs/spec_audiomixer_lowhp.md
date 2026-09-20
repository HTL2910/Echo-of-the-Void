# KỸ THUẬT: HỆ THỐNG AUDIOMIXER & SNAPSHOT DSP (LOW-HP & REALM DYNAMICS)

## 1. Nhận định & Mục tiêu kiến trúc
- **Bài toán:** Spec yêu cầu:
  1. Tuyến routing âm thanh tiêu chuẩn: `Master` -> `Music`, `SFX`, `UI`, `Ambience`.
  2. Điều khiển âm lượng độc lập qua `SettingsService` (Master, Music, SFX, UI volume).
  3. Snapshot chuyển đổi theo trạng thái:
     - `Default_Prime`: Âm sắc bình thường trong Prime Realm.
     - `Default_Echo`: Nhẹ nhàng, tăng chút Reverb/Ethereal cho SFX trong Echo Realm.
     - `LowHealth`: Khi máu < 25%, kích hoạt Low Pass Filter (LPF) ở cutoff **800 Hz** trên toàn bộ nhạc nền/môi trường, chỉ để tiếng tim đập (`LowHealthAudio`) và SFX của Kael sắc nét.
     - `Paused`: Làm dịu nhạc, giảm SFX khi mở Menu/Tạm dừng.
- **Tiêu chí kỹ thuật:**
  - Không alloc rác. Chuyển đổi snapshot mượt mà bằng `AudioMixerSnapshot.TransitionTo(time)`.
  - Giữ tính tương thích ngược: Nếu chưa gán file `AudioMixer.mixer` trong inspector, hệ thống fallback chạy trơn tru với `AudioSource.volume` hiện tại.

---

## 2. Cấu trúc Audio Mixer Bus Routing

```
[Master Bus] (Attenuate 0 dB)
   ├── [Music Bus] (Volume exposed: "MusicVolume")
   │      └── LowPass Filter (Cutoff exposed: "MusicLowPassCutoff", default 22000 Hz, LowHP = 800 Hz)
   ├── [SFX Bus] (Volume exposed: "SfxVolume")
   │      ├── [PlayerSFX Bus] (Không qua LPF khi LowHP để người chơi nghe rõ phản xạ)
   │      └── [WorldSFX Bus] (Quái, bẫy, nổ - có thể LPF nhẹ)
   ├── [UI Bus] (Volume exposed: "UiVolume", độc lập, không bị ducking)
   └── [Ambience Bus] (Gió, tiếng máy móc, warp field)
```

---

## 3. Ma trận Snapshot & Thông số DSP

| Tham số Bus | Default_Prime | Default_Echo | LowHealth (<25% HP) | Paused |
|---|---|---|---|---|
| `MusicLowPassCutoff` | 22,000 Hz | 18,000 Hz | **800 Hz** | 1,200 Hz |
| `MusicVolume` | 0 dB | 0 dB | -3 dB | -6 dB |
| `WorldSfxLowPass` | 22,000 Hz | 16,000 Hz | 2,000 Hz | 800 Hz |
| `HeartbeatVolume` | -80 dB (Mute) | -80 dB | **0 dB** (Active) | -80 dB |
| Thời gian Transition | 0.18 s (Shift) | 0.18 s (Shift) | **0.4 s** (Vào nguy hiểm) | 0.15 s (Tức thì) |

---

## 4. Tích hợp Code: `AudioMixerController.cs`

### 4.1. Kiến trúc Singleton / Component trung tâm
`AudioMixerController` lắng nghe các event sẵn có từ hệ thống:
1. `PlayerStats.OnHealthChanged` -> Kiểm tra ngưỡng `CurrentHealth / MaxHealth <= 0.25f`.
2. `RealityEventBus.OnRealmSwitched` -> Đồng bộ snapshot với `RealmType`.
3. `GameFlow.OnGamePaused` / `OnGameResumed` -> Chuyển đổi snapshot Paused.
4. `SettingsService.OnSettingsChanged` -> Chuyển đổi dB: `volume_dB = Mathf.Log10(Mathf.Max(0.0001f, linear)) * 20f`.

### 4.2. Code mẫu production:
```csharp
using UnityEngine;
using UnityEngine.Audio;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Settings;

namespace EchoOfTheVoid.Audio
{
    public class AudioMixerController : MonoBehaviour
    {
        public static AudioMixerController Instance { get; private set; }

        [Header("Mixer & Snapshots")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerSnapshot snapshotPrime;
        [SerializeField] private AudioMixerSnapshot snapshotEcho;
        [SerializeField] private AudioMixerSnapshot snapshotLowHp;
        [SerializeField] private AudioMixerSnapshot snapshotPaused;

        private bool _isLowHp = false;
        private bool _isPaused = false;
        private RealmType _currentRealm = RealmType.Prime;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            RealityEventBus.OnRealmSwitched += HandleRealmSwitched;
            PlayerStats.HealthChanged += HandlePlayerHealthChanged;
        }

        private void OnDisable()
        {
            RealityEventBus.OnRealmSwitched -= HandleRealmSwitched;
            PlayerStats.HealthChanged -= HandlePlayerHealthChanged;
        }

        public void SetVolume(string exposedParam, float linear01)
        {
            if (mixer == null) return;
            float dB = Mathf.Log10(Mathf.Clamp(linear01, 0.0001f, 1f)) * 20f;
            mixer.SetFloat(exposedParam, dB);
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            bool low = max > 0 && ((float)current / max) <= 0.25f;
            if (low != _isLowHp)
            {
                _isLowHp = low;
                ApplySnapshotState(0.4f);
            }
        }

        private void HandleRealmSwitched(RealmType realm)
        {
            _currentRealm = realm;
            if (!_isLowHp && !_isPaused)
            {
                ApplySnapshotState(0.18f);
            }
        }

        private void ApplySnapshotState(float transitionTime)
        {
            if (mixer == null) return;
            if (_isPaused && snapshotPaused != null)
            {
                snapshotPaused.TransitionTo(transitionTime);
            }
            else if (_isLowHp && snapshotLowHp != null)
            {
                snapshotLowHp.TransitionTo(transitionTime);
            }
            else
            {
                var target = (_currentRealm == RealmType.Echo && snapshotEcho != null) ? snapshotEcho : snapshotPrime;
                target?.TransitionTo(transitionTime);
            }
        }
    }
}
```

---

## 5. Kế hoạch kiểm thử (Tests)
- `AudioMixerTests.cs`:
  1. Kiểm tra chuyển dB chính xác: `linear 1.0 -> 0 dB`, `linear 0.5 -> -6 dB`, `linear 0.0001 -> -80 dB`.
  2. Test mock: Máu rơi dưới 25% -> Event kích hoạt trạng thái LowHP.
  3. Test hồi máu trên 25% -> Phục hồi snapshot realm hiện tại trong 0.4s.
