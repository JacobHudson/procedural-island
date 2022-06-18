using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject{
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

    protected virtual void OnValidate(){ // protected virtual ensures it runs and dosnt clash with NoiseData.cs OnValidate() method
        if(autoUpdate){
            NotifyOfUpdatedVaules();
        }
    }

    public void NotifyOfUpdatedVaules(){
        if(OnValuesUpdated != null){
            OnValuesUpdated();
        }
    } 
}
