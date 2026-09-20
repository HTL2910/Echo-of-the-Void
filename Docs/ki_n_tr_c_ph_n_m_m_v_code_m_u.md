# KIẾN TRÚC PHẦN MỀM & THIẾT KẾ MÃ NGUỒN

Tài liệu cung cấp cấu trúc kiến trúc tiêu chuẩn (Unity/C# hoặc Godot/C#) dựa trên **State Pattern** và **Observer Pattern**, đảm bảo module hóa hoàn toàn.

---

## 1. REALITY EVENT BUS (OBSERVER PATTERN)

Hệ thống điều phối thực tại tập trung. Các đối tượng trong scene chỉ cần lắng nghe sự kiện, không phụ thuộc chéo.

```csharp
using System;
using UnityEngine;

public enum RealmType { Prime, Echo }

public static class RealityEventBus
{
    public static event Action<RealmType> OnRealmSwitched;

    public static void TriggerRealmSwitch(RealmType targetRealm)
    {
        OnRealmSwitched?.Invoke(targetRealm);
    }
}
```

### Script thực thể đổi lớp va chạm (Dành cho Tilemap / Chướng ngại vật)
```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RealityPlatform : MonoBehaviour
{
    [SerializeField] private RealmType solidInRealm;
    private Collider2D _collider;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        RealityEventBus.OnRealmSwitched += HandleRealmSwitch;
    }

    private void OnDestroy()
    {
        RealityEventBus.OnRealmSwitched -= HandleRealmSwitch;
    }

    private void HandleRealmSwitch(RealmType currentRealm)
    {
        bool isSolid = (currentRealm == solidInRealm);
        _collider.enabled = isSolid;

        // Phản hồi trực quan: Mờ đi nếu không thể va chạm
        Color targetColor = _renderer.color;
        targetColor.a = isSolid ? 1.0f : 0.35f;
        _renderer.color = targetColor;
    }
}
```

---

## 2. CHARACTER STATE MACHINE (FSM PATTERN)

Phân tách rành mạch logic điều khiển, loại bỏ cấu trúc lệnh điều kiện phức tạp.

```csharp
public interface IPlayerState
{
    void Enter(PlayerController player);
    void Update(PlayerController player);
    void FixedUpdate(PlayerController player);
    void Exit(PlayerController player);
}

// Trạng thái Lướt (Phase Dash)
public class DashState : IPlayerState
{
    private float _timer;
    private const float DASH_DURATION = 0.2f;
    private const float DASH_SPEED = 24.0f;

    public void Enter(PlayerController player)
    {
        _timer = 0f;
        player.IsInvulnerable = true;
        player.ResetVerticalVelocity();
        player.TriggerDashParticles();
    }

    public void Update(PlayerController player)
    {
        _timer += Time.deltaTime;
        if (_timer >= DASH_DURATION)
        {
            if (player.IsGrounded)
                player.ChangeState(new IdleState());
            else
                player.ChangeState(new FallState());
        }
    }

    public void FixedUpdate(PlayerController player)
    {
        player.SetVelocity(player.FacingDirection * DASH_SPEED, 0f);
    }

    public void Exit(PlayerController player)
    {
        player.IsInvulnerable = false;
        player.StartDashCooldown();
    }
}
```