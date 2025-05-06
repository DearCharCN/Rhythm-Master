using System.Collections.Generic;
using UnityEngine;

public class DemoPlayer : MonoBehaviour
{
    [SerializeField]
    AudioSource[] sources;

    public void SetClips(List<AudioClip> clips)
    {
        TryDestroySources();
        sources = new AudioSource[clips.Count];
        for (int i = 0; i < sources.Length; i++)
        {
            GameObject go = new GameObject(clips[i].name);
            go.transform.SetParent(transform);
            var com = go.AddComponent<AudioSource>();
            com.playOnAwake = false;
            sources[i] = com;
            sources[i].clip = clips[i];
        }
    }

    private void TryDestroySources()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            Destroy(sources[i].gameObject);
        }
    }

    public void Play()
    {
        for (int i = 0; i < sources.Length; ++i)
        {
            sources[i].Play();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < sources.Length; ++i)
        {
            sources[i].Stop();
        }
    }

    public void Pause()
    {
        for (int i = 0; i < sources.Length; ++i)
        {
            sources[i].Pause();
        }
    }

    public void SetVolume(float volume)
    {
        for (int i = 0; i < sources.Length; ++i)
        {
            sources[i].volume = volume;
        }
    }

    public void SetTrackVolume(int trackIndex,float volume)
    {
        sources[trackIndex].volume = volume;
    }

    public int GetTrackCount()
    {
        return sources.Length;
    }

    public float[] GetVolumes()
    {
        float[] volumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; ++i)
        {
            volumes[i] = sources[i].volume;
        }
        return volumes;
    }

    public string GetTrackName(int trackIndex)
    {
        return sources[trackIndex].clip.name;
    }
}