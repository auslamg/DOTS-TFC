using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining which unit keys a trainer building is allowed to produce.
/// </summary>
[CreateAssetMenu(fileName = "TrainRosterSO", menuName = "Buildings/Trainer/TrainRoster")]
public class TrainRosterSO : ScriptableObject
{
    /// <summary>
    /// Serialized list of unit key names allowed by this roster.
    /// </summary>
    [SerializeField]
    [Tooltip("Unit key names that this trainer roster allows for production.")]
    public List<string> unitKeys;

    [SerializeField, HideInInspector]
    /// <summary>
    /// Cached set of strongly typed unit keys.
    /// </summary>
    private HashSet<UnitKey> cachedKeySet;

    /// <summary>
    /// Runtime set of unit keys derived from <see cref="unitKeys"/>.
    /// </summary>
    public HashSet<UnitKey> unitKeySet => cachedKeySet;

    /// <summary>
    /// Rebuilds cached key structures when the asset is loaded.
    /// </summary>
    private void OnEnable()
    {
        Construct();
    }

    /// <summary>
    /// Rebuilds runtime key cache from serialized roster data.
    /// </summary>
    private void Construct()
    {
        if (cachedKeySet == null)
        {
            cachedKeySet = new HashSet<UnitKey>();
        }
        else if (unitKeys.Count > 0)
        {
            foreach (var unitName in unitKeys)
            {
                cachedKeySet.Add(new UnitKey
                {
                    name = unitName
                });
            }
        }
    }

    /// <summary>
    /// Indicates whether cached key state matches the serialized unit-key list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            cachedKeySet != null &&
            cachedKeySet.Count == unitKeys.Count;
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
