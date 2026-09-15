using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//프로젝트 전체에서 하나만 존재하도록 관리된다.
public enum BGMType
{
    None,
    TitieBGM = 0,
    HubBGM = 1,
    ServiceBGM = 2,
    HarvestBGM = 3,
}
public enum SFXType
{
    None = -1,

    // Global
    Global_ButtonClick = 0,         // 활성 공용 버튼을 눌렀을 때
    Global_ButtonHover = 1,         // 포인터가 활성 버튼이나 카드에 처음 진입했을 때
    Global_SceneChangeStart = 2,    // 씬 전환 로딩 화면이 열릴 때
    Global_SceneChangeEnd = 3,      // 로딩 화면이 닫히고 새 씬이 공개될 때
    Global_Notification = 4,        // 토스트나 새 알림이 표시될 때
    Global_Error = 5,               // 실행 가능한 입력이 조건 부족으로 거절됐을 때
    Global_PanelPopup = 6,          // 공통 팝업이나 상세 패널이 열릴 때
    Global_PanelClose = 7,          // 팝업이나 상세 패널을 닫고 이전 화면으로 돌아갈 때
    Global_Confirm = 8,             // 확인, 저장, 초기화 같은 동작이 완료됐을 때

    // Hub
    Hub_Upgrade = 100,              // 시설, 직원, 요리, 수확 능력, 스테이지 업그레이드 성공 시
    Hub_Select = 101,               // 시설, 직원, 요리 카드, 수확 항목, 스테이지, 축제 항목을 탐색 선택했을 때
    Hub_MenuSelect = 103,           // 오늘의 메뉴에 요리를 추가했을 때
    Hub_MenuDeselect = 104,         // 오늘의 메뉴에서 요리를 제거했을 때
    Hub_GetReward = 105,            // 현재 마켓 미션 보상을 수령했을 때
    Hub_Rankup = 106,               // 마켓 승급이 확정되고 승급 패널이 열릴 때
    Hub_FestivalStart = 107,        // 맛 또는 카테고리 축제를 실제 시작했을 때
    Hub_NextDay = 108,              // 밤에서 다음 영업일 아침으로 넘어갈 때

    // Service
    Service_SessionStart = 200,     // 영업 시작 카운트다운의 GO 또는 영업 루프 시작 시
    Service_SessionEnd = 201,       // 영업 시간이 끝나 결과 흐름으로 진입할 때
    Service_CustomerAngry = 202,    // 주문 제한시간 초과로 고객이 화나서 떠날 때
    Service_CustomerEat = 203,      // 고객이 식사하는 동안 한입 연출이 재생될 때
    Service_CustomerPay = 204,      // 식사 또는 음료 대금이 실제 재화로 지급될 때
    Service_ServerVoice = 205,      // 서버가 작업을 시작하거나 상황에 반응할 때
    Service_ComboSound = 206,       // 콤보가 임계치에 도달하여 발동됐을때.
    Service_OrderCreated = 207,     // 새 고객의 주문 UI가 나타났을 때
    Service_OrderAccepted = 208,    // 주문 접수와 완성 요리 차감이 성공했을 때
    Service_CookQueued = 209,       // 조리 요청이 성공해 조리 대기열에 들어갔을 때
    Service_CookComplete = 210,     // 조리가 끝나 완성 요리가 재고에 추가됐을 때
    Service_DishPickup = 211,       // 서버가 주방에서 완성 요리를 집었을 때
    Service_DishServed = 212,       // 서버가 고객에게 요리를 전달했을 때
    Service_TipAdded = 213,         // 고객이 팁 박스에 팁을 추가했을 때
    Service_TipCollected = 214,     // 팁 버튼으로 누적 팁을 회수했을 때
    Service_ComboBreak = 215,       // 제한시간 초과나 먹튀 성공으로 콤보가 끊겼을 때
    Service_NegativeEventStart = 216,   // 먹튀, 테이블 오염, 서버 수면 등 부정 이벤트가 발생했을 때
    Service_NegativeEventSelect = 217,  // 해결할 부정 이벤트를 선택했을 때
    Service_NegativeEventResolve = 218, // 선택한 부정 이벤트를 해결했을 때
    Service_DrinkServed = 220,      // 고객이 음료를 소비하고 음료 매출이 발생했을 때
    Service_DrinkRefill = 221,      // 음료 리필 게이지가 가득 차 보충이 완료됐을 때
    Service_Acceleration = 222,     // 남은 횟수를 소비해 영업 가속이 실제 발동했을 때

