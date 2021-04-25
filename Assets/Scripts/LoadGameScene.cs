using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGameScene : MonoBehaviour
{
    public void onStartPressed()
    {
        SceneManager.LoadScene("GameScene");
    }
}
