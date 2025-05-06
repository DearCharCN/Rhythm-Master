using System;

[Serializable]
public struct MusicData
{
    public string name;
    public string albumName;
    public string albumPictureRelativePath;
    public string artist;
    public Rhythm[] rhythms;
    public TrackData[] trackDatas;
}

[Serializable]
public struct TrackData
{
    public TrackType trackType;
    public string clipRelativePath;
    public int trackIndex;
}

[Serializable]
public enum TrackType
{
    Bass,
    Drums,
    Guitar,
    Other,
    Piano,
    Vocals,
}

[Serializable]
public struct Rhythm
{
    public float timeOffset;//单位：秒
    public TimeSignature timeSignature;
    public BPM BPM;
}

[Serializable]
public struct TimeSignature
{
    public int numerator;     // 分子：每小节的拍数
    public int denominator;   // 分母：哪种音符代表一拍
}

[Serializable]
public struct BPM
{
    public float value;
}