    // Harvest
    Harvest_SessionStart = 300,     // 수확 시작 카운트다운의 GO 또는 수확 루프 시작 시
    Harvest_SessionEnd = 301,       // 수확 시간이나 연료가 끝나 조작이 종료될 때
    Harvest_Collect = 302,          // 수확 보상 재료가 플레이어 재고에 귀속될 때
    Harvest_Grind = 303,            // 커터가 수확 대상과 접촉해 절단 중일 때 사용하는 루프음
    Harvest_CropHarvested = 304,    // 정적 작물의 HP가 0이 되어 수확 완료됐을 때
    Harvest_TractorEngine = 305,    // 수확 세션에서 트랙터가 실제 주행 중일 때 사용하는 루프음
    Harvest_ChickenVoice = 306,     // 닭이 평상 또는 도주 상태에서 울 때
    Harvest_ChickenHit = 307,       // 닭이 커터에 피격됐을 때
    Harvest_ChickenDie = 308,       // 닭의 HP가 0이 되어 사망할 때
    Harvest_CowVoice = 309,         // 소가 평상 또는 도주 상태에서 울 때
    Harvest_CowHit = 310,           // 소가 커터에 피격됐을 때
    Harvest_CowDie = 311,           // 소의 HP가 0이 되어 사망할 때
    Harvest_SheepVoice = 312,       // 양이 평상 또는 도주 상태에서 울 때
    Harvest_SheepHit = 313,         // 양이 커터에 피격됐을 때
    Harvest_SheepDie = 314,         // 양의 HP가 0이 되어 사망할 때
    Harvest_PigVoice = 315,         // 돼지가 평상 또는 도주 상태에서 울 때
    Harvest_PigHit = 316,           // 돼지가 커터에 피격됐을 때
    Harvest_PigDie = 317,           // 돼지의 HP가 0이 되어 사망할 때
    Harvest_ResultReveal = 318,     // 수확 종료 후 결과 패널과 획득량을 공개할 때
    Harvest_CropHit = 319,          // 정적 작물이 커터 피해를 받았을 때
    Harvest_GoldenPigDetected = 320, // 황금돼지가 레이더 범위에 처음 들어왔을 때
    Harvest_GoldenPigCollected = 321,// 황금돼지를 쓰러뜨리고 희귀 보상을 획득했을 때
    Harvest_SkillActivate = 322,    // 수확 스킬이 사용 가능 검사를 통과하고 발동했을 때
    Harvest_SkillReady = 323,       // 수확 스킬 쿨다운이 끝나 다시 사용 가능해졌을 때
    Harvest_TimeWarning = 324       // 수확 남은 시간이 경고 임계값에 처음 진입했을 때
}
public class AudioManager : MonoBehaviour
{
    [Header("AudioSource")]
    [SerializeField] private AudioSource hubBGMSource;
    [SerializeField] private AudioSource serviceBGMSource;
    [SerializeField] private AudioSource harvestBGMSource;
    [SerializeField] private int sfxSourceCount = 20;//10에서 20으로 변경
    [SerializeField] private AudioSource[] sfxSources;

    // AudioSource를 순환 관리하기 위한 Queue
    private Queue<AudioSource> sfxQueue;

    [Header("BGM List")]
    [SerializeField] private BGMClipData[] bgmClips = System.Array.Empty<BGMClipData>();//인스펙터에서 등록할 BGM
    [SerializeField, Min(0f)] private float bgmBlendDuration = 1f;
    [Header("SFX List")]
    [SerializeField] private SFXClipData[] sfxClips = System.Array.Empty<SFXClipData>();//인스펙터에서 등록할 효과음 데이터


    //예 : BGMType.Stage -> Stage BGM데이터
    private Dictionary<BGMType, BGMClipData> bgmDictionary;
    //예 : SFXType.Jump -> Jump 효과음 데이터
    private Dictionary<SFXType, SFXClipData> sfxDictionary;

    // 각 효과음이 마지막으로 재생된 시간을 저장하는 딕셔너리
    // Key   : 효과음 종류(SFXType)
    // Value : 마지막 재생 시간(Time.time)
    private Dictionary<SFXType, float> lastPlayTimes;
    private Dictionary<SFXType, int> playingCounts;
    private Dictionary<SFXType, AudioSource> loopingSFXSources;

