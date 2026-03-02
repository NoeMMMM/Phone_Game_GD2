using System;
using UnityEngine;

public class ObjectMovement : MonoBehaviour
{
    [SerializeField] private Transform[] _transforms;
    [SerializeField] private int _lineIndex;
    [SerializeField] private float _stepDuration = 1f;
    [SerializeField] private ObjectMovement[] _allFallingLines;
    [SerializeField] private PlayerMovement _player;
    [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
    [SerializeField] private AudioType _stepAudioType;
    [SerializeField] private AudioType _missAudioType;

    /// <summary>Fired when the object reaches the bottom and the player is on this line.</summary>
    public event Action OnCaught;

    /// <summary>Fired when the object reaches the bottom and the player is NOT on this line.</summary>
    public event Action OnMissed;

    private GameObject _fallingObject;
    private int _step;
    private float _timer;
    private bool _waitingAtBottom;

    private void Update()
    {
        if (_fallingObject == null)
            return;

        _timer += Time.deltaTime;
        if (_timer < _stepDuration)
            return;

        _timer = 0f;

        if (_waitingAtBottom)
        {
            TransferToNewLine();
            return;
        }

        Step();
    }

    /// <summary>Assigns a falling object to this line and starts it from the top.</summary>
    public void Init(GameObject obj)
    {
        if (_fallingObject != null)
            Destroy(_fallingObject);

        _fallingObject = obj;
        _step = 0;
        _timer = 0f;
        _waitingAtBottom = false;

        _fallingObject.transform.position = _transforms[0].position;
        _audioEventDispatcher.PlayAudio(_stepAudioType);
    }

    private void Step()
    {
        _step++;

        if (_step >= _transforms.Length)
            return;

        _fallingObject.transform.position = _transforms[_step].position;
        _audioEventDispatcher.PlayAudio(_stepAudioType);

        // Object is now visually at the last spot — check and wait one beat before transferring
        if (_step == _transforms.Length - 1)
            ResolveBottom();
    }

    private void ResolveBottom()
    {
        bool caught = _player != null && _player.CurrentIndex == _lineIndex;

        if (caught)
        {
            OnCaught?.Invoke();
            _audioEventDispatcher.PlayAudio(_stepAudioType);
        }
        else
        {
            Debug.Log("Lose");
            OnMissed?.Invoke();
            _audioEventDispatcher.PlayAudio(_missAudioType);
        }

        // Flag to wait one more beat before moving to the next line
        _waitingAtBottom = true;
    }

    private void TransferToNewLine()
    {
        _waitingAtBottom = false;

        // Pick a random line that is not this one
        int randomLine;
        do { randomLine = UnityEngine.Random.Range(0, _allFallingLines.Length); }
        while (_allFallingLines.Length > 1 && _allFallingLines[randomLine] == this);

        _allFallingLines[randomLine].Init(_fallingObject);

        _fallingObject = null;
        _step = 0;
        _timer = 0f;
    }
}