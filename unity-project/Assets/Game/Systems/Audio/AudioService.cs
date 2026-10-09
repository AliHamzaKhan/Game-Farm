using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Audio
{
    /// <summary>
    /// Simple audio hub (§48): SFX by id, music/ambience tracks, volume prefs.
    /// Clips are assigned in the inspector or via Resources in later phases.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        [Header("Assign in inspector (Phase 1 set)")]
        public UnityEngine.AudioClip buttonSound;
        public UnityEngine.AudioClip harvestSound;
        public UnityEngine.AudioClip coinSound;
        public UnityEngine.AudioClip levelUpSound;
        public UnityEngine.AudioClip plantSound;
        public UnityEngine.AudioClip waterSound;

        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        private UnityEngine.AudioSource _sfxSource;
        private UnityEngine.AudioSource _musicSource;
        private readonly Dictionary<string, UnityEngine.AudioClip> _clips = new Dictionary<string, UnityEngine.AudioClip>();

        private void Awake()
        {
            _sfxSource = gameObject.AddComponent<UnityEngine.AudioSource>();
            _musicSource = gameObject.AddComponent<UnityEngine.AudioSource>();
            _musicSource.loop = true;
            Register("button", buttonSound); Register("harvest", harvestSound);
            Register("coin", coinSound); Register("levelup", levelUpSound);
            Register("plant", plantSound); Register("water", waterSound);
        }

        private void Register(string id, UnityEngine.AudioClip clip)
        {
            if (clip != null) _clips[id] = clip;
        }

        public void Play(string clipId)
        {
            if (_clips.TryGetValue(clipId, out var clip) && clip != null)
                _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayMusic(UnityEngine.AudioClip track)
        {
            if (track == null || _musicSource.clip == track) return;
            _musicSource.clip = track;
            _musicSource.volume = musicVolume;
            _musicSource.Play();
        }

        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat("sfx_vol", sfxVolume); }
        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            _musicSource.volume = musicVolume;
            PlayerPrefs.SetFloat("music_vol", musicVolume);
        }
    }
}