    private BGMClipData currentBGMData;
    private Coroutine bgmBlendCoroutine;
    private bool areBGMPlayersStarted;
    private float hubBGMBlendWeight;
    private float serviceBGMBlendWeight;
    private float harvestBGMBlendWeight;
    private float masterVolume = 1.0f;
    private float bgmVolume = 1.0f;
    private float sfxVolume = 1.0f;

    public float MasterVolume => masterVolume;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    protected void Awake()
    {  
        sfxQueue = new Queue<AudioSource>();

        CreateAudioSources();
        // 각 효과음의 마지막 재생 시간을 저장하는 Dictionary 생성
        lastPlayTimes = new Dictionary<SFXType, float>();
        playingCounts = new Dictionary<SFXType, int>();
        loopingSFXSources = new Dictionary<SFXType, AudioSource>();

        InitializeDictionary();
        StartSynchronizedBGM();
    }
   //AudioSource가 없을경우 자동으로 만들어주는 녀석
   private void CreateAudioSources()
    {
        if (sfxSources == null || sfxSources.Length == 0)
        {
            sfxSources = new AudioSource[sfxSourceCount];

            for (int i = 0; i < sfxSourceCount; i++)
            {
                GameObject sfxObj = new GameObject($"SFX Source {i}");
                sfxObj.transform.SetParent(transform);

                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.loop = false;

                sfxSources[i] = source;

                sfxQueue.Enqueue(source);
            }
        }
    }

