using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour {

    public Slider volume_Slider;
    public AudioSource audio_Source;

    private void Start()
    {
        volume_Slider.value = PlayerPrefs.GetFloat("VOLUME", 1f);
    }

    public void ChangeVolume() {
        AudioListener.volume = volume_Slider.value;
        PlayerPrefs.SetFloat("VOLUME", volume_Slider.value);
    }
}