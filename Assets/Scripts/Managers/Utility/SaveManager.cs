using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private string saveFileName = "save.json";

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Start()
    {
        LoadGame();
    }// 게임 시작 시 자동 로드 옵션이 켜져 있으면 저장 파일을 불러온다.
    // 저장 파일이 없으면 아무것도 적용하지 않는다.

    private void OnApplicationQuit()
    {
        SaveGame();
    }// 게임 종료 시 자동 저장 옵션이 켜져 있으면 현재 데이터를 저장한다.
    // 에디터 플레이 종료 시에도 호출될 수 있다.


    public void SaveGame()
    {
        GameSaveData saveData = CreateSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);
        Debug.Log($"Save complete. path : {SavePath}");
    }// 현재 게임 상태를 GameSaveData로 만들고 JSON 파일로 저장한다.
    // 저장 위치는 Application.persistentDataPath 안이다.
    // todo 업그레이드를 할때 저장하기


    public void LoadGame()
    {
        if (!HasSave())
        {
            ResetSave();
            Debug.Log($"Save file does not exist. path : {SavePath}");
            return;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (saveData == null)
        {
            Debug.LogWarning("Save data is null.");
            return;
        }

        ApplySaveData(saveData);
        Debug.Log($"Load complete. path : {SavePath}");
    }// 저장 파일을 읽어서 GameSaveData로 변환한다.
    // 변환된 데이터를 현재 게임 상태에 적용한다.

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        if (!HasSave())
            return;

        File.Delete(SavePath);
        Debug.Log($"Save deleted. path : {SavePath}");
    }// 저장 파일이 있으면 삭제한다.

    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        GameManager.Instance.Upgrade.ResetUpgradeSaveData();
        GameManager.Instance.Utility.Tutorial.ResetTutorialSaveData();
        GameManager.Instance.Utility.Audio.ResetAudioSaveData();
        GameManager.Instance.StockManager.ResetStockSaveData();
        DeleteSave();
    }

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }// 저장 파일이 존재하는지 확인한다.

    private GameSaveData CreateSaveData()
    {
        GameSaveData saveData = new();
        saveData.upgrades = GameManager.Instance.Upgrade.CreateUpgradeSaveData();
        saveData.tutorials = GameManager.Instance.Utility.Tutorial.CreateTutorialSaveData();
        saveData.audio = GameManager.Instance.Utility.Audio.CreateAudioSaveData();
        saveData.stock = GameManager.Instance.StockManager.CreateStockSaveData();
        return saveData;
    }// 저장할 전체 데이터를 만든다.

    private void ApplySaveData(GameSaveData saveData)
    {
        GameManager.Instance.Upgrade.LoadUpgradeSaveData(saveData.upgrades);
        GameManager.Instance.Utility.Tutorial.LoadTutorialSaveData(saveData.tutorials);
        GameManager.Instance.Utility.Audio.LoadAudioSaveData(saveData.audio);
        GameManager.Instance.StockManager.LoadStockSaveData(saveData.stock);
    }// 불러온 저장 데이터를 실제 게임 상태에 반영한다.
}
