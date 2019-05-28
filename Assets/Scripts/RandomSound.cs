using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSound : MonoBehaviour
{
    AudioSource source;
    public AudioClip[] clips;

    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
        int idx = Random.Range(0, clips.Length);
        source.clip = clips[idx];
        source.Play();
    }
}
