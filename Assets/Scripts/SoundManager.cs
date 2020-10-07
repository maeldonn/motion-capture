using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundManager
{

    private static GameObject oneShotGameObject;
    private static AudioSource oneShotAudioSource;

    public static void PlaySound(AudioClip clip)
    {
        if(oneShotGameObject == null)
        {
            oneShotGameObject = new GameObject("Sound");
            oneShotAudioSource = oneShotGameObject.AddComponent<AudioSource>();
        }
        oneShotAudioSource.PlayOneShot(clip);
    }

    public static void PlaySound(AudioClip clip, Vector3 position)
    {
        GameObject soundGameObject = new GameObject("Sound");
        soundGameObject.transform.position = position;
        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
        Object.Destroy(soundGameObject, clip.length);
    }
}
