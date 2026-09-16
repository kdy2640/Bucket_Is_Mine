using System;
using UnityEngine;

// SO 원본과 Job 전달에 공통으로 쓰는 값. YStart는 지형 로컬 거리이며 자연 지층의 최하단은 0이다.
[Serializable]
public struct TerrainLayer
{
    [Min(0f)] public float YStart;
    [Tooltip("0은 인공 지형이며 YStart와 BlendWidth는 초기 지층 배치에 사용하지 않습니다.")]
    public byte TypeId;
    public Color Color;
    // 이 층의 시작 높이를 중심으로 아래층과 섞는 전체 폭. 인접 경계끼리 겹치지 않게 설정한다.
    [Min(0f)] public float BlendWidth;
}
