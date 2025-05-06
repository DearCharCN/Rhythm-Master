using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Driver : MonoBehaviour
{
    [SerializeField]
    DemoPlayer player;
    [SerializeField]
    DemoPlayerUI ui;

    [SerializeField]
    Button play_btn;
    [SerializeField]
    Button pause_btn;
    [SerializeField]
    Button stop_btn;
    [SerializeField]
    Button load_btn;

    
    private void Start()
    {
        Init();
    }

    public void Init()
    {
        play_btn.onClick.AddListener(() =>
        {
            Play();
        });
        pause_btn.onClick.AddListener(() =>
        {
            Pause();
        });
        stop_btn.onClick.AddListener(() =>
        {
            Stop();
        });
        load_btn.onClick.AddListener(() =>
        {
            LoadBtn();
        });
    }

    public void Play()
    {
        player.Play();
    }

    public void Pause()
    {
        player.Pause();
    }

    public void Stop()
    {
        player.Stop();
    }

    public void LoadBtn()
    {
        StartCoroutine(Load());
    }

    public IEnumerator Load()
    {
        string yourPath = @"D:\Projects\Unity\Rhythm Master\TempTest\1"; // 或使用 Application.persistentDataPath 之类
        MusicDataLoader loader = new MusicDataLoader();
        yield return loader.LoadMusicData(yourPath);

        while (loader.State <= MusicDataLoaderState.Loading)
        {
            yield return null;
        }

        Debug.Log("load success");

        player.SetClips(loader.loadedClips);
        player.Play();
        ui.InitAndCreate(player);
    }
}
