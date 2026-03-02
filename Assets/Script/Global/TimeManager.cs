using System.Collections;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private float _timeStepDuration = 5.0f;
    
    public event Action OnTimePassed;
    
    
    IEnumerator SpendingTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            OnTimePassed?.Invoke();
        }    
        
    }


    private void Start()
    {
        StartTime();
    }


    public void StopTime()
    {
        StopCoroutine(SpendingTime());
    }
    
    public void StartTime()
    {
        StartCoroutine(SpendingTime());
    }
}