    private void StartSynchronizedBGM()
    {
        if (bgmDictionary.Count == 0)
            return;

        double startTime = AudioSettings.dspTime + 0.1d;

        hubBGMBlendWeight = 0f;
        serviceBGMBlendWeight = 0f;
        harvestBGMBlendWeight = 0f;

        hubBGMSource.clip = bgmDictionary[BGMType.HubBGM].clip;
        serviceBGMSource.clip = bgmDictionary[BGMType.ServiceBGM].clip;
        harvestBGMSource.clip = bgmDictionary[BGMType.HarvestBGM].clip;

        hubBGMSource.volume = 0f;
        serviceBGMSource.volume = 0f;
        harvestBGMSource.volume = 0f;

        hubBGMSource.PlayScheduled(startTime);
        serviceBGMSource.PlayScheduled(startTime);
        harvestBGMSource.PlayScheduled(startTime);

        areBGMPlayersStarted = true;
    }
    private AudioSource GetSFXSource()
    {
        int count = sfxQueue.Count;

        // Queue에 있는 AudioSource를 모두 검사
        for (int i = 0; i < count; i++)
        {
            AudioSource source = sfxQueue.Dequeue();

            // 다시 Queue의 뒤에 넣는다.
            sfxQueue.Enqueue(source);

            // 사용 가능하면 즉시 반환
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return null; //오디오소스가 10개인 경우 새 SFX효과음은 무시하고 기존 10개만 끝까지 재생
    }
    //배열로 등록한 오디 데이터를 딕셔너리에 저장하는 녀석
    private void InitializeDictionary()
    {
        bgmDictionary = new Dictionary<BGMType, BGMClipData>();
        sfxDictionary = new Dictionary<SFXType, SFXClipData>();

        for (int i = 0;i<bgmClips.Length;i++)
        {
            //배열 요소가 비어 있으면
            if (bgmClips[i] == null) continue;
            //BGM데이터 안에 AudioClip이 연결되어 있지 않으면
            if (bgmClips[i].clip == null) continue;

            //딕셔너리에 같은 BGMType이 아직 없으면
            if (!bgmDictionary.ContainsKey(bgmClips[i].type))
            {
                //BGMType을 key, BGMClipData를 Value로 저장
                bgmDictionary.Add(bgmClips[i].type, bgmClips[i]);
            }
        }
        for(int i = 0;i<sfxClips.Length;i++)
        {
            if (sfxClips[i] == null) continue;
            if (sfxClips[i].clip == null) continue;

            //딕셔너리에 같은 SFXType이 아직 없으면
            if (!sfxDictionary.ContainsKey(sfxClips[i].type))
            {
                sfxDictionary.Add(sfxClips[i].type, sfxClips[i]);
            }
        }
    }
#if UNITY_EDITOR
    [ContextMenu("Apply SFX To GameManager Prefab")]
    private void ApplyToGameManagerPrefab()
    {
        const string gameManagerPrefabPath =
            "Assets/Prefabs/GameManager.prefab";

        GameObject gameManagerPrefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(gameManagerPrefabPath);
        AudioManager prefabAudioManager =
            gameManagerPrefab.GetComponentInChildren<AudioManager>(true);

        UnityEditor.Undo.RecordObject(
            prefabAudioManager,
            "Apply SFX To GameManager Prefab");

        UnityEditor.SerializedObject sourceObject =
            new UnityEditor.SerializedObject(this);
        UnityEditor.SerializedObject targetObject =
            new UnityEditor.SerializedObject(prefabAudioManager);

        sourceObject.Update();
        targetObject.Update();
        targetObject.CopyFromSerializedProperty(
            sourceObject.FindProperty(nameof(sfxClips)));
        targetObject.ApplyModifiedProperties();

        UnityEditor.EditorUtility.SetDirty(prefabAudioManager);
        UnityEditor.PrefabUtility.SavePrefabAsset(gameManagerPrefab);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log(
            "[AudioManager] SFX 설정을 GameManager 프리팹에 적용했습니다.",
            prefabAudioManager);
    }

    [ContextMenu("Refresh SFX Clips By Type")]
    private void RefreshSFXClipsByType()
    {
        const string sfxFolderPath = "Assets/Resources/Audios/SFXs";

        UnityEditor.Undo.RecordObject(this, "Refresh SFX Clips By Type");

        Dictionary<SFXType, SFXClipData> existingClips = new Dictionary<SFXType, SFXClipData>();
        for (int i = 0; i < sfxClips.Length; i++)
        {
            SFXClipData data = sfxClips[i];
            if (data == null || data.type == SFXType.None) continue;
            if (!existingClips.ContainsKey(data.type))
            {
                existingClips.Add(data.type, data);
            }
        }

        string[] audioClipGuids = UnityEditor.AssetDatabase.FindAssets(
            "t:AudioClip",
            new[] { sfxFolderPath });
        List<string> audioClipPaths = new List<string>();

        for (int i = 0; i < audioClipGuids.Length; i++)
        {
            audioClipPaths.Add(
                UnityEditor.AssetDatabase.GUIDToAssetPath(audioClipGuids[i]));
        }

        audioClipPaths.Sort(System.StringComparer.Ordinal);

        Dictionary<string, AudioClip> audioClipsByName =
            new Dictionary<string, AudioClip>(System.StringComparer.Ordinal);

        for (int i = 0; i < audioClipPaths.Count; i++)
        {
            string audioClipPath = audioClipPaths[i];
            string audioClipName =
                System.IO.Path.GetFileNameWithoutExtension(audioClipPath);
            AudioClip audioClip =
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);

            if (audioClipsByName.ContainsKey(audioClipName))
            {
                Debug.LogWarning(
                    $"[AudioManager] 같은 이름의 SFX AudioClip이 중복되었습니다. "
                    + $"name: {audioClipName}, ignoredPath: {audioClipPath}",
                    this);
                continue;
            }

            audioClipsByName.Add(audioClipName, audioClip);
        }

        System.Array types = System.Enum.GetValues(typeof(SFXType));
        List<SFXClipData> refreshedClips = new List<SFXClipData>();

        for (int i = 0; i < types.Length; i++)
        {
            SFXType type = (SFXType)types.GetValue(i);
            if (type == SFXType.None) continue;

            if (!existingClips.TryGetValue(type, out SFXClipData data))
            {
                data = new SFXClipData { type = type };
            }

            if (audioClipsByName.TryGetValue(type.ToString(), out AudioClip audioClip))
            {
                data.clip = audioClip;
            }
            else
            {
                data.clip = null;
                Debug.LogWarning(
                    $"[AudioManager] SFX AudioClip을 찾을 수 없습니다. "
                    + $"type: {type}, folder: {sfxFolderPath}",
                    this);
            }

            refreshedClips.Add(data);
        }

        sfxClips = refreshedClips.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    //BGM을 재생하는 녀석
    //AudioManager.Instance.PlayBGM(BGMType.Stage);
    public void PlayBGM(BGMType type)
    {
        //요청한 BGMType이 딕셔너리에 없으면
        if(!bgmDictionary.ContainsKey(type))
        {
            return;
        }
        //딕셔너리에서 해당 BGM데이터를 가져온다.
        BGMClipData data = bgmDictionary[type];

        //현재 재생중인 BGM과 요청한 BGM이 같다면
        if(currentBGMData == data)
        {
            return;
        }

        if (!areBGMPlayersStarted)
        {
            StartSynchronizedBGM();
        }

        if (bgmBlendCoroutine != null)
        {
            StopCoroutine(bgmBlendCoroutine);
        }

        //현재 재생중인 BGM데이터를 저장
        currentBGMData = data;
        bgmBlendCoroutine = StartCoroutine(BlendBGM(data));
    }

    private IEnumerator BlendBGM(BGMClipData nextBGMData)
    {
        float startHubWeight = hubBGMBlendWeight;
        float startServiceWeight = serviceBGMBlendWeight;
        float startHarvestWeight = harvestBGMBlendWeight;

        float elapsedTime = 0f;

        while (elapsedTime < bgmBlendDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / bgmBlendDuration);

            float hubTargetWeight = nextBGMData.type == BGMType.HubBGM ? 1f : 0f;
            float serviceTargetWeight = nextBGMData.type == BGMType.ServiceBGM ? 1f : 0f;
            float harvestTargetWeight = nextBGMData.type == BGMType.HarvestBGM ? 1f : 0f;

            hubBGMBlendWeight = Mathf.Lerp(startHubWeight, hubTargetWeight, progress);
            serviceBGMBlendWeight = Mathf.Lerp(startServiceWeight, serviceTargetWeight, progress);
            harvestBGMBlendWeight = Mathf.Lerp(startHarvestWeight, harvestTargetWeight, progress);

            UpdateBGMVolume();
            yield return null;
        }

        hubBGMBlendWeight = nextBGMData.type == BGMType.HubBGM ? 1f : 0f;
        serviceBGMBlendWeight = nextBGMData.type == BGMType.ServiceBGM ? 1f : 0f;
        harvestBGMBlendWeight = nextBGMData.type == BGMType.HarvestBGM ? 1f : 0f;
        UpdateBGMVolume();

        bgmBlendCoroutine = null;
    }
    //BGM을 정지시키는 퓬
    public void StopBGM()
    {
        if (bgmBlendCoroutine != null)
        {
            StopCoroutine(bgmBlendCoroutine);
            bgmBlendCoroutine = null;
        }

        hubBGMSource.Stop();
        serviceBGMSource.Stop();
        harvestBGMSource.Stop();

        hubBGMSource.clip = null;
        serviceBGMSource.clip = null;
        harvestBGMSource.clip = null;

        hubBGMBlendWeight = 0f;
        serviceBGMBlendWeight = 0f;
        harvestBGMBlendWeight = 0f;

        areBGMPlayersStarted = false;
        //현재 재생중인 BGM데이터도 비우자.
        currentBGMData = null;
    }
    //일시 정지
    public void PauseBGM()
    {
        hubBGMSource.Pause();
        serviceBGMSource.Pause();
        harvestBGMSource.Pause();
    }
    //일시정지된 BGM을 다시 재생
    public void ResumeBGM()
    {
        hubBGMSource.UnPause();
        serviceBGMSource.UnPause();
        harvestBGMSource.UnPause();
    }

