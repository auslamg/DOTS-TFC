using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Tracks player-critical entities by one or more logical tags and triggers a loss condition
/// when any tracked tag becomes empty.
/// </summary>
/// <remarks>
/// Design overview:
/// - Entities are tracked in two maps:
///   1) tag -> set of entities, used to evaluate if a tag bucket is empty.
///   2) entity -> set of tags, used to resolve all tag buckets for a specific entity.
/// - Tags are read from <see cref="GameOverOnGroupDeathTag"/> dynamic buffers.
/// - Construction callbacks can safely read data only after explicit existence/component guards.
/// </remarks>
public class LossConditionManager : MonoBehaviour
{
    /// <summary>
    /// Forward index for gameplay checks.
    /// Key: tag.
    /// Value: all currently tracked critical entities belonging to that tag.
    /// </summary>
    private readonly Dictionary<string, HashSet<Entity>> tagsToCriticalEntitiesDict = new Dictionary<string, HashSet<Entity>>();

    /// <summary>
    /// Reverse index for targeted removals.
    /// Key: entity.
    /// Value: all tags the entity belongs to.
    /// </summary>
    private readonly Dictionary<Entity, HashSet<string>> criticalEntityToTagsDict = new Dictionary<Entity, HashSet<string>>();

    private EntityManager entityManager;

    public static LossConditionManager Instance { get; private set; }

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
    /// Resolves runtime dependencies and subscribes to DOTS bridge events.
    /// </summary>
    /// <remarks>
    /// Startup bails out early if either the default world or event manager is not available.
    /// In that case the manager remains inactive and does not process critical entity events.
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
    /// Handles critical-entity construction events and registers the firing entity for loss tracking.
    /// </summary>
    /// <param name="sender">Event source (unused).</param>
    /// <param name="e">Event args containing the firing entity.</param>
    private void DOTSEventManager_OnCriticalConstruction(object sender, EntityEventArgs e)
    {
        Entity firingEntity = e.firingEntity;

        // Validate entity lifetime and required component before reading ECS data.
        if (!EntityUtil.ExistsAndPersists(ref entityManager, ref firingEntity) ||
            !entityManager.HasComponent<GameOverOnGroupDeath>(firingEntity) ||
            !entityManager.HasBuffer<GameOverOnGroupDeathTag>(firingEntity))
        {
            return;
        }


        AddCriticalEntity(firingEntity);

    }

    /// <summary>
    /// Handles critical-entity destruction events and removes the firing entity from
    /// all tracked tag buckets.
    /// </summary>
    /// <param name="sender">Event source (unused).</param>
    /// <param name="e">Event args containing the firing entity.</param>
    /// <remarks>
    /// This handler attempts a reverse-lookup first and exits early when the entity was never
    /// tracked by this manager.
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
    /// Adds an entity to tracking structures and marks its ECS component as registered.
    /// </summary>
    /// <param name="e">Entity to add.</param>
    /// <remarks>
    /// The entity may contribute to multiple tags. For each buffered tag, the entity is inserted
    /// into the forward index and mirrored in the reverse index.
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

            //Add entity to dictionary tag entry
            if (!tagsToCriticalEntitiesDict.ContainsKey(tag))
            {
                tagsToCriticalEntitiesDict[tag] = new HashSet<Entity>();
            }

            tagsToCriticalEntitiesDict[tag].Add(e);
            tagsForEntity.Add(tag);
        }

        // Set as registered
        GameOverOnGroupDeath gameOverOnGroupDeath = entityManager.GetComponentData<GameOverOnGroupDeath>(e);
        gameOverOnGroupDeath.registered = true;
        entityManager.SetComponentData(e, gameOverOnGroupDeath);
    }

    /// <summary>
    /// Removes an entity from both the reverse and forward tracking maps.
    /// </summary>
    /// <param name="e">Entity to remove.</param>
    /// <param name="groupTags">Tags associated with the entity in the reverse map.</param>
    /// <remarks>
    /// Removal iterates all known tags for the entity and removes
    /// the entity from each corresponding forward bucket.
    /// </remarks>
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
    /// Triggers game over for each tag that becomes empty after a removal.
    /// </summary>
    /// <param name="groupTags">Tags that should be evaluated.</param>
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
