using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Xabe.FFmpeg;

public class MusicDataEditorWindow : EditorWindow
{
    private MusicData musicData;
    private Texture2D albumTexture;
    private List<string> clipPaths = new List<string>(); // 替代原本 List<AudioClip>
    private List<TrackData> tracks = new List<TrackData>();
    private string loadedFolderPath = "";
    private bool isDirty = false;
    private Vector2 scrollPosition;
    private List<bool> trackFoldouts = new List<bool>();
    private List<bool> rhythmFoldouts = new List<bool>();
    private string albumTextureSourcePath = null;

    [MenuItem("Tools/Music Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<MusicDataEditorWindow>("Music Data Editor");
    }

    //[MenuItem("Tools/Test")]
    //public static async void Test()
    //{
    //    // 输入和输出路径
    //    //string inputPath = "D:\\Projects\\Unity\\Rhythm Master\\TempTest\\input.mp4";
    //    //string outputPath = "D:\\Projects\\Unity\\Rhythm Master\\TempTest\\output.mp4";

    //    string inputPath = "D:\\Projects\\Unity\\Rhythm Master\\TempTest\\input.jpg";
    //    string outputPath = "D:\\Projects\\Unity\\Rhythm Master\\TempTest\\output.png";

    //    await FFmpegConversion(inputPath, outputPath);
    //    Debug.Log("转换完成！");
    //}

    public static Task<IConversionResult> FFmpegConversion(string inputPath, string outputPath)
    {
        //Debug.Log($"inputPath:{inputPath}");
        //Debug.Log($"outputPath:{outputPath}");
        // 构建转换任务
        IConversion conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i \"{inputPath}\"")        // 输入文件
            .AddParameter($"\"{outputPath}\"", ParameterPosition.PostInput); // 输出文件

        // 执行转换
        return conversion.Start();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 所有字段输入
        musicData.name = EditorGUILayout.TextField("Song Name", musicData.name);
        musicData.albumName = EditorGUILayout.TextField("Album Name", musicData.albumName);
        musicData.artist = EditorGUILayout.TextField("Artist", musicData.artist);

        DrawAlbumTexture();

        DrawRhythms();
        DrawTrackList();

        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace(); // 推到底部

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(30))) SaveToFolder(false);
        if (GUILayout.Button("Save As", GUILayout.Height(30))) SaveToFolder(true);
        if (GUILayout.Button("Load", GUILayout.Height(30))) TryLoadFolder();
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Music Data Changed");
            isDirty = true;
        }
    }

    void DrawAlbumTexture()
    {
        EditorGUILayout.LabelField("Album Texture", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        // ⬅ 左侧：路径+按钮
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 160));

        // 路径显示（只读）
        EditorGUILayout.LabelField("Path:");
        GUI.enabled = false;
        EditorGUILayout.SelectableLabel(albumTextureSourcePath ?? "(none)", EditorStyles.textField, GUILayout.Height(18));
        GUI.enabled = true;

        // 选择按钮
        if (GUILayout.Button("Select Album Image", GUILayout.Height(30)))
        {
            string path = EditorUtility.OpenFilePanel("Select Album Image", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                byte[] texBytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(texBytes))
                {
                    albumTexture = tex;
                    albumTexture.name = Path.GetFileNameWithoutExtension(path);
                    albumTextureSourcePath = path;
                    isDirty = true;
                }
                else
                {
                    Debug.LogWarning("Failed to load selected image.");
                }
            }
        }

        EditorGUILayout.EndVertical();

        // ➡ 右侧：预览图
        GUILayout.Space(10);
        Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
        GUI.Box(previewRect, ""); // 背景框
        if (albumTexture != null)
        {
            EditorGUI.DrawPreviewTexture(previewRect, albumTexture, null, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.Label(previewRect, "No Image", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawRhythms()
    {
        EditorGUILayout.LabelField("Rhythms", EditorStyles.boldLabel);

        if (musicData.rhythms == null)
            musicData.rhythms = new Rhythm[0];

        while (rhythmFoldouts.Count < musicData.rhythms.Length)
            rhythmFoldouts.Add(true);
        while (rhythmFoldouts.Count > musicData.rhythms.Length)
            rhythmFoldouts.RemoveAt(rhythmFoldouts.Count - 1);

        for (int i = 0; i < musicData.rhythms.Length; i++)
        {
            rhythmFoldouts[i] = EditorGUILayout.Foldout(rhythmFoldouts[i], $"Rhythm {i + 1}", true);

            if (rhythmFoldouts[i])
            {
                EditorGUILayout.BeginVertical("box");

                Rhythm rhythm = musicData.rhythms[i];
                rhythm.timeOffset = EditorGUILayout.FloatField("Time Offset (s)", rhythm.timeOffset);
                rhythm.timeSignature.numerator = EditorGUILayout.IntField("Numerator", rhythm.timeSignature.numerator);
                rhythm.timeSignature.denominator = EditorGUILayout.IntField("Denominator", rhythm.timeSignature.denominator);
                rhythm.BPM.value = EditorGUILayout.FloatField("BPM", rhythm.BPM.value);
                musicData.rhythms[i] = rhythm;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 0)
                {
                    Undo.RecordObject(this, "Move Rhythm Up");
                    SwapRhythm(i, i - 1);
                    return;
                }

                if (GUILayout.Button("↓", GUILayout.Width(30)) && i < musicData.rhythms.Length - 1)
                {
                    Undo.RecordObject(this, "Move Rhythm Down");
                    SwapRhythm(i, i + 1);
                    return;
                }

                if (GUILayout.Button("Remove"))
                {
                    Undo.RecordObject(this, "Remove Rhythm");
                    var list = new List<Rhythm>(musicData.rhythms);
                    list.RemoveAt(i);
                    musicData.rhythms = list.ToArray();
                    rhythmFoldouts.RemoveAt(i);
                    isDirty = true;
                    return;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        if (GUILayout.Button("Add Rhythm"))
        {
            Undo.RecordObject(this, "Add Rhythm");
            var list = new List<Rhythm>(musicData.rhythms);
            list.Add(new Rhythm
            {
                timeOffset = 0,
                timeSignature = new TimeSignature { numerator = 4, denominator = 4 },
                BPM = new BPM { value = 120f }
            });
            musicData.rhythms = list.ToArray();
            rhythmFoldouts.Add(true);
            isDirty = true;
        }
    }

    void SwapRhythm(int i, int j)
    {
        (musicData.rhythms[i], musicData.rhythms[j]) = (musicData.rhythms[j], musicData.rhythms[i]);
        (rhythmFoldouts[i], rhythmFoldouts[j]) = (rhythmFoldouts[j], rhythmFoldouts[i]);
        isDirty = true;
    }
    void DrawTrackList()
    {
        int moveFrom = -1, moveTo = -1;

        EditorGUILayout.LabelField("Tracks", EditorStyles.boldLabel);

        for (int i = 0; i < tracks.Count; i++)
        {
            if (i >= trackFoldouts.Count) trackFoldouts.Add(true);

            trackFoldouts[i] = EditorGUILayout.Foldout(trackFoldouts[i], $"Track {i + 1}", true);

            if (trackFoldouts[i])
            {
                EditorGUILayout.BeginVertical("box");

                TrackData td = tracks[i];
                td.trackType = (TrackType)EditorGUILayout.EnumPopup("Track Type", td.trackType);
                td.trackIndex = i; // ✅ 自动设置 index

                GUI.enabled = false;
                EditorGUILayout.IntField("Track Index", td.trackIndex); // ✅ 显示但禁用
                GUI.enabled = true;

                tracks[i] = td;

                if (i >= clipPaths.Count) clipPaths.Add(null);

                // 替换 clips[i] 为 clipPaths[i]
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Audio Clip");

                if (!string.IsNullOrEmpty(clipPaths[i]))
                {
                    EditorGUILayout.LabelField(Path.GetFileName(clipPaths[i]), GUILayout.MaxWidth(200));
                }
                else
                {
                    EditorGUILayout.LabelField("None", GUILayout.MaxWidth(200));
                }

                if (GUILayout.Button("Select File", GUILayout.MaxWidth(100)))
                {
                    string path = EditorUtility.OpenFilePanel("Select Audio Clip", "", "wav");
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        clipPaths[i] = path;
                        isDirty = true;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                //if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 0)
                //{
                //    moveFrom = i;
                //    moveTo = i - 1;
                //}

                //if (GUILayout.Button("↓", GUILayout.Width(30)) && i < tracks.Count - 1)
                //{
                //    moveFrom = i;
                //    moveTo = i + 1;
                //}

                if (GUILayout.Button("Remove"))
                {
                    Undo.RecordObject(this, "Remove Track");
                    trackFoldouts.RemoveAt(i);
                    tracks.RemoveAt(i);
                    clipPaths.RemoveAt(i);
                    isDirty = true;
                    return;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        if (GUILayout.Button("Add Track"))
        {
            Undo.RecordObject(this, "Add Track");
            tracks.Add(new TrackData());
            clipPaths.Add(null);
            trackFoldouts.Add(true);
            isDirty = true;
        }

        if (moveFrom != -1 && moveTo != -1)
        {
            Undo.RecordObject(this, "Move Track");
            SwapTrack(moveFrom, moveTo);
        }
    }


    void SwapTrack(int i, int j)
    {
        (trackFoldouts[i], trackFoldouts[j]) = (trackFoldouts[j], trackFoldouts[i]);
        (tracks[i], tracks[j]) = (tracks[j], tracks[i]);
        (clipPaths[i], clipPaths[j]) = (clipPaths[j], clipPaths[i]);
        isDirty = true;
    }

    private async void SaveToFolder(bool isSaveAs)
    {
        if (musicData.name == null || string.IsNullOrEmpty(musicData.name))
        {
            EditorUtility.DisplayDialog("保存失败", "请填写歌曲名字", "OK");
            return;
        }

        string rootPath;

        if (isSaveAs || string.IsNullOrEmpty(loadedFolderPath))
        {
            // 首次保存或点击 Save As，弹出选择框
            rootPath = EditorUtility.SaveFolderPanel("Select Save Folder", "", musicData.name);
            if (string.IsNullOrEmpty(rootPath)) return;
            loadedFolderPath = Path.Combine(rootPath, musicData.name); // ✅ 设置保存路径
        }

        string rootDir = loadedFolderPath;
        string streamsDir = Path.Combine(rootDir, "Streams");
        string dataDir = Path.Combine(rootDir, "Datas");
        string pictureDir = Path.Combine(rootDir, "Pictures");

        Directory.CreateDirectory(streamsDir);
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(pictureDir);

        await ConvertAlbumTextureWithFFmpeg(pictureDir);

        // 在导出前更新每个 TrackData 的 trackIndex
        for (int i = 0; i < tracks.Count; i++)
        {
            var td = tracks[i];
            td.trackIndex = i;
            tracks[i] = td;
        }

        // 保存前清理旧 wav 文件
        var expectedFiles = new HashSet<string>();
        for (int i = 0; i < tracks.Count; i++)
        {
            string expectedFile = $"track{i}_{tracks[i].trackType.ToString().ToLower()}.wav";
            expectedFiles.Add(expectedFile);
        }

        if (Directory.Exists(streamsDir))
        {
            var existingFiles = Directory.GetFiles(streamsDir, "*.wav");
            foreach (var file in existingFiles)
            {
                string filename = Path.GetFileName(file);
                if (!expectedFiles.Contains(filename))
                {
                    File.Delete(file); // 删除旧文件
                }
            }
        }

        // 保存音频片段
        for (int i = 0; i < clipPaths.Count; i++)
        {
            string sourcePath = clipPaths[i];
            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
            {
                TrackData td = tracks[i];
                string fileName = $"track{td.trackIndex}_{td.trackType.ToString().ToLower()}.wav";
                string destPath = Path.Combine(streamsDir, fileName);
                File.Copy(sourcePath, destPath, true);
                clipPaths[i] = destPath;
                td.clipRelativePath = fileName;
                tracks[i] = td;
            }
        }

        musicData.trackDatas = tracks.ToArray();

        string json = JsonUtility.ToJson(musicData, true);
        File.WriteAllText(Path.Combine(dataDir, "data.json"), json);

        EditorUtility.DisplayDialog("保存成功", "数据保存成功", "OK");
        EditorUtility.RevealInFinder(rootDir); // ✅ 自动打开保存的文件夹
        isDirty = false;
    }

    private async Task<bool> ConvertAlbumTextureWithFFmpeg(string pictureDir)
    {
        if (albumTexture == null) return false;

        if (string.IsNullOrEmpty(albumTextureSourcePath) || !File.Exists(albumTextureSourcePath))
        {
            Debug.LogWarning("Invalid album texture path.");
            return false;
        }

        string outputFileName = "album.png";
        string outputPath = Path.Combine(pictureDir, outputFileName);
        if (ArePathsEqual(albumTextureSourcePath, outputPath))
            return false;
        musicData.albumPictureRelativePath = outputFileName;

        string ext = Path.GetExtension(albumTextureSourcePath).ToLowerInvariant();

        if (ext == ".png")
        {
            // ✅ 已是PNG，直接复制
            try
            {
                File.Copy(albumTextureSourcePath, outputPath, true);
                OnAlbumTextureSaved(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Copying album image failed: {ex.Message}");
                return false;
            }
        }
        else
        {
            // ❗ 不是PNG，使用 FFmpeg 转码
            string sourcePath = Path.GetFullPath(albumTextureSourcePath).Replace("\\", "/");
            outputPath = Path.GetFullPath(outputPath).Replace("\\", "/");

            try
            {
                await FFmpegConversion(sourcePath, outputPath);
                OnAlbumTextureSaved(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FFmpeg conversion failed: {ex.Message}");
                return false;
            }
        }
    }

    private void OnAlbumTextureSaved(string outputPath)
    {
        albumTextureSourcePath = outputPath; // ✅ 更新封面图路径
    }

    void TryLoadFolder()
    {
        if (isDirty)
        {
            bool shouldContinue = EditorUtility.DisplayDialog("Unsaved Changes", "You have unsaved changes. Continue anyway?", "Yes", "No");
            if (!shouldContinue) return;
        }

        string folder = EditorUtility.OpenFolderPanel("Select Music Root Folder", "", "");
        if (string.IsNullOrEmpty(folder)) return;

        string dataPath = Path.Combine(folder, "Datas");
        string picturePath = Path.Combine(folder, "Pictures");
        string streamsPath = Path.Combine(folder, "Streams");

        if (!Directory.Exists(dataPath) || !Directory.Exists(picturePath) || !Directory.Exists(streamsPath))
        {
            EditorUtility.DisplayDialog("Error", "Invalid folder structure!", "OK");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(dataPath, "*.json");
        if (jsonFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No JSON data file found!", "OK");
            return;
        }

        string json = File.ReadAllText(jsonFiles[0]);
        musicData = JsonUtility.FromJson<MusicData>(json);

        // 加载贴图
        string texPath = Path.Combine(picturePath, musicData.albumPictureRelativePath);
        if (File.Exists(texPath))
        {
            byte[] texBytes = File.ReadAllBytes(texPath);
            albumTexture = new Texture2D(2, 2);
            albumTexture.LoadImage(texBytes);

            albumTextureSourcePath = texPath; // ✅ 缓存原始路径
        }

        // 加载 clip 信息
        clipPaths.Clear();
        tracks = new List<TrackData>(musicData.trackDatas);
        for (int i = 0; i < tracks.Count; i++)
        {
            string clipFile = Path.Combine(streamsPath, tracks[i].clipRelativePath);
            clipPaths.Add(File.Exists(clipFile) ? clipFile : null);
        }

        loadedFolderPath = folder;

        isDirty = false;
        Repaint();
    }

    public static bool ArePathsEqual(string path1, string path2)
    {
        try
        {
            string fullPath1 = Path.GetFullPath(path1.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                    .ToLowerInvariant();

            string fullPath2 = Path.GetFullPath(path2.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                    .ToLowerInvariant();

            return fullPath1 == fullPath2;
        }
        catch (Exception)
        {
            return false; // 非法路径处理
        }
    }
}
