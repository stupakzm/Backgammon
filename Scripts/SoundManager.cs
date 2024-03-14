using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour {

    [SerializeField] private SoundAudioClip[] SoundAudioClips;
    [SerializeField] private Slider VolumeSlider;

    private void Start() {
        if (!PlayerPrefs.HasKey(ConstantStrings.VOLUME)) {
            PlayerPrefs.SetFloat(ConstantStrings.VOLUME, 0.7f);
            LoadVolume();
        }
        else {
            LoadVolume();
        }
    }

    public void PlaySoundOnce(Sound sound) {
        foreach (SoundAudioClip SoundAudioClip in SoundAudioClips) {
            if (SoundAudioClip.sound == sound) {
                int randomClip = Random.Range(0, SoundAudioClip.audioClip.Length);
                SoundAudioClip.audioSource.PlayOneShot(SoundAudioClip.audioClip[randomClip]);
            }
        }
    }


    public void StopPlayingSound(Sound sound) {
        foreach (SoundAudioClip SoundAudioClip in SoundAudioClips) {
            if (SoundAudioClip.sound == sound) {
                 SoundAudioClip.audioSource.Stop();
            }
        }
    }

    public void ChangeVolume() {
        AudioListener.volume = VolumeSlider.value;
        SaveVolume();
    }

    private void LoadVolume() {
        VolumeSlider.value = PlayerPrefs.GetFloat(ConstantStrings.VOLUME);
    }

    private void SaveVolume() {
        PlayerPrefs.SetFloat(ConstantStrings.VOLUME, VolumeSlider.value);
    }
}

public enum Sound {
    Dice,
    ChipMove,
    Background
}

