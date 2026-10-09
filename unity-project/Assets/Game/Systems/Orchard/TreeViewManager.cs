using System.Collections.Generic;
using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.Orchard
{
    /// <summary>Spawns one TreeView per planted tree on a 4-per-row orchard grid.</summary>
    public class TreeViewManager : MonoBehaviour
    {
        public Transform orchardOrigin;
        public float spacing = 4f;
        public int perRow = 4;

        private readonly Dictionary<int, TreeView> _views = new Dictionary<int, TreeView>();

        private void Start()
        {
            GameEvents.OrchardChanged += Sync;
            Sync();
        }

        private void OnDestroy()
        {
            GameEvents.OrchardChanged -= Sync;
        }

        public void Sync()
        {
            if (!ServiceLocator.TryGet(out OrchardService orchard)) return;
            var seen = new HashSet<int>();
            foreach (var tree in orchard.Trees)
            {
                seen.Add(tree.SpotIndex);
                if (!_views.ContainsKey(tree.SpotIndex))
                    _views[tree.SpotIndex] = SpawnView(tree);
            }
            var dead = new List<int>();
            foreach (var spot in _views.Keys)
                if (!seen.Contains(spot)) dead.Add(spot);
            foreach (var spot in dead)
            {
                if (_views[spot] != null) Destroy(_views[spot].gameObject);
                _views.Remove(spot);
            }
        }

        private TreeView SpawnView(TreeInstance tree)
        {
            Vector3 origin = orchardOrigin != null ? orchardOrigin.position : Vector3.zero;
            Vector3 pos = origin + new Vector3(
                (tree.SpotIndex % perRow) * spacing, 0f,
                (tree.SpotIndex / perRow) * spacing);

            GameObject go;
            if (tree.Data.prefab != null)
            {
                go = Instantiate(tree.Data.prefab, pos, Quaternion.identity, transform);
            }
            else
            {
                // Placeholder trunk + canopy.
                go = new GameObject($"Tree_{tree.Data.treeId}");
                go.transform.position = pos;
                go.transform.SetParent(transform);
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(go.transform, false);
                trunk.transform.localScale = new Vector3(0.4f, 2f, 0.4f);
                trunk.transform.localPosition = Vector3.up * 1f;
                var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy.transform.SetParent(go.transform, false);
                canopy.transform.localScale = new Vector3(2.4f, 2f, 2.4f);
                canopy.transform.localPosition = Vector3.up * 3f;
                canopy.GetComponent<Renderer>().material.color = new Color(0.25f, 0.6f, 0.3f);
            }
            var view = go.AddComponent<TreeView>();
            view.fruitIndicator = CreateFruitIndicator(go);
            view.Bind(tree);
            return view;
        }

        private static GameObject CreateFruitIndicator(GameObject parent)
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "FruitIndicator";
            indicator.transform.SetParent(parent.transform, false);
            indicator.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            indicator.transform.localPosition = Vector3.up * 4.2f;
            indicator.GetComponent<Renderer>().material.color = Color.red;
            indicator.SetActive(false);
            return indicator;
        }
    }
}
