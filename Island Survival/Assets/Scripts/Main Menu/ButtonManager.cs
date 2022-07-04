using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    GameManager gameManager;
    
    void Awake(){
        gameManager = GameManager.instance;
    }

    public void Play(){
        gameManager.LoadGame();
    }
    public void Quit(){
        Application.Quit();
    }
}
