using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveAudio : MonoBehaviour
{
    public AudioSource audio;

    void OnTriggerEnter(Collider other){
        if(other.tag == "Player"){
            audio.Play();
        }
    }
}
