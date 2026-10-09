using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    /// <summary>Registry for animal/pet/building/decor data assets.</summary>
    [CreateAssetMenu(fileName = "AnimalDatabase", menuName = "FarmQuest/Animal Database")]
    public class AnimalDatabase : ScriptableObject
    {
        public List<AnimalData> animals = new List<AnimalData>();
        public List<PetData> pets = new List<PetData>();
        public List<Buildings.BuildingData> buildings = new List<Buildings.BuildingData>();
        public List<Decor.DecorData> decorations = new List<Decor.DecorData>();

        private Dictionary<string, AnimalData> _animalMap;
        private Dictionary<string, PetData> _petMap;
        private Dictionary<string, Buildings.BuildingData> _buildingMap;
        private Dictionary<string, Decor.DecorData> _decorMap;

        public void Initialize()
        {
            _animalMap = new Dictionary<string, AnimalData>();
            foreach (var a in animals)
                if (a != null && !_animalMap.ContainsKey(a.animalId)) _animalMap.Add(a.animalId, a);
            _petMap = new Dictionary<string, PetData>();
            foreach (var p in pets)
                if (p != null && !_petMap.ContainsKey(p.petId)) _petMap.Add(p.petId, p);
            _buildingMap = new Dictionary<string, Buildings.BuildingData>();
            foreach (var b in buildings)
                if (b != null && !_buildingMap.ContainsKey(b.buildingId)) _buildingMap.Add(b.buildingId, b);
            _decorMap = new Dictionary<string, Decor.DecorData>();
            foreach (var d in decorations)
                if (d != null && !_decorMap.ContainsKey(d.decorId)) _decorMap.Add(d.decorId, d);
        }

        public AnimalData GetAnimal(string id)
        {
            if (_animalMap == null) Initialize();
            _animalMap.TryGetValue(id, out var data);
            return data;
        }

        public PetData GetPet(string id)
        {
            if (_petMap == null) Initialize();
            _petMap.TryGetValue(id, out var data);
            return data;
        }

        public Buildings.BuildingData GetBuilding(string id)
        {
            if (_buildingMap == null) Initialize();
            _buildingMap.TryGetValue(id, out var data);
            return data;
        }

        public Decor.DecorData GetDecor(string id)
        {
            if (_decorMap == null) Initialize();
            _decorMap.TryGetValue(id, out var data);
            return data;
        }
    }
}
