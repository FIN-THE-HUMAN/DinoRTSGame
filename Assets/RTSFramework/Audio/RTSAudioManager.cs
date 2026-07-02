using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Audio
{
    public class RTSAudioManager : MonoBehaviour
    {
        public static RTSAudioManager Instance { get; private set; }

        private AudioSource voiceSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                voiceSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlayVoice(AudioClip clip)
        {
            if (clip == null || voiceSource == null) return;

            // Stop any currently playing voice line to prevent overlapping clutter
            if (voiceSource.isPlaying)
            {
                voiceSource.Stop();
            }

            voiceSource.PlayOneShot(clip);
        }

        public void PlayVoice(IReadOnlyList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0) return;
            PlayVoice(clips[Random.Range(0, clips.Count)]);
        }
    }
}
