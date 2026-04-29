using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fait apparaître des bombes aléatoirement sur les lanes libres à intervalles variables.
/// Délègue le tracking d'occupation à BombRecycler.
/// Ne connaît ni le chevalier, ni l'explosion, ni le score.
/// </summary>
public class BombSpawner : MonoBehaviour
{
    // ── Classe interne sérialisable ───────────────────────────────────────────

    /// <summary>
    /// Contient les points de téléportation d'une lane de descente.
    /// Nécessaire car Transform[][] n'est pas sérialisable par Unity.
    /// </summary>
    [Serializable]
    public class LaneData
    {
        [Tooltip("Points de téléportation de cette lane, du haut (Position 0) vers le bas (Position 3)")]
        public Transform[] descentPoints;
    }

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Prefab de la bombe — doit avoir un composant BombFallController")]
    private GameObject _bombPrefab;

    [SerializeField, Tooltip("Point d'apparition en haut de chaque lane (doit correspondre à descentPoints[0])")]
    private Transform[] _laneSpawnPoints;

    [SerializeField, Tooltip("4 lanes, une par soldat — chacune contient ses points de descente")]
    private LaneData[] _lanes;

    [SerializeField, Tooltip("Délai minimum entre deux spawns (secondes)")]
    private float _minSpawnInterval = 1f;

    [SerializeField, Tooltip("Délai maximum entre deux spawns (secondes)")]
    private float _maxSpawnInterval = 3f;

    [SerializeField, Tooltip("Référence au BombRecycler de la scène")]
    private BombRecycler _recycler;

    [SerializeField, Tooltip("Délai minimum garanti entre deux spawns, indépendamment de l'intervalle aléatoire. " +
                             "Doit être >= durée totale de descente pour éviter que deux bombes arrivent au sol simultanément.")]
    private float _minIntervalBetweenSpawns = 2f;

    // ── État interne ──────────────────────────────────────────────────────────

    private bool      _isActive;
    private Coroutine _spawnCoroutine;

    /// <summary>
    /// Timestamp du dernier spawn réel. Initialisé à une valeur très basse
    /// pour que le premier spawn ne soit jamais bloqué.
    /// </summary>
    private float _lastSpawnTime = -999f;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Démarre le spawn de bombes.
    /// À appeler depuis GameFlowManager ou SpawnerDebugger au début du Jeu 1.
    /// </summary>
    public void StartSpawning()
    {
        if (!ValidateReferences()) return;

        _isActive = true;

        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);

        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Arrête le spawn de bombes. À appeler quand le Jeu 1 est gagné ou terminé.
    /// Les bombes déjà en descente continuent jusqu'à leur fin naturelle.
    /// </summary>
    public void StopSpawning()
    {
        _isActive = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    // ── Coroutine de spawn ────────────────────────────────────────────────────

    private IEnumerator SpawnRoutine()
    {
        while (_isActive)
        {
            float interval = UnityEngine.Random.Range(_minSpawnInterval, _maxSpawnInterval);
            Debug.Log($"[BombSpawner] Prochain spawn dans {interval:F2}s."); // LOG TEMPORAIRE
            yield return new WaitForSeconds(interval);

            // Garantit un délai minimum absolu entre deux spawns.
            // Si l'intervalle aléatoire était trop court, on attend la différence en plus.
            float timeSinceLast = Time.time - _lastSpawnTime;
            if (timeSinceLast < _minIntervalBetweenSpawns)
            {
                float extra = _minIntervalBetweenSpawns - timeSinceLast;
                Debug.Log($"[BombSpawner] Filet min actif — attente supplémentaire {extra:F2}s."); // LOG TEMPORAIRE
                yield return new WaitForSeconds(extra);
            }

            List<int> freeLanes = _recycler.GetFreeLanes();
            Debug.Log($"[BombSpawner] Lanes libres : {freeLanes.Count} / {_lanes.Length}"); // LOG TEMPORAIRE

            // Toutes les lanes sont occupées — on retentera après le prochain délai
            if (freeLanes.Count == 0)
                continue;

            int laneIndex = freeLanes[UnityEngine.Random.Range(0, freeLanes.Count)];
            Debug.Log($"[BombSpawner] Lane choisie : {laneIndex}"); // LOG TEMPORAIRE

            SpawnBomb(laneIndex);
        }
    }

    /// <summary>
    /// Instancie une bombe sur la lane donnée et l'initialise.
    /// </summary>
    private void SpawnBomb(int laneIndex)
    {
        // OccupyLane en premier — avant même l'instanciation — pour qu'aucune
        // autre itération ne puisse choisir cette lane si la coroutine reprend.
        _recycler.OccupyLane(laneIndex);

        Vector3 spawnPosition = _laneSpawnPoints[laneIndex].position;
        Debug.Log($"[BombSpawner] Instanciation bombe — lane {laneIndex}, position {spawnPosition}"); // LOG TEMPORAIRE
        GameObject bombObj    = Instantiate(_bombPrefab, spawnPosition, Quaternion.identity);

        if (!bombObj.TryGetComponent(out BombFallController bomb))
        {
            Debug.LogError("[BombSpawner] Le prefab assigné ne possède pas de composant BombFallController.");
            _recycler.FreeLane(laneIndex);
            Destroy(bombObj);
            return;
        }

        _lastSpawnTime = Time.time;
        bomb.Initialize(_lanes[laneIndex].descentPoints, laneIndex);
        Debug.Log($"[BombSpawner] Bombe initialisée — lane {laneIndex}, {_lanes[laneIndex].descentPoints.Length} points de descente."); // LOG TEMPORAIRE
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateReferences()
    {
        if (_bombPrefab == null)
        {
            Debug.LogError("[BombSpawner] _bombPrefab non assigné.");
            return false;
        }

        if (_recycler == null)
        {
            Debug.LogError("[BombSpawner] _recycler non assigné.");
            return false;
        }

        if (_lanes == null || _lanes.Length == 0)
        {
            Debug.LogError("[BombSpawner] _lanes non configuré.");
            return false;
        }

        if (_laneSpawnPoints == null || _laneSpawnPoints.Length != _lanes.Length)
        {
            Debug.LogError($"[BombSpawner] _laneSpawnPoints doit avoir {_lanes.Length} entrées.");
            return false;
        }

        return true;
    }
}
