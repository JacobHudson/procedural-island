using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetWorldInfomation : MonoBehaviour{
    public TMP_InputField noiseSeedInput;
    public TMP_InputField entitySeedInput;
    public TMP_InputField octavesInput;
    public TMP_InputField noiseScaleInput;
    public TMP_InputField lacunarityInput;
    public Slider persistanceInput;
    public TMP_Text persistanceText;
    public TMP_Text startButtonText;

    public void SetSeeds(){
        if(string.IsNullOrEmpty(noiseSeedInput.text)){
            noiseSeedInput.text = GameManager.instance.DEFAULT_NOISE_DATA.seed.ToString();
        }
        if(string.IsNullOrEmpty(entitySeedInput.text)){
            entitySeedInput.text = GameManager.instance.DEFAULT_ENTITY_DATA.seed.ToString();
        }

        if(string.IsNullOrEmpty(octavesInput.text)){
            octavesInput.text = GameManager.instance.DEFAULT_NOISE_DATA.octaves.ToString();
        }
        if(string.IsNullOrEmpty(noiseScaleInput.text)){
            noiseScaleInput.text = GameManager.instance.DEFAULT_NOISE_DATA.noiseScale.ToString();
        }
        if(string.IsNullOrEmpty(lacunarityInput.text)){
            lacunarityInput.text = GameManager.instance.DEFAULT_NOISE_DATA.lacunarity.ToString();
        }
        if(persistanceInput.value == 0){
            persistanceInput.value = GameManager.instance.DEFAULT_NOISE_DATA.persistance * 100;
        }


        GameManager.instance.noiseData = GameManager.instance.CUSTOM_NOISE_DATA;
        GameManager.instance.entityData = GameManager.instance.CUSTOM_ENTITY_DATA;

        GameManager.instance.noiseData.seed = int.Parse(noiseSeedInput.text);
        GameManager.instance.entityData.seed = int.Parse(entitySeedInput.text);

        GameManager.instance.noiseData.octaves = int.Parse(octavesInput.text);
        GameManager.instance.noiseData.noiseScale = float.Parse(noiseScaleInput.text);
        GameManager.instance.noiseData.lacunarity = float.Parse(lacunarityInput.text);
        GameManager.instance.noiseData.persistance = persistanceInput.value / 100f;


        startButtonText.text = "Generate World Based On World Info";
    }
    
    public void UpdatePersistanceText(){
        persistanceText.text = (persistanceInput.value / 100f).ToString();
    }
}
