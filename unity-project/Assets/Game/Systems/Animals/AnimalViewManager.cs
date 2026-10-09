using System.Collections.Generic;
using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    /// <summary>
    /// Spawns/keeps one AnimalView per owned animal, arranged near a pen origin.
    /// Art-free fallback: capsule placeholder if the animal prefab is missing.
    /// </summary>
    public class AnimalViewManager : MonoBehaviour
    {
        public Transform penOrigin;
        public float spacing = 2.5f;
        public int perRow = 4;

        private readonly Dictionary<string, AnimalView> _views = new Dictionary<string, AnimalView>();

        private void Start()
        {
            GameEvents.AnimalsChanged += Sync;
            GameEvents.AnimalBought += id => Sync();
            Sync();
        }

        private void OnDestroy()
        {
            GameEvents.AnimalsChanged -= Sync;
            GameEvents.AnimalBought -= id => Sync();
        }

        private void Sync()
        {
            if (!ServiceLocator.TryGet(out AnimalService animals)) return;
            var seen = new HashSet<string>();
            int i = 0;
            foreach (var animal in animals.Animals)
            {
                seen.Add(animal.InstanceId);
                if (!_views.ContainsKey(animal.InstanceId))
                    _views[animal.InstanceId] = SpawnView(animal, i);
                i++;
            }
            // Remove views for sold/removed animals (future).
            var dead = new List<string>();
            foreach (var id in _views.Keys)
                if (!seen.Contains(id)) dead.Add(id);
            foreach (var id in dead)
            {
                if (_views[id] != null) Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        private AnimalView SpawnView(AnimalInstance animal, int index)
        {
            Vector3 pos = penOrigin != null ? penOrigin.position : Vector3.zero;
            pos += new Vector3((index % perRow) * spacing, 0f, (index / perRow) * spacing);

            GameObject go;
            if (animal.Data.prefab != null)
            {
                go = Instantiate(animal.Data.prefab, pos, Quaternion.identity, transform);
            }
            else
            {
                // Placeholder: capsule body so the system is testable without art.
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.position = pos + Vector3.up * 0.5f;
                go.transform.SetParent(transform);
            }
            var view = go.AddComponent<AnimalView>();
            view.Bind(animal);
            return view;
        }
    }
}
