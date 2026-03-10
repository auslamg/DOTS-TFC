using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class DOTSEventManager : MonoBehaviour
{
    public static DOTSEventManager Instance { get; private set;}
    public event EventHandler OnTrainerUnitQueueChange;

    /// <summary>
    /// Used for singleton logic.
    /// </summary>
    void Awake()
    {
        //Singleton logic
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }

    public void TriggerOnTrainerUnitQueueChange(NativeList<Entity> firingEntities)
    {
        foreach (Entity e in firingEntities)
        {
            OnTrainerUnitQueueChange?.Invoke(e, EventArgs.Empty);
        }        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
