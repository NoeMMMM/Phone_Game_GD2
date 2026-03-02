using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private ObjectMovement[] _fallingLines;
    [SerializeField] private int _pointsPerCatch = 1;

    private void OnEnable()
    {
        foreach (ObjectMovement line in _fallingLines)
            line.OnCaught += OnObjectCaught;
    }

    private void OnDisable()
    {
        foreach (ObjectMovement line in _fallingLines)
            line.OnCaught -= OnObjectCaught;
    }

    /// <summary>Called when the player catches a falling object on any line.</summary>
    private void OnObjectCaught()
    {
        _playerDatas.AddScore(_pointsPerCatch);
    }
}
