using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private ObjectMovement[] _fallingLines;
    [SerializeField] private GameObject _objectPrefab;
    [SerializeField] private float _spawnInterval = 3f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _spawnInterval)
            return;

        _timer = 0f;
        SpawnObject();
    }

    /// <summary>Spawns a new object on a random falling line.</summary>
    private void SpawnObject()
    {
        int randomLine = UnityEngine.Random.Range(0, _fallingLines.Length);
        _fallingLines[randomLine].Init(Instantiate(_objectPrefab));
    }
}
