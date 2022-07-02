using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject loadingScreen;
    public Slider loadingSlider;

    void Awake(){
        instance = this;

        SceneManager.LoadSceneAsync((int)SceneIndexes.TitleScene, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    public void LoadGame(){
        loadingScreen.SetActive(true);

        scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.TitleScene));
        scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.Main, LoadSceneMode.Additive));

        StartCoroutine(GetSceneLoadProgress());
        StartCoroutine(GetTotalProgress());
    }

    float totalSceneProgress;
    float totalSpawnProgress;
    public IEnumerator GetSceneLoadProgress(){
        for (int i = 0; i < scenesLoading.Count; i++){
            while(!scenesLoading[i].isDone){
                totalSceneProgress = 0;
                foreach (AsyncOperation operation in scenesLoading)
                {
                    totalSceneProgress += operation.progress;
                }

                totalSceneProgress = (totalSceneProgress / scenesLoading.Count) * 100f;

                yield return null;
            }
        }

    }

    public IEnumerator GetTotalProgress(){
        float totalProgress = 0;

        while(SpawnEntityValidation.current == null || !SpawnEntityValidation.current.initialSpawnDone){
            if(SpawnEntityValidation.current == null){
                totalProgress = 0f;
            }else {
                totalSpawnProgress = Mathf.Round(SpawnEntityValidation.current.initialSpawnProgress * 100f);
            }

            totalProgress = Mathf.Round((totalSpawnProgress + totalSceneProgress) / 2f);
            loadingSlider.value = Mathf.RoundToInt(totalProgress);
        }


        loadingScreen.SetActive(false);
        yield return null;
    }
}
