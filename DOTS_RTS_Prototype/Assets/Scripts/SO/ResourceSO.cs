using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a single resource definition used by the game.
/// Contains metadata such as type, UI representation, and a deterministic lookup key.
/// </summary>
[CreateAssetMenu(fileName = "ResourceDataSO", menuName = "Scriptable Objects/Resources/Resource")]
public class ResourceSO : ScriptableObject
{
    /// <summary>
    /// Defines the category or type of this resource.
    /// Used for grouping, filtering, and gameplay logic classification.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this resource.")]
    public ResourceType resourceType;

    /// <summary>
    /// Icon representation used in UI elements such as cards, lists, and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this resource in UI cards/buttons.")]
    public Sprite icon;

    /// <summary>
    /// Cached deterministic key used for fast lookup and registry indexing.
    /// Serialized as hidden to preserve editor visibility while maintaining persistence.
    /// </summary>
    [SerializeField, HideInInspector]
    private ResourceKey cachedKey;

    /// <summary>
    /// Deterministic identifier derived from the asset name.
    /// Used as the primary lookup key in <see cref="ResourceRegistrySO"/>.
    /// </summary>
    public ResourceKey resourceKey => cachedKey;

    /// <summary>
    /// Unity editor callback invoked when the asset is modified.
    /// Ensures the cached lookup key remains synchronized with the asset name.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new ResourceKey
        {
            name = this.name
        };
    }
}