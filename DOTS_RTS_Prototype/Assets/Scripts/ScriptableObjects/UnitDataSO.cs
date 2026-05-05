using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObject describing gameplay and UI data for one unit type.
/// </summary>
[CreateAssetMenu(fileName = "UnitDataSO", menuName = "Units/UnitDataSO")]
public class UnitDataSO : ScriptableObject
{
    /// <summary>
    /// Category of this unit.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this unit.")]
    public UnitType unitType;

    /// <summary>
    /// Time required to train this unit.
    /// </summary>
    [SerializeField]
    [Tooltip("Training time required before this unit is produced.")]
    public float trainingTime;

    /// <summary>
    /// Card sprite used by UI lists and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this unit in UI cards/buttons.")]
    public Sprite imageCard;

    /// <summary>
    /// Resource construction cost for unit training.
    /// </summary>
    [SerializeField]
    [Tooltip("Resource construction cost for unit training.")]
    public ResourceQuantity[] constructionCost;

    [SerializeField, HideInInspector]
    private UnitKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public UnitKey unitKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new UnitKey
        {
            name = this.name
        };
    }
}


