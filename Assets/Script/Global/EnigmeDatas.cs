using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnigmeDatas", menuName = "Scriptable Objects/EnigmeDatas")]
public class EnigmeDatas : ScriptableObject
{
    public bool[] LampsActivated;
    
    public bool IsAllActivated()
    {
        return !LampsActivated.Contains(false); 
        
        for (int i = 0; i < LampsActivated.Length; i++)
        {
            if (LampsActivated[i])
            {
                return false;
            }
        }
        return true;  
        
        
    }
}
