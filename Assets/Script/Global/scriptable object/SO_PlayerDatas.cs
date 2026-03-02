using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerDatas", menuName = "Scriptable Objects/SO_PlayerDatas")]
public class SO_PlayerDatas : ScriptableObject
{
    public string Name;
    public int Score;
    public int Level;
    public int HighScore;

    private SaveController saveSystem;

    /// <summary>Adds points to the current score and updates the high score if beaten.</summary>
    public void AddScore(int amount)
    {
        Score += amount;
        if (Score > HighScore)
            HighScore = Score;
    }

    public void LoadDatas()
    {
        CheckSaveDatas();
        PlayerDatas datas = saveSystem.Load();
        Name = datas.Name;
        Score = datas.Score;
        Level = datas.Level;
        HighScore = datas.HighScore;
    }

    public void SaveDatas()
    {
        CheckSaveDatas();
        PlayerDatas datas = new PlayerDatas();
        datas.Name = Name;
        datas.Score = Score;
        datas.Level = Level;
        datas.HighScore = HighScore;
        saveSystem.Save(datas);
    }

    private void CheckSaveDatas()
    {
        if (saveSystem == null)
            saveSystem = new SaveController();
    }
}
