using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnigmeController : MonoBehaviour
{
    [SerializeField] private EnigmeDatas enigmeData;
    [SerializeField] private Light2D[] lights;
    [SerializeField] private Light2D WinLight;

    private void start()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = enigmeData.LampsActivated[i];
        }
        
        WinLight.enabled = enigmeData.IsAllActivated();
    }
    
}
