using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDataRegistrySO", menuName = "AnimationData/AnimationDataRegistrySO")]
public class AnimationDataRegistrySO : ScriptableObject
{
    [SerializeField] public List<AnimationDataSO> animationDataSOList;
    private Dictionary<AnimationKey, AnimationDataSO> animationDataDictionary;
    
    private void OnEnable()
    {
        Construct();
    }

    private void Construct()
    {
        animationDataDictionary = new Dictionary<AnimationKey, AnimationDataSO>();

        foreach (AnimationDataSO so in animationDataSOList)
        {
            if (animationDataDictionary.ContainsKey(so.animationKey))
            {
                Debug.LogWarning($"Duplicate AnimationKey found: {so.animationKey}", this);
                continue;
            }

            animationDataDictionary.Add(so.animationKey, so);
        }

        animationDataSOList = animationDataSOList.OrderBy((AnimationDataSO so) => so.name).ToHashSet().ToList();
    }

    /// <summary>
    /// Used to indicate if the internal Dictionary has already been verified to
    /// contain the elements of the serialized list.
    /// 
    /// This is because methods OnEnable() and OnValidate() build the dictionary BEFORE 
    /// the list is serialized, so it is verified in the first access to the Dictionary.
    /// </summary>
    private bool IsVerified()
    {
        return
            animationDataDictionary != null &&
            animationDataDictionary.Count == animationDataSOList.Count;
    }

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

        Debug.LogError($"Could not find AnimationData ScriptableObject for Animation type {animationKey}", this);
        return null;
    }
}
