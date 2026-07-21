using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Audio
{
    public class RTSAudioManager : MonoBehaviour
    {
        public static RTSAudioManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField] private List<AudioClip> ambientMusic = new List<AudioClip>();
        [SerializeField] private List<AudioClip> combatMusic = new List<AudioClip>();
        [SerializeField] private float musicFadeSpeed = 1.0f;
        [SerializeField] [Range(0f, 1f)] private float targetMusicVolume = 0.4f;

        [Header("SFX Library")]
        [SerializeField] private AudioClip buildingPlacedSFX;
        [SerializeField] private AudioClip buildingCompletedSFX;
        [SerializeField] private AudioClip unitTrainedSFX;
        [SerializeField] private AudioClip rangedAttackSFX;
        [SerializeField] private AudioClip meleeAttackSFX;
        [SerializeField] private AudioClip unitDeathSFX;
        [SerializeField] private AudioClip buildingDeathSFX;

        [Header("SFX Pooling")]
        [SerializeField] private int sfxPoolSize = 16;

        private AudioSource voiceSource;
        private AudioSource musicSource1;
        private AudioSource musicSource2;

        private AudioSource activeMusicSource;
        private AudioSource inactiveMusicSource;

        private List<AudioSource> sfxPool = new List<AudioSource>();
        private List<AudioClip> currentPlaylist;
        private int currentTrackIndex = 0;
        private bool isPlayingMusic = false;

        // Combat Music State
        private bool isInCombat = false;
        private float combatTimer = 0f;
        private float combatDuration = 10f; // 10 seconds of peace needed to return to ambient music

        // Voice Command Cooldown
        private float lastCommandVoiceTime = -10f;
        private float commandVoiceCooldown = 1.5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Voice channel (direct 2D radio responses)
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.spatialBlend = 0.0f;
                voiceSource.playOnAwake = false;

                // Two music channels for crossfading
                musicSource1 = gameObject.AddComponent<AudioSource>();
                musicSource2 = gameObject.AddComponent<AudioSource>();
                musicSource1.spatialBlend = 0.0f;
                musicSource2.spatialBlend = 0.0f;
                musicSource1.loop = false;
                musicSource2.loop = false;
                musicSource1.playOnAwake = false;
                musicSource2.playOnAwake = false;

                activeMusicSource = musicSource1;
                inactiveMusicSource = musicSource2;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializePool();

            // Start playing ambient playlist by default
            if (ambientMusic != null && ambientMusic.Count > 0)
            {
                isPlayingMusic = true;
                currentPlaylist = ambientMusic;
                currentTrackIndex = Random.Range(0, ambientMusic.Count);
                
                activeMusicSource.clip = currentPlaylist[currentTrackIndex];
                activeMusicSource.volume = targetMusicVolume;
                activeMusicSource.Play();
            }
        }

        private void Update()
        {
            // 1. Update BGM crossfade volumes
            float fadeDelta = Time.unscaledDeltaTime * musicFadeSpeed;
            
            if (activeMusicSource != null)
            {
                activeMusicSource.volume = Mathf.MoveTowards(activeMusicSource.volume, targetMusicVolume, fadeDelta);
                
                // If the track finishes naturally, cycle to the next in the current playlist
                if (!activeMusicSource.isPlaying && isPlayingMusic && activeMusicSource.clip != null)
                {
                    PlayNextMusicTrack();
                }
            }

            if (inactiveMusicSource != null)
            {
                inactiveMusicSource.volume = Mathf.MoveTowards(inactiveMusicSource.volume, 0f, fadeDelta);
                if (inactiveMusicSource.volume == 0f && inactiveMusicSource.isPlaying)
                {
                    inactiveMusicSource.Stop();
                }
            }

            // 2. Update combat timer state
            if (isInCombat)
            {
                combatTimer -= Time.unscaledDeltaTime;
                if (combatTimer <= 0f)
                {
                    isInCombat = false;
                    TransitionToPlaylist(ambientMusic);
                }
            }
        }

        // --- Music Management ---

        private void PlayMusicTrack(AudioClip clip)
        {
            if (clip == null) return;

            // Swap active and inactive sources
            AudioSource temp = activeMusicSource;
            activeMusicSource = inactiveMusicSource;
            inactiveMusicSource = temp;

            activeMusicSource.clip = clip;
            activeMusicSource.volume = 0f;
            activeMusicSource.Play();
        }

        private void PlayNextMusicTrack()
        {
            if (currentPlaylist == null || currentPlaylist.Count == 0) return;
            currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Count;
            PlayMusicTrack(currentPlaylist[currentTrackIndex]);
        }

        private void TransitionToPlaylist(List<AudioClip> playlist)
        {
            if (playlist == null || playlist.Count == 0) return;
            if (currentPlaylist == playlist) return;

            currentPlaylist = playlist;
            currentTrackIndex = Random.Range(0, playlist.Count);
            PlayMusicTrack(currentPlaylist[currentTrackIndex]);
        }

        public void NotifyCombatEvent()
        {
            combatTimer = combatDuration;
            if (!isInCombat)
            {
                isInCombat = true;
                TransitionToPlaylist(combatMusic);
            }
        }

        // --- Voice Line Management (RTS Voice Rules) ---

        public void PlayVoice(AudioClip clip, bool isCommand = false)
        {
            if (clip == null || voiceSource == null) return;

            if (isCommand)
            {
                // Prevent unit response voice spam on fast clicks
                if (Time.time - lastCommandVoiceTime < commandVoiceCooldown)
                {
                    return;
                }
                lastCommandVoiceTime = Time.time;
            }

            // Interrupt any previous selection or command line instantly
            if (voiceSource.isPlaying)
            {
                voiceSource.Stop();
            }

            voiceSource.clip = clip;
            voiceSource.Play();
        }

        public void PlayVoice(IReadOnlyList<AudioClip> clips, bool isCommand = false)
        {
            if (clips == null || clips.Count == 0) return;
            PlayVoice(clips[Random.Range(0, clips.Count)], isCommand);
        }

        // --- SFX Pooling System ---

        private void InitializePool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject obj = new GameObject($"SFX_Source_{i}", typeof(AudioSource));
                obj.transform.SetParent(transform);
                AudioSource src = obj.GetComponent<AudioSource>();
                src.playOnAwake = false;
                sfxPool.Add(src);
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (!sfxPool[i].isPlaying)
                {
                    return sfxPool[i];
                }
            }

            // Expand pool if necessary
            GameObject obj = new GameObject($"SFX_Source_{sfxPool.Count}", typeof(AudioSource));
            obj.transform.SetParent(transform);
            AudioSource src = obj.GetComponent<AudioSource>();
            src.playOnAwake = false;
            sfxPool.Add(src);
            return src;
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource src = GetAvailableSFXSource();
            src.gameObject.transform.position = position;
            src.spatialBlend = 1.0f; // 3D Spatial sound
            src.minDistance = 4f;
            src.maxDistance = 40f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.clip = clip;
            src.volume = volume;
            src.Play();
        }

        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource src = GetAvailableSFXSource();
            src.spatialBlend = 0.0f; // 2D Stereo Sound
            src.clip = clip;
            src.volume = volume;
            src.Play();
        }

        // --- Specialized Event Callbacks ---

        public void PlayBuildingPlacedSound(Vector3 position)
        {
            PlaySFX(buildingPlacedSFX, position, 0.8f);
        }

        public void PlayBuildingCompletedSound(Vector3 position)
        {
            PlaySFX(buildingCompletedSFX, position, 0.9f);
        }

        public void PlayUnitTrainedSound(Vector3 position)
        {
            PlaySFX(unitTrainedSFX, position, 0.7f);
        }

        public void PlayWeaponFireSound(Vector3 position, bool isRanged)
        {
            PlaySFX(isRanged ? rangedAttackSFX : meleeAttackSFX, position, 0.6f);
        }

        public void PlayDeathSound(Vector3 position, bool isUnit)
        {
            PlaySFX(isUnit ? unitDeathSFX : buildingDeathSFX, position, 0.8f);
        }
    }
}
