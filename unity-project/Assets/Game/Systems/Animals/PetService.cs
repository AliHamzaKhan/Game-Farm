using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Animals
{
    /// <summary>
    /// Pet companionship (§22): unlock, name, choose active pet. Pets give
    /// love, not coins. Cat auto-unlocks at level 14; dog is Grandpa's gift.
    /// </summary>
    public class PetService
    {
        private readonly AnimalDatabase _database;
        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public string ActivePetId { get; private set; }
        public string PetName { get; private set; } = "";

        public PetService(AnimalDatabase database)
        {
            _database = database;
            GameEvents.LevelUp += OnLevelUp;
        }

        private void OnLevelUp(int level)
        {
            foreach (var pet in _database.pets)
            {
                if (pet != null && pet.unlockLevel > 0 && pet.unlockLevel <= level
                    && !_unlocked.Contains(pet.petId))
                {
                    UnlockPet(pet.petId);
                }
            }
        }

        public bool UnlockPet(string petId)
        {
            var pet = _database.GetPet(petId);
            if (pet == null || _unlocked.Contains(petId)) return false;
            _unlocked.Add(petId);
            if (string.IsNullOrEmpty(ActivePetId)) ActivePetId = petId;
            GameEvents.RaisePetUnlocked(petId);
            GameEvents.RaiseToast($"🐾 {pet.displayName} joined your farm!");
            return true;
        }

        public bool IsUnlocked(string petId) => _unlocked.Contains(petId);
        public IEnumerable<PetData> UnlockedPets()
        {
            foreach (var id in _unlocked)
            {
                var pet = _database.GetPet(id);
                if (pet != null) yield return pet;
            }
        }

        public void SetActivePet(string petId)
        {
            if (_unlocked.Contains(petId)) ActivePetId = petId;
        }

        public void SetPetName(string name)
        {
            PetName = (name ?? "").Trim();
            if (PetName.Length > 16) PetName = PetName.Substring(0, 16);
        }

        public PetData ActivePet() =>
            string.IsNullOrEmpty(ActivePetId) ? null : _database.GetPet(ActivePetId);

        // ---------- save ----------
        public PetSaveData CaptureState()
        {
            return new PetSaveData
            {
                unlocked = new List<string>(_unlocked),
                activePetId = ActivePetId,
                petName = PetName
            };
        }

        public void RestoreState(PetSaveData data)
        {
            _unlocked.Clear();
            ActivePetId = null;
            PetName = "";
            if (data == null) return;
            foreach (var id in data.unlocked) _unlocked.Add(id);
            ActivePetId = data.activePetId;
            PetName = data.petName ?? "";
        }
    }
}
