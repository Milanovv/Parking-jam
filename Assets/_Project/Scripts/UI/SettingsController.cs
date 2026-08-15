using UnityEngine;

public class SettingsController : MonoBehaviour
{
    public const string SfxVolumeKey = "ParkingJam.SFXVolume";

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
    }
}
