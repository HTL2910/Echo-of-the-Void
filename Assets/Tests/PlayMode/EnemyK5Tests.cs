using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using EchoOfTheVoid.Combat;
using EchoOfTheVoid.Core;
using EchoOfTheVoid.Enemies;

namespace EchoOfTheVoid.Tests
{
    public class RiftKnightTestTarget : MonoBehaviour, IDamageable
    {
        public RealmType EntityRealm => RealmType.Prime;
        public int MaxDamage { get; private set; }

        public HitFeedback TakeDamage(DamageInfo info)
        {
            MaxDamage = Mathf.Max(MaxDamage, info.Amount);
            return new HitFeedback(false, info.Amount, false);
        }
    }

    /// <summary>K5: Behaviour tests for the heavy Rift Knight enemy.</summary>
    public class EnemyK5Tests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Shield_BlocksAllDamageFromTheFront()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();

            var attacker = new GameObject("Attacker");
            attacker.transform.position = Vector3.right;
            yield return null;

            int healthBefore = knight.CurrentHealth;
            HitFeedback feedback = knight.TakeDamage(new DamageInfo(
                30,
                knightObject.transform.position,
                Vector2.zero,
                RealmType.Prime,
                attacker: attacker));

            Assert.IsTrue(feedback.IsDeflected, "The shield should report a deflection");
            Assert.AreEqual(0, feedback.DealtDamage, "A frontal attack must deal no damage");
            Assert.AreEqual(healthBefore, knight.CurrentHealth, "A frontal attack must not reduce health");
        }

        [UnityTest]
        public IEnumerator AttackFromBehind_DealsDamage()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();

            var attacker = new GameObject("Attacker");
            attacker.transform.position = Vector3.left;
            yield return null;

            int healthBefore = knight.CurrentHealth;
            HitFeedback feedback = knight.TakeDamage(new DamageInfo(
                30,
                knightObject.transform.position,
                Vector2.zero,
                RealmType.Prime,
                attacker: attacker));

            Assert.IsFalse(feedback.IsDeflected, "An attack behind the shield should connect");
            Assert.AreEqual(30, feedback.DealtDamage);
            Assert.AreEqual(healthBefore - 30, knight.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator ThreeHitsFromBehind_SwitchRealm()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();

            var attacker = new GameObject("Attacker");
            attacker.transform.position = Vector3.left;
            yield return null;

            RealmType initialRealm = knight.EntityRealm;
            for (int i = 0; i < 3; i++)
            {
                knight.TakeDamage(new DamageInfo(
                    5,
                    knightObject.transform.position,
                    Vector2.zero,
                    knight.EntityRealm,
                    attacker: attacker));
            }

            Assert.AreNotEqual(initialRealm, knight.EntityRealm,
                "Three attacks that connect behind the shield must switch the knight's realm");
        }

        [UnityTest]
        public IEnumerator FourSecondsWithoutHits_SwitchRealm()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();
            yield return null;

            RealmType initialRealm = knight.EntityRealm;
            yield return new WaitForSeconds(4.1f);

            Assert.AreNotEqual(initialRealm, knight.EntityRealm,
                "The knight must switch realm after four seconds even when it is not attacked");
        }

        [UnityTest]
        public IEnumerator ResetEnemyState_RestoresPrimeRealmAndHitCounter()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();
            var attacker = new GameObject("Attacker");
            attacker.transform.position = Vector3.left;
            yield return null;

            for (int i = 0; i < 3; i++)
                knight.TakeDamage(new DamageInfo(5, knightObject.transform.position, Vector2.zero,
                    knight.EntityRealm, attacker: attacker));
            Assert.AreEqual(RealmType.Echo, knight.EntityRealm);

            knight.ResetEnemyState();
            for (int i = 0; i < 2; i++)
                knight.TakeDamage(new DamageInfo(5, knightObject.transform.position, Vector2.zero,
                    knight.EntityRealm, attacker: attacker));

            Assert.AreEqual(RealmType.Prime, knight.EntityRealm,
                "Reset must restore Prime and clear the previous hit count");
        }

        [UnityTest]
        public IEnumerator PlayerOnTheLeft_TurnsShieldLeft()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            var knight = knightObject.AddComponent<RiftKnight>();

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = Vector3.left * 3f;
            yield return null;
            yield return null;

            Assert.AreEqual(-1f, knight.FacingDirection,
                "The shield must face the detected player");
        }

        [UnityTest]
        public IEnumerator PlayerInDetectionRange_MovesTowardPlayer()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            knightObject.AddComponent<RiftKnight>();

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = Vector3.right * 5f;
            yield return null;
            yield return new WaitForFixedUpdate();

            Rigidbody2D body = knightObject.GetComponent<Rigidbody2D>();
            Assert.Greater(body.linearVelocity.x, 0f,
                "A detected player on the right should make the knight advance right");
        }

        [UnityTest]
        public IEnumerator PlayerInSlashRange_ReceivesTwentyFiveDamage()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            knightObject.AddComponent<RiftKnight>();

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = Vector3.right;
            player.AddComponent<BoxCollider2D>();
            var target = player.AddComponent<RiftKnightTestTarget>();

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(25, target.MaxDamage,
                "A close player should receive the configured horizontal slash damage");
        }

        [UnityTest]
        public IEnumerator PlayerInStompRange_ReceivesThirtyFiveDamage()
        {
            var knightObject = new GameObject("RiftKnight");
            knightObject.AddComponent<BoxCollider2D>();
            knightObject.AddComponent<RiftKnight>();

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = Vector3.right * 3f;
            player.AddComponent<BoxCollider2D>();
            var target = player.AddComponent<RiftKnightTestTarget>();

            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual(35, target.MaxDamage,
                "A player in mid range should receive the configured stomp shockwave damage");
        }

        [Test]
        public void Prefab_UsesRiftKnightDataAndVisualChild()
        {
            const string path = "Assets/Prefabs/Enemies/RiftKnight.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "The K5 prefab builder must create RiftKnight.prefab");

            Assert.AreEqual(Vector3.one, prefab.transform.localScale);
            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), prefab.layer);
            Assert.IsNotNull(prefab.transform.Find("Visual"));
            Assert.IsNotNull(prefab.GetComponent<RiftKnight>());

            var serializedKnight = new SerializedObject(prefab.GetComponent<RiftKnight>());
            var data = serializedKnight.FindProperty("enemyData").objectReferenceValue as RiftKnightDataSO;
            Assert.IsNotNull(data, "The prefab must reference RiftKnightDataSO");
            Assert.AreEqual(130, data.MaxHealth);
            Assert.AreEqual(3.8f, data.MoveSpeed, 0.001f);
        }
    }
}
