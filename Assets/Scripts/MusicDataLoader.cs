using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class MusicDataLoader
{
    public string folderPath; // 设置为你传入的路径
    public MusicData loadedData;
    public List<AudioClip> loadedClips = new List<AudioClip>();
    public Texture2D albumTexture;

    public MusicDataLoaderState State { get; private set; } = MusicDataLoaderState.New;
    public async Task<bool> LoadMusicData(string folderPath)
    {
        if (State > MusicDataLoaderState.New)
            return false;

        State = MusicDataLoaderState.Loading;

        this.folderPath = folderPath;
        loadedClips.Clear();
        albumTexture = null;

        // 路径准备
        string dataPath = Path.Combine(folderPath, "Datas", "data.json");
        string streamDir = Path.Combine(folderPath, "Streams");
        string pictureDir = Path.Combine(folderPath, "Pictures");

        if (!File.Exists(dataPath))
        {
            Debug.LogError("MusicData json not found.");
            return false;
        }

        // 读取 JSON
        string json = File.ReadAllText(dataPath);
        loadedData = JsonUtility.FromJson<MusicData>(json);

        // 加载封面图
        string albumPath = Path.Combine(pictureDir, loadedData.albumPictureRelativePath);
        if (File.Exists(albumPath))
        {
            byte[] texBytes = File.ReadAllBytes(albumPath);
            albumTexture = new Texture2D(2, 2);
            albumTexture.LoadImage(texBytes);
        }
        else
        {
            Debug.LogWarning("Album image not found.");
        }

        // 加载所有音轨
        foreach (var track in loadedData.trackDatas)
        {
            string audioPath = Path.Combine(streamDir, track.clipRelativePath);
            if (File.Exists(audioPath))
            {
                AudioClip clip = await LoadClipFromFile(audioPath);
                if (clip != null)
                {
                    loadedClips.Add(clip);
                }
                else
                {
                    Debug.LogWarning("Failed to load clip: " + audioPath);
                    loadedClips.Add(null);
                }
            }
            else
            {
                Debug.LogWarning("Clip file not found: " + audioPath);
                loadedClips.Add(null);
            }
        }

        Debug.Log("MusicData loaded successfully.");
        State = MusicDataLoaderState.Loaded;
        return true;
    }

    private async Task<AudioClip> LoadClipFromFile(string fullPath)
    {
        string uri = "file://" + fullPath.Replace("\\", "/");
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            var op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var clip = DownloadHandlerAudioClip.GetContent(request);
                clip.name = Path.GetFileNameWithoutExtension(fullPath);
                return clip;
            }
            else
            {
                Debug.LogError("Failed to load AudioClip from " + fullPath);
                return null;
            }
        }
    }

    public void UnloadMusicData()
    {
        if (State != MusicDataLoaderState.Loaded)
            return;

        State = MusicDataLoaderState.Unloading;
        // 卸载音频资源
        foreach (var clip in loadedClips)
        {
            if (clip != null)
            {
                // 卸载 AudioClip（仅限非 Asset 中 clip）
                if (clip.loadType == AudioClipLoadType.DecompressOnLoad || clip.loadState == AudioDataLoadState.Loaded)
                {
                    Resources.UnloadAsset(clip); // or UnityEngine.Object.Destroy(clip);
                }
            }
        }
        loadedClips.Clear();

        // 卸载封面图
        if (albumTexture != null)
        {
            UnityEngine.Object.Destroy(albumTexture);
            albumTexture = null;
        }

        loadedData = new MusicData();
        folderPath = null;
        Debug.Log("MusicData unloaded.");
        State = MusicDataLoaderState.Unloaded;
    }
}

public enum MusicDataLoaderState
{
    New,
    Loading,
    Loaded,
    Unloading,
    Unloaded,
}