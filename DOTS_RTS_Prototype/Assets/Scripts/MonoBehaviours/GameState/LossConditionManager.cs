using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Tracks player-critical ECS entities grouped by logical tags and triggers a game-over condition
/// when any tracked tag group becomes empty.
/// </summary>
/// <remarks>
/// Maintains both forward (tag → entities) and reverse (entity → tags) indices to support efficient
/// registration and removal without full scans.
/// </remarks>
public class LossConditionManager : MonoBehaviour
{
    /// <summary>
    /// Forward lookup: maps a tag to all currently tracked entities associated with it.
    /// </summary>
    private readonly Dictionary<string, HashSet<Entity>> tagsToCriticalEntitiesDict = new Dictionary<string, HashSet<Entity>>();

    /// <summary>
    /// Reverse lookup: maps an entity to all tags it is registered under.
    /// </summary>
    private readonly Dictionary<Entity, HashSet<string>> criticalEntityToTagsDict = new Dictionary<Entity, HashSet<string>>();

    private EntityManager entityManager;

    /// <summary>
    /// Global singleton instance of the manager.
    /// </summary>
    public static LossConditionManager Instance { get; private set; }

    /// <summary>
    /// Ensures singleton integrity for this manager instance.
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
    /// Unity lifecycle method. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Resolves ECS dependencies and subscribes to DOTS event system.
    /// </summary>
    /// <remarks>
    /// If required ECS world or event manager is unavailable, the system remains inactive.
    /// </remarks>
    private void Start()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            Debug.LogError("LossConditionManager could not resolve DefaultGameObjectInjectionWorld.");
            return;
        }

        if (DOTSEventManager.Instance == null)
        {
            Debug.LogError("LossConditionManager could not resolve DOTSEventManager instance.");
            return;
        }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        DOTSEventManager.Instance.OnCriticalConstruction += DOTSEventManager_OnCriticalConstruction;
        DOTSEventManager.Instance.OnCriticalDestruction += DOTSEventManager_OnCriticalDestruction;
    }

    /// <summary>
    /// Handles registration of newly constructed critical entities.
    /// </summary>
    /// <param name="sender">Event source (unused).</param>
    /// <param name="e">Event data containing the entity.</param>
    private void DOTSEventManager_OnCriticalConstruction(object sender, EntityEventArgs e)
    {
        Entity firingEntity = e.firingEntity;

        // Ensure entity is valid and contains required ECS components before processing.
        if (!EntityUtil.ExistsAndPersists(ref entityManager, ref firingEntity) ||
            !entityManager.HasComponent<GameOverOnGroupDeath>(firingEntity) ||
            !entityManager.HasBuffer<GameOverOnGroupDeathTag>(firingEntity))
        {
            return;
        }

        AddCriticalEntity(firingEntity);
    }

    /// <summary>
    /// Handles removal of destroyed critical entities and updates tracking structures.
    /// </summary>
    /// <param name="sender">Event source (unused).</param>
    /// <param name="e">Event data containing the entity.</param>
    /// <remarks>
    /// Performs a reverse lookup to avoid unnecessary processing for untracked entities.
    /// </remarks>
    private void DOTSEventManager_OnCriticalDestruction(object sender, EntityEventArgs e)
    {
        Entity firingEntity = e.firingEntity;

        if (!criticalEntityToTagsDict.TryGetValue(firingEntity, out HashSet<string> groupTags))
        {
            return;
        }

        RemoveCriticalEntity(firingEntity, groupTags);
        CheckForEmptyGroups(groupTags);
    }

    /// <summary>
    /// Registers an entity in both forward and reverse tracking structures and marks it as registered in ECS.
    /// </summary>
    /// <param name="e">Entity to register.</param>
    /// <remarks>
    /// Reads all tags from the entity's dynamic buffer and inserts it into corresponding tag groups.
    /// </remarks>
    private void AddCriticalEntity(Entity e)
    {
        DynamicBuffer<GameOverOnGroupDeathTag> tagsBuffer = entityManager.GetBuffer<GameOverOnGroupDeathTag>(e);
        if (tagsBuffer.Length <= 0)
        {
            return;
        }

        if (!criticalEntityToTagsDict.TryGetValue(e, out HashSet<string> tagsForEntity))
        {
            tagsForEntity = new HashSet<string>();
            criticalEntityToTagsDict[e] = tagsForEntity;
        }
        else
        {
            tagsForEntity.Clear();
        }

        foreach (GameOverOnGroupDeathTag tagElement in tagsBuffer)
        {
            string tag = tagElement.value.ToString();
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            if (!tagsToCriticalEntitiesDict.ContainsKey(tag))
            {
                tagsToCriticalEntitiesDict[tag] = new HashSet<Entity>();
            }

            tagsToCriticalEntitiesDict[tag].Add(e);
            tagsForEntity.Add(tag);
        }

        GameOverOnGroupDeath gameOverOnGroupDeath = entityManager.GetComponentData<GameOverOnGroupDeath>(e);
        gameOverOnGroupDeath.registered = true;
        entityManager.SetComponentData(e, gameOverOnGroupDeath);
    }

    /// <summary>
    /// Removes an entity from all tag groups and clears reverse lookup data.
    /// </summary>
    /// <param name="e">Entity to remove.</param>
    /// <param name="groupTags">Tags associated with the entity.</param>
    private void RemoveCriticalEntity(Entity e, IEnumerable<string> groupTags)
    {
        foreach (string tag in groupTags)
        {
            if (!tagsToCriticalEntitiesDict.ContainsKey(tag))
            {
                continue;
            }

            tagsToCriticalEntitiesDict[tag].Remove(e);
        }

        criticalEntityToTagsDict.Remove(e);
    }

    /// <summary>
    /// Evaluates whether any tag group has become empty and triggers game-over if necessary.
    /// </summary>
    /// <param name="groupTags">Tags to evaluate.</param>
    private void CheckForEmptyGroups(IEnumerable<string> groupTags)
    {
        foreach (string tag in groupTags)
        {
            if (!tagsToCriticalEntitiesDict.ContainsKey(tag))
            {
                continue;
            }

            if (tagsToCriticalEntitiesDict[tag].Count <= 0)
            {
                DOTSEventManager.Instance.TriggerOnGameOver($"You lost all your {tag}!");
            }
        }
    }
}