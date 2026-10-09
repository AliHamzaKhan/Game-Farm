using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;
using UnityEngine;

namespace FarmQuest.Systems.Wildlife
{
    /// <summary>
    /// Ambient wildlife (§26): butterflies, birds, and a rare hedgehog.
    /// Pure atmosphere — the hedgehog leaves a tiny gift. Placeholder visuals
    /// until the art pass; behavior is what matters.
    /// </summary>
    public class WildlifeService : MonoBehaviour
    {
        public float minSpawnDelay = 20f;
        public float maxSpawnDelay = 40f;
        public float critterLifetime = 30f;
        public Vector2 spawnArea = new Vector2(30f, 30f);

        private float _nextSpawn;

        private void Start()
        {
            _nextSpawn = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
        }

        private void Update()
        {
            if (Time.time < _nextSpawn) return;
            _nextSpawn = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
            SpawnCritter();
        }

        private void SpawnCritter()
        {
            float roll = Random.value;
            if (roll < 0.65f) Spawn("🦋 Butterfly", new Color(1f, 0.7f, 0.9f), 0.3f, true);
            else if (roll < 0.95f) Spawn("🐦 Bird", new Color(0.5f, 0.7f, 1f), 0.4f, true);
            else SpawnHedgehog();
        }

        private void Spawn(string name, Color color, float size, bool flies)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var pos = new Vector3(
                Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                flies ? Random.Range(2f, 5f) : 0.3f,
                Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f));
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<Renderer>().material.color = color;
            go.AddComponent<CritterWander>().lifetime = critterLifetime;
        }

        private void SpawnHedgehog()
        {
            Spawn("🦔 Hedgehog", new Color(0.5f, 0.35f, 0.2f), 0.5f, false);
            if (ServiceLocator.TryGet(out EconomyService economy))
            {
                economy.AddCoins(20, "hedgehog");
                GameEvents.RaiseToast("🦔 A hedgehog visited! It left you 20 coins!");
            }
        }

        /// <summary>Gentle wandering for ambient critters.</summary>
        private class CritterWander : MonoBehaviour
        {
            public float lifetime = 30f;
            private float _born;
            private Vector3 _dir;

            private void Start()
            {
                _born = Time.time;
                _dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            }

            private void Update()
            {
                if (Time.time - _born > lifetime) { Destroy(gameObject); return; }
                if (Random.value < 0.01f)
                    _dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                transform.position += _dir * Time.deltaTime * 1.5f;
                transform.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.3f * Time.deltaTime;
            }
        }
    }
}
