using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialType
    {
        Hub,
        BeforeHarvest,
        HarvestUpgrade,
        Facility,
        Menu,
        Staff,
        ServiceSelection,
        Sales,
        Harvest,
        Length
    }

    private Dictionary<TutorialType, bool> tutorialProgressionMap = new();

    private void Awake()
    {
        Initialize();
    }

    public bool GetTutorialProgressed(TutorialType type)
    {
        if (type < 0 || type >= TutorialType.Length)
            return false;

        return tutorialProgressionMap[type];
    }

    public void ResolveTutorial(TutorialType type)
    {
        if (type < 0 || type >= TutorialType.Length)
        {
            Debug.LogError($"Invalid tutorial type: {type}");
            return;
        }

        tutorialProgressionMap[type] = true;
    }

    public void Initialize()
    {
        tutorialProgressionMap = new Dictionary<TutorialType, bool>();

        for (int i = 0; i < (int)TutorialType.Length; i++)
            tutorialProgressionMap.Add((TutorialType)i, false);
    }

    public List<TutorialSaveData> CreateTutorialSaveData()
    {
        List<TutorialSaveData> saveData = new();

        foreach (KeyValuePair<TutorialType, bool> pair in tutorialProgressionMap)
            saveData.Add(new TutorialSaveData(pair.Key.ToString(), pair.Value));

        return saveData;
    }

    public void LoadTutorialSaveData(List<TutorialSaveData> saveData)
    {
        Initialize();

        if (saveData == null)
            return;

        foreach (TutorialSaveData savedState in saveData)
        {
            if (savedState == null
                || !Enum.TryParse(savedState.id, out TutorialType type)
                || (int)type < 0
                || (int)type >= (int)TutorialType.Length)
                continue;

            tutorialProgressionMap[type] = savedState.flag;
        }
    }

    public void ResetTutorialSaveData()
    {
        Initialize();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Log Tutorial Progress")]
    private void LogTutorialProgress()
    {
        foreach (KeyValuePair<TutorialType, bool> pair in tutorialProgressionMap)
            Debug.Log($"{pair.Key}: {pair.Value}");
    }
#endif
}
