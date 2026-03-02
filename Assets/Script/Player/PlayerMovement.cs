using UnityEngine;

public class PlayerMovement : MonoBehaviour

{
    [SerializeField] private Transform[] m_transforms;
    [SerializeField] private InputPlayerManagerCustom m_inputManager;
    private int m_currentIndex = 2;
    private int m_movespeed = 1;

    /// <summary>Returns the player's current position index (0 to transforms.Length - 1).</summary>
    public int CurrentIndex => m_currentIndex;

    private void Start()
    {
        m_currentIndex = 2;
        UpdatePosition();
    }

    public void OnEnable()
    {
        m_inputManager.OnMoveLeft += MoveToPreviousPosition;
        m_inputManager.OnMoveRight += MoveToNextPosition;
    }

    public void OnDisable()
    {
        m_inputManager.OnMoveLeft -= MoveToPreviousPosition;
        m_inputManager.OnMoveRight -= MoveToNextPosition;  
    }
    
    public void MoveToNextPosition()
    {
        m_currentIndex += m_movespeed;
        m_currentIndex = Mathf.Clamp(m_currentIndex, 0, m_transforms.Length-1);
        UpdatePosition();
    }

    public void MoveToPreviousPosition()
    {
        m_currentIndex -= m_movespeed;
        m_currentIndex = Mathf.Clamp(m_currentIndex, 0, m_transforms.Length-1);
        UpdatePosition();
    }

    public void MoveToDirection(int direction) // direction = -1 ou 1
    {
        m_currentIndex = m_currentIndex + m_movespeed * direction;
        m_currentIndex = Mathf.Clamp(m_currentIndex, 0, m_transforms.Length-1);
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        transform.position = m_transforms[m_currentIndex].position;
    }
}
