using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Machines
{
    /// <summary>Registry for equipment + orchard tree data.</summary>
    [CreateAssetMenu(fileName = "MachineDatabase", menuName = "FarmQuest/Machine Database")]
    public class MachineDatabase : ScriptableObject
    {
        public List<EquipmentData> equipment = new List<EquipmentData>();
        public List<Orchard.TreeData> trees = new List<Orchard.TreeData>();
        public List<Processing.RecipeData> recipes = new List<Processing.RecipeData>();

        private Dictionary<string, EquipmentData> _equipmentMap;
        private Dictionary<string, Orchard.TreeData> _treeMap;
        private Dictionary<string, Processing.RecipeData> _recipeMap;

        public void Initialize()
        {
            _equipmentMap = new Dictionary<string, EquipmentData>();
            foreach (var e in equipment)
                if (e != null && !_equipmentMap.ContainsKey(e.equipmentId)) _equipmentMap.Add(e.equipmentId, e);
            _treeMap = new Dictionary<string, Orchard.TreeData>();
            foreach (var t in trees)
                if (t != null && !_treeMap.ContainsKey(t.treeId)) _treeMap.Add(t.treeId, t);
            _recipeMap = new Dictionary<string, Processing.RecipeData>();
            foreach (var r in recipes)
                if (r != null && !_recipeMap.ContainsKey(r.recipeId)) _recipeMap.Add(r.recipeId, r);
        }

        public Processing.RecipeData GetRecipe(string id)
        {
            if (_recipeMap == null) Initialize();
            _recipeMap.TryGetValue(id, out var data);
            return data;
        }

        public EquipmentData GetEquipment(string id)
        {
            if (_equipmentMap == null) Initialize();
            _equipmentMap.TryGetValue(id, out var data);
            return data;
        }

        public Orchard.TreeData GetTree(string id)
        {
            if (_treeMap == null) Initialize();
            _treeMap.TryGetValue(id, out var data);
            return data;
        }
    }
}
