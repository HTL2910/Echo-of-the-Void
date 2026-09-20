using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Bosses;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Environment;
using EchoOfTheVoid.Environment.Mechanics;
using EchoOfTheVoid.Player;
using EchoOfTheVoid.Save;
using EchoOfTheVoid.UI;

namespace EchoOfTheVoid.Tests
{
    /// <summary>The three workstreams (core, Codex, Anti) wired together and played as a whole.</summary>
    public class BossFightIntegrationTests
    {
        private TestWorld _world;
        private string _dir;
        private readonly List<GameObject> _cleanup = new List<GameObject>();
        private Sentinel01 _boss;
        private BossArena _arena;
        private GameObject _gate;
        private GameObject _rewardTemplate;
        private GameObject _stationTemplate;
        private int _enemyLayer;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "eotv_it_" + System.Guid.NewGuid().ToString("N"));
            SaveService.DirectoryOverride = _dir;
            GameSession.Reset();
            _enemyLayer = LayerMask.NameToLayer("Enemy");
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            foreach (var go in _cleanup) if (go != null) Object.Destroy(go);
            _cleanup.Clear();
            foreach (var pickup in Object.FindObjectsByType<AbilityPickup>(FindObjectsSortMode.None)) Object.Destroy(pickup.gameObject);
            foreach (var station in Object.FindObjectsByType<ChronoStation>(FindObjectsSortMode.None)) Object.Destroy(station.gameObject);
            Time.timeScale = 1f;
            GameSession.Reset();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        private T Track<T>(T go) where T : Object
        {
            if (go is GameObject g) _cleanup.Add(g);
            return go;
        }

        /// <summary>Kael starts at x = -6; the arena trigger covers x 4..20, its gate stands at x = 2.</summary>
        private void BuildArena()
        {
            _world = TestWorld.Create(new Vector2(-6f, 1.5f));

            var bossGo = Track(new GameObject("Sentinel01"));
            bossGo.layer = _enemyLayer;
            bossGo.transform.position = new Vector3(16f, 1.25f, 0f);
            var rb = bossGo.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
            bossGo.AddComponent<BoxCollider2D>().size = new Vector2(2f, 3f);
            _boss = bossGo.AddComponent<Sentinel01>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(bossGo.transform, false);
            visual.AddComponent<SpriteRenderer>();

            _gate = Track(new GameObject("Gate"));
            _gate.transform.position = new Vector3(2f, 4f, 0f);
            _gate.layer = _world.NeutralLayer;
            _gate.AddComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
            _gate.SetActive(false);

            // "Prefabs": active templates parked far below the world so nothing touches them
            _rewardTemplate = Track(new GameObject("RewardTemplate"));
            _rewardTemplate.transform.position = new Vector3(0f, -100f, 0f);
            var reward = _rewardTemplate.AddComponent<CircleCollider2D>();
            reward.isTrigger = true;
            reward.radius = 0.7f;
            _rewardTemplate.AddComponent<PersistentId>().SetId("pickup_walljump");
            _rewardTemplate.AddComponent<AbilityPickup>().Configure(AbilityFlags.WallJump, "PISTON BOOTS");

            _stationTemplate = Track(new GameObject("StationTemplate"));
            _stationTemplate.transform.position = new Vector3(0f, -110f, 0f);
            _stationTemplate.AddComponent<BoxCollider2D>().isTrigger = true;
            _stationTemplate.AddComponent<ChronoStation>().Configure("station_boss", 25);

            var arenaGo = Track(new GameObject("Arena"));
            arenaGo.transform.position = new Vector3(12f, 3f, 0f);
            arenaGo.AddComponent<BoxCollider2D>().size = new Vector2(16f, 14f); // x 4..20
            var rewardPoint = new GameObject("RewardPoint").transform;
            rewardPoint.SetParent(arenaGo.transform, false);
            rewardPoint.position = new Vector3(10f, 0.3f, 0f);
            var stationPoint = new GameObject("StationPoint").transform;
            stationPoint.SetParent(arenaGo.transform, false);
            stationPoint.position = new Vector3(6f, 0f, 0f);
            _arena = arenaGo.AddComponent<BossArena>();
            _arena.Configure(_boss, _gate, _rewardTemplate, rewardPoint, _stationTemplate, stationPoint);
        }

        private IEnumerator RunIntoTheArena()
        {
            _world.Input.Move = 1f;
            yield return new WaitForSeconds(1.2f); // -6 -> ~8
            _world.Input.Move = 0f;
        }