    //효과음 랜덤으로 Randomratio (0~0.2) 추천
    public void PlaySFXRandomPitch(SFXType type, float randomRatio)
    {
        if (!sfxDictionary.ContainsKey(type))
            return;

        SFXClipData data = sfxDictionary[type];
        // 현재 같은 효과음이 최대 개수 이상 재생 중이면 재생하지 않는다.
        if (playingCounts.TryGetValue(type, out int count))
        {
            if (count >= data.maxSimultaneousCount) return;
        }
        AudioSource source = GetSFXSource();
        if (source == null) return;

        float volume = data.volume * sfxVolume * masterVolume;
        // 현재 재생 중인 개수 증가
        if (!playingCounts.ContainsKey(type))
        {
            playingCounts[type] = 0;
        }
        playingCounts[type]++;

        source.pitch = data.pitch + Random.Range(-randomRatio, randomRatio);

        source.PlayOneShot(data.clip, volume);
        // 효과음이 끝나면 재생 중 개수를 감소시킨다.
        StartCoroutine(ReleaseVoice(type, data.clip.length));
    }
    //효과음 재생하는 녀석
    public void PlaySFX(SFXType type)
    {
        if (!sfxDictionary.ContainsKey(type)) return;

        SFXClipData data = sfxDictionary[type];
        //최대 개수를 넘으면 재생X
        if (playingCounts.TryGetValue(type, out int count))
        {
            if (count >= data.maxSimultaneousCount)
                return;
        }
        // 마지막 재생 시간이 저장되어 있다면
        if (lastPlayTimes.TryGetValue(type, out float lastPlayTime))
        {
            // 마지막 재생 후 아직 최소 재생 간격이 지나지 않았다면
            // 이번 재생은 무시한다.
            if (Time.time - lastPlayTime < data.minInterval) return;
        }
        // 이번 재생 시간을 저장한다.
        lastPlayTimes[type] = Time.time;

        AudioSource source = GetSFXSource();
        if (source == null) return;
        
        float volume = data.volume * sfxVolume * masterVolume;

        float pitch = data.pitch;
        //재생직전 개수 증가
        if (!playingCounts.ContainsKey(type))
        {
            playingCounts[type] = 0;
        }
        playingCounts[type]++;

        source.pitch = pitch;
        source.PlayOneShot(data.clip, volume);

        StartCoroutine(ReleaseVoice(type, data.clip.length));
    }
    //루프 효과음 재생
    public void PlayLoopSFX(SFXType type)
    {
        if (!sfxDictionary.ContainsKey(type)) return;

        SFXClipData data = sfxDictionary[type];
        if (!data.isLoop) return;
        if (loopingSFXSources.ContainsKey(type)) return;

        AudioSource source = GetSFXSource();
        if (source == null) return;

        source.clip = data.clip;
        source.volume = data.volume * sfxVolume * masterVolume;
        source.pitch = data.pitch;
        source.loop = true;
        source.Play();

        loopingSFXSources.Add(type, source);
    }

