using System;
using UnityEngine;


[Serializable]
public class BGMClipData
{
    public BGMType type;//어떤 BGM인지 구분하는 열거형
    public AudioClip clip;//실제 재생할 오디오 파일

    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;
}
[Serializable]
public class SFXClipData
{
    public SFXType type;
    public AudioClip clip;

    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;
    [Range(-3.0f, 3.0f)]
    public float pitch = 1.0f;
    public bool isLoop;

    // 같은 효과음이 너무 짧은 시간 안에 여러 번 재생되는 것을 막기 위한 최소 재생 간격(초)
    // 예)
    // 0    → 제한 없음
    // 0.03 → 0.03초마다 한 번만 재생
    // 0.05 → 0.05초마다 한 번만 재생
    [Min(0f)] public float minInterval = 0f;
    [Min(1f)] public int maxSimultaneousCount = 5;
}