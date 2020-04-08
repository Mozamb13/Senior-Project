using UnityEngine.Audio;
using System;
using UnityEngine;

public class audiomanager : MonoBehaviour
{
    public Sound[] sounds;

    public static audiomanager instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
        
    }

    void Start ()
    {
        Play("Music");
    }

    public void Play (string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.Play();
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }

/*
if anim state changes
s.source.Stop();
*/

    /*
    when referencing this to play a sound at an event
    Write in the script that causes the thing to happen
    FindObjectOfType<AudioManager>().Play("nameofaudioclip");
    */
}