    public void StopLoopSFX(SFXType type)
    {
        if (!loopingSFXSources.TryGetValue(type, out AudioSource source)) return;

        source.Stop();
        source.clip = null;
        source.volume = 1f;
        source.loop = false;

        loopingSFXSources.Remove(type);
    }

    private void UpdateLoopSFXVolume()
    {
        foreach (KeyValuePair<SFXType, AudioSource> pair in loopingSFXSources)
        {
            SFXClipData data = sfxDictionary[pair.Key];
            pair.Value.volume = data.volume * sfxVolume * masterVolume;
        }
    }

    //효과음 종료 후 카운트 감소용 코루틴
    private IEnumerator ReleaseVoice(SFXType type, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playingCounts.ContainsKey(type))
        {
            playingCounts[type]--;

            if (playingCounts[type] < 0)
                playingCounts[type] = 0;
        }
    }

    //전체 볼륨을 변경하는 녀석
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
        UpdateLoopSFXVolume();
    }
    //BGM볼륨을 변경하는 녀석
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
    }
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateLoopSFXVolume();
    }

    public AudioSaveData CreateAudioSaveData()
    {
        return new AudioSaveData(masterVolume, bgmVolume, sfxVolume);
    }

    public void LoadAudioSaveData(AudioSaveData saveData)
    {
        if (saveData == null)
        {
            ResetAudioSaveData();
            return;
        }

        SetMasterVolume(saveData.masterVolume);
        SetBGMVolume(saveData.bgmVolume);
        SetSFXVolume(saveData.sfxVolume);
    }

    public void ResetAudioSaveData()
    {
        SetMasterVolume(1f);
        SetBGMVolume(1f);
        SetSFXVolume(1f);
    }

    //현재 재생중인 BGM의 볼륨을 계산
    private void UpdateBGMVolume()
    {
        if (bgmDictionary.Count == 0)
            return;

        hubBGMSource.volume = bgmDictionary[BGMType.HubBGM].volume * hubBGMBlendWeight * bgmVolume * masterVolume;
        serviceBGMSource.volume = bgmDictionary[BGMType.ServiceBGM].volume * serviceBGMBlendWeight * bgmVolume * masterVolume;
        harvestBGMSource.volume = bgmDictionary[BGMType.HarvestBGM].volume * harvestBGMBlendWeight * bgmVolume * masterVolume;
    }
}
