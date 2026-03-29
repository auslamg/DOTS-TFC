using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Central MonoBehaviour event bridge used to relay ECS-originating gameplay events to UI and managed systems.
/// </summary>
/// <remarks>
/// ECS systems invoke the Trigger* methods in this class to publish high-level game events
/// without directly depending on UI MonoBehaviours.
/// </remarks>
public class DOTSEventManager : MonoBehaviour
{
    /// <summary>
    /// Raised when one or more trainer entities changed their training queue.
    /// </summary>
    public event EventHandler OnTrainerUnitQueueChange;

    /// <summary>
    /// Raised when a critical entity is constructed/registered.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnCriticalConstruction;

    /// <summary>
    /// Raised when a critical entity is destroyed/unregistered.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnCriticalDestruction;

    /// <summary>
    /// Raised when a game-over condition is reached.
    /// </summary>
    public event EventHandler<MsgEventArgs> OnGameOver;

    /// <summary>
    /// Global singleton access to the DOTS event bridge.
    /// </summary>
    public static DOTSEventManager Instance { get; private set;}

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void Awake()
    {
        // Initialize singleton instance state.
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

    /// <summary>
    /// Emits queue-change notifications for each entity in the provided list.
    /// </summary>
    /// <param name="firingEntities">Entities whose queue changed this frame.</param>
    public void TriggerOnTrainerUnitQueueChange(NativeList<Entity> firingEntities)
    {
        foreach (Entity e in firingEntities)
        {
            OnTrainerUnitQueueChange?.Invoke(e, EventArgs.Empty);
        }        
    }

    /// <summary>
    /// Emits a critical-construction event.
    /// </summary>
    /// <param name="firingEntity">Entity that was registered as critical.</param>
    public void TriggerOnCriticalConstruction(in Entity firingEntity)
    {
        OnCriticalConstruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    /// <summary>
    /// Emits a critical-destruction event.
    /// </summary>
    /// <param name="firingEntity">Entity that was removed as critical.</param>
    public void TriggerOnCriticalDestruction(in Entity firingEntity)
    {
        OnCriticalDestruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    /// <summary>
    /// Emits a game-over event with the supplied display message.
    /// </summary>
    /// <param name="msg">Message shown by game-over UI consumers.</param>
    public void TriggerOnGameOver(string msg)
    {
        OnGameOver?.Invoke(this, new MsgEventArgs(msg));
    }
}

/// <summary>
/// Event arguments carrying a single ECS entity reference.
/// </summary>
public class EntityEventArgs : EventArgs
{
    /// <summary>
    /// Entity that triggered the event.
    /// </summary>
    public Entity firingEntity { get; }

    /// <summary>
    /// Creates event args for an entity-triggered event.
    /// </summary>
    /// <param name="firingEntity">Entity that triggered the event.</param>
    public EntityEventArgs(Entity firingEntity)
    {
        this.firingEntity = firingEntity;
    }
}

/// <summary>
/// Event arguments carrying a message payload.
/// </summary>
public class MsgEventArgs : EventArgs
{
    /// <summary>
    /// Message payload.
    /// </summary>
    public string msg { get; }

    /// <summary>
    /// Creates event args for a message-based event.
    /// </summary>
    /// <param name="msg">Message payload.</param>
    public MsgEventArgs(string msg)
    {
        this.msg = msg;
    }
}
