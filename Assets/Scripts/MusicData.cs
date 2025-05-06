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
    public float timeOffset;//��λ����
    public TimeSignature timeSignature;
    public BPM BPM;
}

[Serializable]
public struct TimeSignature
{
    public int numerator;     // ���ӣ�ÿС�ڵ�����
    public int denominator;   // ��ĸ��������������һ��
}

[Serializable]
public struct BPM
{
    public float value;
}