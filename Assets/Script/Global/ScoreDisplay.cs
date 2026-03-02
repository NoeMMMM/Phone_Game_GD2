using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas _playerDatas;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _highScoreText;

    private void Update()
    {
        _scoreText.text = "Score : " + _playerDatas.Score.ToString();
        _highScoreText.text = "Highscore : " + _playerDatas.HighScore.ToString();
    }
}
