using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    GameManager gameManager;
    public void Awake(){
        gameManager = GameManager.instance;
    }

    public void Play(){
        gameManager.LoadGame();
    }
}
