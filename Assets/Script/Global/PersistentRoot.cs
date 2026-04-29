using UnityEngine;

/// <summary>
/// À placer uniquement sur le GameObject racine "_PersistentManagers".
/// Appelle DontDestroyOnLoad sur lui-même au Awake, ce qui fait persister
/// l'intégralité de son sous-arbre d'enfants (SceneLoader, SwipeInputReader, etc.)
/// entre les changements de scène.
///
/// IMPORTANT : les singletons enfants (SwipeInputReader, GlobalTimerManager, etc.)
/// NE doivent PAS appeler DontDestroyOnLoad sur eux-mêmes — Unity sortirait
/// chaque enfant du parent et les placerait à la racine de DontDestroyOnLoad,
/// cassant la hiérarchie. Seul ce script fait l'appel, une seule fois, sur le parent.
/// </summary>
public class PersistentRoot : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
