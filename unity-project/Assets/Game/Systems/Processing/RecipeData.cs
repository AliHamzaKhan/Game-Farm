using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Processing
{
    [Serializable]
    public class RecipeIngredient
    {
        public string itemId;
        public int count = 1;
    }

    /// <summary>Data-driven production recipe (§20): inputs → machine → product.</summary>
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "FarmQuest/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        public string recipeId = "flour";
        public string displayName = "Flour";
        public string buildingId = "mill";
        public int unlockLevel = 18;
        public List<RecipeIngredient> inputs = new List<RecipeIngredient>();
        public string outputItemId = "flour";
        public int outputCount = 2;
        [Tooltip("Real seconds per batch.")]
        public float durationSeconds = 300f;
        public int xpReward = 10;
        public Sprite icon;
    }
}
