using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "ResourceDataSO", menuName = "Resources/Resource")]
public class ResourceSO : ScriptableObject
{
    /// <summary>
    /// Category of this resource.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this resource.")]
    public ResourceType resourceType;

    /// <summary>
    /// Card sprite used by UI lists and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this resource in UI cards/buttons.")]
    public Sprite icon;

    [SerializeField, HideInInspector]
    private ResourceKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public ResourceKey resourceKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new ResourceKey
        {
            name = this.name
        };
    }
}
