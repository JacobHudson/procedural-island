using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject LoadingScreen;
    public Slider loadingBar;
    public TMP_Text loadingText;
    MapGenerator mapGenerator;

    void Awake(){
        instance = this;
        SceneManager.LoadSceneAsync((int)SceneIndexes.MainMenu, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    public void LoadGame(){
        LoadingScreen.SetActive(true);

        scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.MainMenu));
        scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.Main, LoadSceneMode.Additive));
        
        StartCoroutine(GetSceneLoadProgress());
        StartCoroutine(GetTotalProgress());
    }

    float totalSceneProgress;
    float totalSpawnProgress;
    public IEnumerator GetSceneLoadProgress(){
        for (int i = 0; i < scenesLoading.Count; i++){
            while (!scenesLoading[i].isDone){
                totalSceneProgress = 0;

                foreach(AsyncOperation operation in scenesLoading){
                    totalSceneProgress += operation.progress;
                }

                loadingText.text = $"Loading Scene {i + 1}/{scenesLoading.Count}";
                totalSceneProgress = (totalSceneProgress / scenesLoading.Count) * 100f;
                yield return null;
            }
        }

        mapGenerator = MapGenerator.instance;
    }

    public IEnumerator GetTotalProgress(){
        yield return new WaitForSeconds(0.1f);
        float totalProgress = 0;

        while(mapGenerator == null || !mapGenerator.initialEntitiesSpawned){ // checks if all entitys have spawned or if the map generator is null
            totalSpawnProgress = mapGenerator.initialChunkSpawnProgress;

            totalProgress = Mathf.Round((totalSceneProgress + totalSpawnProgress) / 2);

            loadingBar.value = Mathf.RoundToInt(totalProgress);
            string currentLoadingStageText = $"Spawning {mapGenerator.currentObjectSpawningName} instance number [{mapGenerator.spawnedEntityCount}]";
            loadingText.text = currentLoadingStageText;

            yield return null;
        }

        loadingText.text = "Done!";
        loadingBar.value = 100;
        yield return new WaitForSeconds(0.5f);
        LoadingScreen.SetActive(false);
    }
}
