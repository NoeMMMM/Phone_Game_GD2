using UnityEngine;

public class SaveGameSystem : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerData;


    public void LoadSaveGAme()
    {
        playerData.LoadDatas();
    }

    public void SaveDatas()
    {
        playerData.SaveDatas();
    }
}