        [UnityTest]
        public IEnumerator EnteringTheArena_EngagesTheBoss_ShowsTheBar_AndLocksTheGate()
        {
            BuildArena();
            var barGo = Track(new GameObject("BossBar"));
            var bar = barGo.AddComponent<BossHealthBar>();
            int engaged = 0; string engagedId = null;
            System.Action<string, string, int> onEngaged = (id, n, max) => { engaged++; engagedId = id; };
            BossEvents.Engaged += onEngaged;
            try
            {
                yield return new WaitForSeconds(0.5f);
                Assert.IsFalse(_arena.FightStarted, "Nothing happens while Kael is outside");
                Assert.IsFalse(_gate.activeSelf);

                yield return RunIntoTheArena();

                Assert.IsTrue(_arena.FightStarted);
                Assert.AreEqual(1, engaged, "Engaged exactly once");
                Assert.AreEqual("sentinel_01", engagedId);
                Assert.IsTrue(_gate.activeSelf, "The entrance locks behind Kael");
                Assert.IsTrue(bar.IsVisible, "The boss health bar appears (core UI + Codex boss)");
                Assert.AreEqual("SENTINEL-01", bar.DisplayedName);
            }
            finally { BossEvents.Engaged -= onEngaged; }
        }

        [UnityTest]
        public IEnumerator KaelsAttack_ReachesTheBoss_AndMovesTheHealthBar()
        {
            BuildArena();
            var barGo = Track(new GameObject("BossBar"));
            var bar = barGo.AddComponent<BossHealthBar>();
            yield return RunIntoTheArena();
            Assert.IsTrue(_arena.FightStarted);
            int hpBefore = _boss.CurrentHp;

            _world.Player.transform.position = new Vector2(14.2f, 1.5f); // right in front of the boss
            _world.Rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.4f);
            _world.Input.PressAttack();
            yield return new WaitForSeconds(0.4f);

