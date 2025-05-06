// RhythmDAWWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RhythmDAWWindow : EditorWindow
{
    private const int BeatsPerBar = 4;
    private const float BeatWidth = 50f;
    private const float TrackHeight = 50f;

    private List<bool[]> beatTracks = new List<bool[]>();
    private Vector2 scrollPos;
    private Vector2 verticalScroll;

    private float trackHierarchyWidth = 120f;

    private float bpm = 120f;
    private bool isPlaying = false;
    private double startTime;
    private float playbackPosition;
    private float startOffset = 0f;

    private AudioClip audioClip;
    private AudioClip metronomeClip;
    private AudioSource previewSource;
    private AudioSource metronomeSource;

    private bool draggingPlayhead = false;
    private int totalBeats = 64;
    private int lastBeatIndex = -1;

    [MenuItem("Window/节奏编辑器 DAW Pro")]
    public static void ShowWindow()
    {
        GetWindow<RhythmDAWWindow>("节奏编辑器（DAW Pro）");
    }

    private void OnEnable()
    {
        if (beatTracks.Count == 0) AddTrack();
        EditorApplication.update += Update;
        SceneView.duringSceneGui += OnSceneGUI;
        wantsMouseMove = true;
    }

    private void OnDisable()
    {
        StopAudio();
        EditorApplication.update -= Update;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Space)
            {
                if (isPlaying) StopAudio();
                else PlayAudio();
                e.Use();
            }
            else if (e.character == '.')
            {
                // 停止
                StopAudio();
                playbackPosition = 0f;
                e.Use();
            }
        }


    }

    private void OnSceneGUI(SceneView view)
    {

    }

    private void Update()
    {
        if (isPlaying && !draggingPlayhead)
        {
            double elapsed = EditorApplication.timeSinceStartup - startTime;
            playbackPosition = (float)elapsed;
            float totalDuration = GetTotalDuration();

            int currentBeatIndex = Mathf.FloorToInt(playbackPosition / GetSecondsPerBeat());
            if (currentBeatIndex != lastBeatIndex)
            {
                lastBeatIndex = currentBeatIndex;
                PlayMetronome();
            }

            if (playbackPosition >= totalDuration)
            {
                StopAudio();
                playbackPosition = 0;
            }
            Repaint();
        }
    }

    private float GetSecondsPerBeat() => 60f / bpm;

    private float GetTotalDuration()
    {
        if (audioClip != null)
        {
            return audioClip.length + Mathf.Max(0f, startOffset);
        }
        return totalBeats * GetSecondsPerBeat();
    }

    private void PlayMetronome()
    {
        if (metronomeClip == null) return;

        if (metronomeSource == null)
        {
            GameObject temp = new GameObject("MetronomeAudio");
            metronomeSource = temp.AddComponent<AudioSource>();
            metronomeSource.hideFlags = HideFlags.HideAndDontSave;
        }

        metronomeSource.PlayOneShot(metronomeClip);
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("BPM", GUILayout.Width(30));
        bpm = EditorGUILayout.FloatField(bpm, GUILayout.Width(60));
        if (bpm < 1) bpm = 1;

        GUILayout.Space(10);
        GUILayout.Label("StartOffset", GUILayout.Width(70));
        startOffset = EditorGUILayout.FloatField(startOffset, GUILayout.Width(60));
        // allow negative offset

        GUILayout.Space(20);
        audioClip = (AudioClip)EditorGUILayout.ObjectField(audioClip, typeof(AudioClip), false, GUILayout.Width(200));

        GUILayout.Label("节拍音效", GUILayout.Width(70));
        metronomeClip = (AudioClip)EditorGUILayout.ObjectField(metronomeClip, typeof(AudioClip), false, GUILayout.Width(150));

        GUIContent playPauseIcon = EditorGUIUtility.IconContent(isPlaying ? "d_PauseButton" : "d_PlayButton");
        if (GUILayout.Button(playPauseIcon, EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            if (!isPlaying) PlayAudio();
            else StopAudio();
        }

        GUIContent replayIcon = EditorGUIUtility.IconContent("Animation.FirstKey");
        if (GUILayout.Button(replayIcon, EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            ResetPlay();
        }

        GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad");
        if (GUILayout.Button(stopIcon, EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            StopAudio();
            playbackPosition = 0f;
            Repaint();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void DrawTrackSelector()
    {
        GUILayout.BeginVertical(GUILayout.Width(trackHierarchyWidth));
        var titleStyle = new GUIStyle("AnimClipToolbar");
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.BeginHorizontal(titleStyle, GUILayout.Width(trackHierarchyWidth));
        var titleLableStyle = new GUIStyle(EditorStyles.boldLabel);
        titleLableStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("轨道管理", titleLableStyle);
        GUILayout.EndHorizontal();
        for (int i = 0; i < beatTracks.Count; i++)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(TrackHeight));
            GUILayout.Label($"轨道 {i + 1}", GUILayout.Height(TrackHeight - 4));
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(TrackHeight - 4)))
            {
                beatTracks.RemoveAt(i);
                i--;
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ 添加轨道", GUILayout.Height(TrackHeight)))
        {
            AddTrack();
        }

        GUILayout.EndVertical();
    }

    private void DrawTrackArea()
    {
        float totalWidth = totalBeats * BeatWidth;
        float totalDuration = GetTotalDuration();

        GUILayout.BeginVertical();
        {
            Rect beatLabelArea = GUILayoutUtility.GetRect(totalWidth, 20f);
            DrawBeatLabels(beatLabelArea);
            HandlePlayheadDrag(beatLabelArea);

            Rect trackArea = GUILayoutUtility.GetRect(totalWidth, beatTracks.Count * TrackHeight);
            GUI.BeginGroup(trackArea);
            {
                for (int y = 0; y < beatTracks.Count; y++)
                {
                    var track = beatTracks[y];
                    for (int x = 0; x < totalBeats; x++)
                    {
                        Rect cell = new Rect(x * BeatWidth, y * TrackHeight, BeatWidth - 2, TrackHeight - 2);
                        if (x < track.Length && track[x])
                            EditorGUI.DrawRect(cell, new Color(0.2f, 0.8f, 1f, 1f));
                        else
                            EditorGUI.DrawRect(cell, new Color(0.3f, 0.3f, 0.3f, 0.3f));
                        if (x < track.Length && GUI.Button(cell, track[x] ? "●" : "", EditorStyles.miniButton))
                            track[x] = !track[x];
                    }
                }
                float playheadX = (playbackPosition / totalDuration) * totalWidth;
                Handles.color = Color.red;
                Handles.DrawLine(new Vector2(playheadX, 0), new Vector2(playheadX, beatTracks.Count * TrackHeight));
                Handles.color = Color.white;
            }
            GUI.EndGroup();
        }
        GUILayout.EndVertical();
    }

    private void OnGUI()
    {
        HandleKeyboardShortcuts();
        DrawToolbar();

        float totalDuration = GetTotalDuration();
        totalBeats = Mathf.CeilToInt(totalDuration / GetSecondsPerBeat());
        
        float totalHeight = Mathf.Max(beatTracks.Count * TrackHeight, position.height - 40);

        GUILayout.BeginHorizontal();
        DrawTrackSelector();
        DrawTrackArea();
        GUILayout.EndHorizontal();
    }

    private void DrawBeatLabels(Rect area)
    {
        float totalWidth = totalBeats * BeatWidth;
        var titleStyle = new GUIStyle("AnimClipToolbar");
        GUI.Box(new Rect(trackHierarchyWidth, area.y, totalWidth, area.height),"", titleStyle);

        for (int i = 0; i < totalBeats; i++)
        {
            float x = trackHierarchyWidth + i * BeatWidth;
            Rect beatRect = new Rect(x, area.y, BeatWidth, area.height);
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = i % BeatsPerBar == 0 ? FontStyle.Bold : FontStyle.Normal
            };
            GUI.Label(beatRect, $"{i / BeatsPerBar + 1}.{i % BeatsPerBar + 1}", style);
        }
    }

    private void HandlePlayheadDrag(Rect area)
    {
        Event e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && area.Contains(e.mousePosition))
        {
            draggingPlayhead = true;
            float mouseX = e.mousePosition.x + scrollPos.x;
            float totalWidth = totalBeats * BeatWidth;
            float totalDuration = GetTotalDuration();
            playbackPosition = Mathf.Clamp01(mouseX / totalWidth) * totalDuration;
            if (previewSource != null && previewSource.clip != null)
                previewSource.time = Mathf.Max(0f, playbackPosition - startOffset);
            Repaint();
            e.Use();
        }
        if (draggingPlayhead && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
        {
            draggingPlayhead = false;
            if (isPlaying) PlayAudio();
            e.Use();
        }
    }

    private void PlayAudio()
    {
        if (audioClip != null)
        {
            if (previewSource == null)
            {
                GameObject temp = new GameObject("PreviewAudio");
                previewSource = temp.AddComponent<AudioSource>();
                previewSource.hideFlags = HideFlags.HideAndDontSave;
            }
            previewSource.clip = audioClip;
            previewSource.time = Mathf.Max(0f, playbackPosition - startOffset);
            previewSource.Play();
        }
        isPlaying = true;
        startTime = EditorApplication.timeSinceStartup - playbackPosition;
    }

    private void StopAudio()
    {
        if (previewSource != null) previewSource.Stop();
        isPlaying = false;
    }

    private void ResetPlay()
    {
        playbackPosition = 0f;
        if (previewSource != null)
        {
            previewSource.Stop();
            previewSource.time = 0f;
        }
        PlayAudio();
    }

    private void AddTrack()
    {
        var newTrack = new bool[totalBeats];
        beatTracks.Add(newTrack);
    }
}
