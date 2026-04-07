using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry ScriptableObject containing all <see cref="AnimationDataSO"/> assets used by animation lookup systems.
/// </summary>
/// <remarks>
/// Maintains a runtime dictionary keyed by <see cref="AnimationKey"/> for fast managed lookups
/// and keeps the serialized list sorted for deterministic baking and binary-search workflows.
/// </remarks>
[CreateAssetMenu(fileName = "AnimationDataRegistrySO", menuName = "AnimationData/AnimationDataRegistrySO")]
public class AnimationDataRegistrySO : ScriptableObject
{
    /// <summary>
    /// Serialized animation data entries that populate this registry.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation data entries included in this registry.")]
    public List<AnimationDataSO> animationDataSOList;

    /// <summary>
    /// Runtime dictionary for fast key-based lookups.
    /// </summary>
    private Dictionary<AnimationKey, AnimationDataSO> animationDataDictionary;

    /// <summary>
    /// Rebuilds cached lookup structures when the asset is loaded.
    /// </summary>
    private void OnEnable()
    {
        Construct();
    }

    /// <summary>
    /// Rebuilds runtime lookup structures from serialized list data.
    /// </summary>
    private void Construct()
    {
        animationDataDictionary = new Dictionary<AnimationKey, AnimationDataSO>();

        foreach (AnimationDataSO so in animationDataSOList)
        {
            if (animationDataDictionary.ContainsKey(so.animationKey))
            {
                if (so.animationKey.name != "")
                {
                    Debug.LogWarning($"Duplicate AnimationKey found: {so.animationKey}", this);
                }
                continue;
            }

            animationDataDictionary.Add(so.animationKey, so);
        }

        animationDataSOList = animationDataSOList.OrderBy((AnimationDataSO so) => so.name).ToHashSet().ToList();
    }

    /// <summary>
    /// Indicates whether cached dictionary state matches the serialized list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            animationDataDictionary != null &&
            animationDataDictionary.Count == animationDataSOList.Count;
    }

    /// <summary>
    /// Ensures lookup cache is fully constructed and synchronized with serialized data.
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

    /// <summary>
    /// Retrieves an animation data asset by key.
    /// </summary>
    /// <param name="animationKey">Animation key to retrieve.</param>
    /// <returns>Matching animation data asset, or <see langword="null"/> when not found.</returns>
    public AnimationDataSO GetAnimationDataSO(AnimationKey animationKey)
    {
        if (!IsVerified())
        {
            Construct();
        }

        if (animationDataDictionary.TryGetValue(animationKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find animation data asset for key {animationKey}", this);
        return null;
    }
}
