using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Settings", menuName ="Settings/Game Settings")]
public class GameSettings : ScriptableObject
{
    public bool mute;
    private float volume;
    bool initialized = false;

    public void Initialize()
    {
        if(initialized) { return; }
        initialized = true;

        if (mute)
        {
            volume = AudioListener.volume;
            AudioListener.volume = 0;
        }
        else
        {
            volume = 0;
        }
        
    }

    public void ToggleSound()
    {
        mute = !mute;
        float v = volume;
        volume = AudioListener.volume;
        AudioListener.volume = v;
    }
}
