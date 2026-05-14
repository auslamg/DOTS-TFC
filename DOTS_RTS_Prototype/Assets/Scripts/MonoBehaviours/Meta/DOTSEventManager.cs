using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Central MonoBehaviour event bridge used to relay ECS-originating gameplay events
/// to UI and managed systems without direct coupling.
/// </summary>
/// <remarks>
/// ECS systems should invoke the Trigger* methods to publish high-level gameplay events
/// instead of directly referencing UI or MonoBehaviour logic.
/// </remarks>
public class DOTSEventManager : MonoBehaviour
{
    /// <summary>
    /// Raised when one or more trainer entities update their unit queue.
    /// </summary>
    public event EventHandler OnTrainerUnitQueueChange;

    /// <summary>
    /// Raised when a tracked critical entity is removed or dies.
    /// </summary>
    public event EventHandler OnSelectedDeath;

    /// <summary>
    /// Raised when a critical entity is constructed or registered.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnCriticalConstruction;

    /// <summary>
    /// Raised when a critical entity is destroyed or unregistered.
    /// </summary>
    public event EventHandler<EntityEventArgs> OnCriticalDestruction;

    /// <summary>
    /// Raised when the game over condition is triggered.
    /// </summary>
    public event EventHandler<MsgEventArgs> OnGameOver;

    /// <summary>
    /// Global singleton instance of the DOTS event bridge.
    /// </summary>
    public static DOTSEventManager Instance { get; private set; }

    /// <summary>
    /// Ensures singleton instance validity.
    /// </summary>
    private void InitializeSingleton()
    {
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
    /// Unity Awake callback. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Triggers queue-change events for each entity in the provided list.
    /// </summary>
    /// <param name="firingEntities">Entities whose queues have changed.</param>
    public void TriggerOnTrainerUnitQueueChange(NativeList<Entity> firingEntities)
    {
        foreach (Entity e in firingEntities)
        {
            OnTrainerUnitQueueChange?.Invoke(e, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Triggers an event indicating a selected entity has been removed or died.
    /// </summary>
    public void TriggerOnSelectedDeath()
    {
        OnSelectedDeath?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Triggers a critical entity construction event.
    /// </summary>
    /// <param name="firingEntity">Entity that was registered as critical.</param>
    public void TriggerOnCriticalConstruction(in Entity firingEntity)
    {
        OnCriticalConstruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    /// <summary>
    /// Triggers a critical entity destruction event.
    /// </summary>
    /// <param name="firingEntity">Entity that was unregistered as critical.</param>
    public void TriggerOnCriticalDestruction(in Entity firingEntity)
    {
        OnCriticalDestruction?.Invoke(this, new EntityEventArgs(firingEntity));
    }

    /// <summary>
    /// Triggers a game-over event using a fixed string message.
    /// </summary>
    /// <param name="msg">Message displayed by game-over consumers.</param>
    public void TriggerOnGameOver(FixedString64Bytes msg)
    {
        OnGameOver?.Invoke(this, new MsgEventArgs(msg));
    }

    /// <summary>
    /// Triggers a game-over event using a standard string message.
    /// </summary>
    /// <param name="msg">Message displayed by game-over consumers.</param>
    public void TriggerOnGameOver(string msg)
    {
        OnGameOver?.Invoke(this, new MsgEventArgs(msg));
    }
}

/// <summary>
/// Event arguments containing a single ECS entity reference.
/// </summary>
public class EntityEventArgs : EventArgs
{
    /// <summary>
    /// Entity associated with the event.
    /// </summary>
    public Entity firingEntity { get; }

    /// <summary>
    /// Initializes event arguments with the specified entity.
    /// </summary>
    /// <param name="firingEntity">Entity that triggered the event.</param>
    public EntityEventArgs(Entity firingEntity)
    {
        this.firingEntity = firingEntity;
    }
}

/// <summary>
/// Event arguments containing a message payload.
/// </summary>
public class MsgEventArgs : EventArgs
{
    /// <summary>
    /// Message associated with the event.
    /// </summary>
    public string msg { get; }

    /// <summary>
    /// Initializes event arguments with a string message.
    /// </summary>
    /// <param name="msg">Message payload.</param>
    public MsgEventArgs(string msg)
    {
        this.msg = msg;
    }

    /// <summary>
    /// Initializes event arguments with a fixed string message.
    /// </summary>
    /// <param name="msg">Message payload.</param>
    public MsgEventArgs(FixedString64Bytes msg)
    {
        this.msg = msg.ToString();
    }
}