            Assert.Less(_boss.CurrentHp, hpBefore, "Kael's normal attack damages the boss (it must sit on the Enemy layer)");
            Assert.Less(bar.FillAmount, 1f, "and the bar follows");
        }

        [UnityTest]
        public IEnumerator DefeatingTheBoss_UnlocksRewardsAndIsRemembered()
        {
            BuildArena();
            yield return RunIntoTheArena();
            Assert.IsTrue(_gate.activeSelf);

            _boss.TakeDamage(new DamageInfo(99999, _boss.transform.position, Vector2.zero, RealmType.Echo));
            yield return new WaitForSeconds(0.8f);

            Assert.IsTrue(_arena.IsDefeated);
            Assert.IsFalse(_gate.activeSelf, "The entrance reopens");
            Assert.IsTrue(GameSession.IsBossDefeated("sentinel_01"), "The kill is recorded in the save");

            bool rewardSpawned = false, stationSpawned = false;
            foreach (var p in Object.FindObjectsByType<AbilityPickup>(FindObjectsSortMode.None))
                if (Vector3.Distance(p.transform.position, new Vector3(10f, 0.3f, 0f)) < 0.1f) rewardSpawned = true;
            foreach (var st in Object.FindObjectsByType<ChronoStation>(FindObjectsSortMode.None))
                if (Vector3.Distance(st.transform.position, new Vector3(6f, 0f, 0f)) < 0.1f) stationSpawned = true;
            Assert.IsTrue(rewardSpawned, "The ability reward appears");
            Assert.IsTrue(stationSpawned, "A Chrono Station appears");
        }

        [UnityTest]
        public IEnumerator KaelDyingMidFight_ResetsTheBoss_ReopensTheGate_AndARematchWorks()
        {
            BuildArena();
            int engaged = 0, resets = 0;
            System.Action<string, string, int> onEngaged = (id, n, max) => engaged++;
            System.Action<string> onReset = id => resets++;
            BossEvents.Engaged += onEngaged;
            BossEvents.Reset += onReset;
            try
            {
                yield return RunIntoTheArena();
                Assert.IsTrue(_gate.activeSelf);
                _boss.TakeDamage(new DamageInfo(100, _boss.transform.position, Vector2.zero, RealmType.Echo));
                Assert.Less(_boss.CurrentHp, 600);

                _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
                yield return new WaitForSecondsRealtime(1.7f); // respawn at the start, outside the arena

                Assert.AreEqual(1, resets, "The boss resets when Kael dies");
                Assert.AreEqual(600, _boss.CurrentHp, "Full health again");
                Assert.IsFalse(_gate.activeSelf, "The way in is open again (no soft-lock)");
                Assert.IsFalse(_arena.FightStarted);
                Assert.That(_world.Player.transform.position.x, Is.LessThan(0f), "Kael is back at the start");

                yield return RunIntoTheArena();
                Assert.AreEqual(2, engaged, "The rematch engages again");
                Assert.IsTrue(_gate.activeSelf);
            }
            finally
            {
                BossEvents.Engaged -= onEngaged;
                BossEvents.Reset -= onReset;
            }
        }

        [UnityTest]
        public IEnumerator ABossAlreadyDefeatedInThisSave_DoesNotComeBack()
        {
            GameSession.MarkBossDefeated("sentinel_01");
            BuildArena();
            int engaged = 0;
            System.Action<string, string, int> onEngaged = (id, n, max) => engaged++;
            BossEvents.Engaged += onEngaged;
            try
            {
                yield return RunIntoTheArena();

                Assert.AreEqual(0, engaged, "No fight starts");
                Assert.IsFalse(_boss.gameObject.activeSelf, "The boss is gone");
                Assert.IsFalse(_gate.activeSelf, "The way stays open");
                Assert.IsTrue(_arena.IsDefeated);
            }
            finally { BossEvents.Engaged -= onEngaged; }
        }

        [UnityTest]
        public IEnumerator TheSentinelsLaser_DealsWholeDamageInTicks_NotPerFrame()
        {
            // Regression: damage-per-second * deltaTime rounds to 0, which only flickered invulnerability
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            yield return new WaitForSeconds(0.5f);
            int hp = _world.Stats.CurrentHealth;

            var hit = _world.Stats.TakeDamage(new DamageInfo(0, Vector2.zero, Vector2.zero, RealmType.Prime));
            Assert.AreEqual(hp, _world.Stats.CurrentHealth, "A zero-damage hit costs nothing");
            Assert.AreEqual(0, hit.DealtDamage);
            Assert.IsFalse(_world.Stats.IsInvulnerable, "and does not start invulnerability frames");
        }
    }

    public class SoundIntegrationTests
    {
        private TestWorld _world;
        private GameObject _audioGo;
        private AudioManager _audio;
        private readonly List<SfxGroup> _played = new List<SfxGroup>();

        [SetUp]
        public void SetUp()
        {
            _played.Clear();
            _audioGo = new GameObject("Audio");
            _audio = _audioGo.AddComponent<AudioManager>();
            foreach (SfxGroup group in System.Enum.GetValues(typeof(SfxGroup)))
                _audio.ConfigureGroup(group, new[] { AudioClip.Create(group.ToString(), 4410, 1, 44100, false) });
            _audio.GroupPlayed += _played.Add;
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            Object.Destroy(_audioGo);
            foreach (var st in Object.FindObjectsByType<ChronoStation>(FindObjectsSortMode.None)) Object.Destroy(st.gameObject);
            GameSession.Reset();
        }

        [UnityTest]
        public IEnumerator LandingFromAFall_PlaysASoftLandingSound()
        {
            _world = TestWorld.Create(new Vector2(0f, 3f)); // a short drop
            yield return new WaitForSeconds(1f);
            CollectionAssert.Contains(_played, SfxGroup.LandSoft);
        }

        [UnityTest]
        public IEnumerator LandingFromHigh_PlaysAHardLandingSound()
        {
            _world = TestWorld.Create(new Vector2(0f, 14f)); // long enough to build real fall speed
            yield return new WaitForSeconds(1.4f);
            CollectionAssert.Contains(_played, SfxGroup.LandHard);
        }

        [UnityTest]
        public IEnumerator Running_PlaysFootsteps_OfTheCurrentRealm()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            yield return new WaitForSeconds(0.6f);
            _played.Clear();

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(1f);
            _world.Input.Move = 0f;

            int steps = _played.FindAll(g => g == SfxGroup.FootstepPrime).Count;
            Assert.GreaterOrEqual(steps, 2, "Several footsteps while running");
            Assert.IsFalse(_played.Contains(SfxGroup.FootstepEcho), "Prime steps in Prime");
        }

        [UnityTest]
        public IEnumerator DamageAndDeath_PlayHurtAndDeathSounds()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            yield return new WaitForSeconds(0.5f);
            _played.Clear();

            _world.Stats.TakeDamage(new DamageInfo(10, Vector2.zero, Vector2.zero, RealmType.Prime));
            CollectionAssert.Contains(_played, SfxGroup.Hurt);

            yield return new WaitForSeconds(1.4f); // invulnerability over
            _world.Stats.TakeDamage(new DamageInfo(999, Vector2.zero, Vector2.zero, RealmType.Prime));
            CollectionAssert.Contains(_played, SfxGroup.Death);
        }

        [UnityTest]
        public IEnumerator UsingAStation_PlaysTheStationSound()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            var stationGo = new GameObject("Station");
            stationGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var station = stationGo.AddComponent<ChronoStation>();
            station.Configure("s", 25);
            yield return new WaitForSeconds(0.5f);

            station.Activate(_world.Controller);
            CollectionAssert.Contains(_played, SfxGroup.StationActivate);
        }

        [UnityTest]
        public IEnumerator LowHealth_StartsAHeartbeat_ThatSpeedsUpAsHealthFalls_AndStopsWhenHealed()
        {
            _world = TestWorld.Create(new Vector2(0f, 1.5f));
            var heartGo = new GameObject("Heart");
            var heart = heartGo.AddComponent<LowHealthAudio>();
            heart.Configure(AudioClip.Create("beat", 44100, 1, 44100, false));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(heart.IsBeating, "Healthy: silent");

            _world.Stats.RestoreProgress(24, 100);          // just under 25%
            Assert.IsTrue(heart.IsBeating);
            Assert.AreEqual(1f, heart.Pitch, 0.05f, "60 BPM at the threshold");

            _world.Stats.RestoreProgress(5, 100);           // nearly dead
            Assert.Greater(heart.Pitch, 1.8f, "Approaching 130 BPM");

            _world.Stats.RestoreProgress(80, 100);
            Assert.IsFalse(heart.IsBeating, "Healed: silent again");
            Object.Destroy(heartGo);
        }
    }

    public class MechanicsIntegrationTests
    {
        private TestWorld _world;
        private GameObject _manager;
        private GameObject _spikes;

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
            if (_spikes != null) Object.Destroy(_spikes);
            if (_manager != null) Object.Destroy(_manager);
        }

        private void BuildSpikes(float x)
        {
            _manager = new GameObject("RealityManager");
            _manager.AddComponent<RealityManager>();
            _world = TestWorld.Create(new Vector2(0f, 1.5f));

            _spikes = new GameObject("Spikes");
            _spikes.layer = LayerMask.NameToLayer("Hazard") >= 0 ? LayerMask.NameToLayer("Hazard") : 0;
            _spikes.transform.position = new Vector3(x, 0.3f, 0f);
            var box = _spikes.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2f, 0.6f);
            _spikes.AddComponent<Spikes>();
        }

        [UnityTest]
        public IEnumerator SpikesInPrime_SendKaelBackToSafeGround_WithoutLosingHealth()
        {
            BuildSpikes(6f);
            yield return new WaitForSeconds(0.6f); // grounded at x = 0: the safe spot
            int hp = _world.Stats.CurrentHealth;

            _world.Input.Move = 1f;
            yield return new WaitForSeconds(0.9f);   // runs into the spikes at x = 5
            _world.Input.Move = 0f;
            yield return new WaitForSeconds(0.8f);   // soft respawn (0.3 s) has happened

            Assert.AreEqual(hp, _world.Stats.CurrentHealth, "Soft respawn costs no health (spec D9)");
            Assert.Less(_world.Player.transform.position.x, 4f, "Kael was put back on safe ground, before the spikes");
            Assert.IsTrue(_world.Controller.enabled);
        }

        [UnityTest]
        public IEnumerator SpikesInEcho_BounceKaelUp_InsteadOfHurtingHim()
        {
            BuildSpikes(0f);
            yield return new WaitForSeconds(0.2f);
            RealityManager.Instance.SwitchRealm(RealmType.Echo); // spikes turn into a bounce pad
            _world.Player.transform.position = new Vector2(0f, 3f);
            _world.Rb.linearVelocity = Vector2.zero;

            float peak = 0f;
            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, _world.Rb.linearVelocity.y);
            }

            Assert.Greater(peak, 10f, "Kael is launched upward by the Echo-realm spikes");
            Assert.IsFalse(_world.Stats.IsDead);
        }
    }
}
