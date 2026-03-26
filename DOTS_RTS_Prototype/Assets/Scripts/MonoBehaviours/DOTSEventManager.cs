using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class DOTSEventManager : MonoBehaviour
{
    public event EventHandler OnTrainerUnitQueueChange;

    public event EventHandler<EntityEventArgs> OnCriticalConstruction;
    public event EventHandler<EntityEventArgs> OnCriticalDestruction;

    public event EventHandler<MsgEventArgs> OnGameOver;
    public static DOTSEventManager Instance { get; private set;}

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

    public void TriggerOnCriticalConstruction(in Entity firingEntity)
    {
        OnCriticalConstruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    public void TriggerOnCriticalDestruction(in Entity firingEntity)
    {
        OnCriticalDestruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    public void TriggerOnGameOver(string msg)
    {
        OnGameOver?.Invoke(this, new MsgEventArgs(msg));
    }
}

public class EntityEventArgs : EventArgs
{
    public Entity firingEntity { get; }

    public EntityEventArgs(Entity firingEntity)
    {
        this.firingEntity = firingEntity;
    }
}

public class MsgEventArgs : EventArgs
{
    public string msg { get; }

    public MsgEventArgs(string msg)
    {
        this.msg = msg;
    }
}
