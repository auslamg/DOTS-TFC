using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry ScriptableObject containing prefab GameObjects used for ECS prefab mapping.
/// </summary>
/// <remarks>
/// Builds a cached set of <see cref="EntityPrefabKey"/> values and keeps the serialized list sorted
/// for deterministic baking and lookup behavior.
/// </remarks>
[CreateAssetMenu(fileName = "PrefabRegistrySO", menuName = "Editor/PrefabRegistrySO")]
public class PrefabRegistrySO : ScriptableObject
{
    /// <summary>
    /// Serialized prefab GameObjects used by entity prefab registry baking.
    /// </summary>
    [SerializeField]
    [Tooltip("Prefab GameObjects included in the entity prefab registry.")]
    public List<GameObject> prefabGOs;

    [SerializeField, HideInInspector]
    /// <summary>
    /// Cached key set derived from <see cref="prefabGOs"/>.
    /// </summary>
    private HashSet<EntityPrefabKey> cachedKeySet;

    /// <summary>
    /// Runtime set of prefab keys derived from the serialized list.
    /// </summary>
    public HashSet<EntityPrefabKey> entityReferenceKeySet => cachedKeySet;

    /// <summary>
    /// Rebuilds cached lookup structures when the asset is loaded.
    /// </summary>
    private void OnEnable()
    {
        Construct();
    }

    /// <summary>
    /// Rebuilds runtime key cache from serialized prefab data.
    /// </summary>
    private void Construct()
    {
        if (cachedKeySet == null)
        {
            cachedKeySet = new HashSet<EntityPrefabKey>();
        }
        else if (prefabGOs.Count > 0)
        {
            foreach (var prefab in prefabGOs)
            {
                cachedKeySet.Add(new EntityPrefabKey
                {
                    name = prefab.name
                });
            }
        }
        prefabGOs = prefabGOs.OrderBy((GameObject go) => go.name).ToHashSet().ToList();
    }

    /// <summary>
    /// Indicates whether cached key state matches the serialized prefab list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            cachedKeySet != null &&
            cachedKeySet.Count == prefabGOs.Count;
    }

    /// <summary>
    /// Ensures key cache is fully constructed and synchronized with serialized data.
    /// </summary>
    /// <returns><see langword="true"/> when cache verification succeeds; otherwise <see langword="false"/>.</returns>
    public bool VerifyConstruction()
    {
        if (IsVerified())
        {
            return true;
        }
        else
        {
            Construct();
            return IsVerified();
        }
    }

}
