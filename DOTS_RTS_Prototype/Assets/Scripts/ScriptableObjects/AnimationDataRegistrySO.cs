using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDataRegistrySO", menuName = "AnimationData/AnimationDataRegistrySO")]
public class AnimationDataRegistrySO : ScriptableObject
{
    [SerializeField] public List<AnimationDataSO> animationDataSOList;
    private Dictionary<AnimationKey, AnimationDataSO> animationDataDictionary;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
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
    }

    public AnimationDataSO GetAnimationDataSO(AnimationKey animationKey)
    {
        if (animationDataDictionary.TryGetValue(animationKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find AnimationData ScriptableObject for Animation type {animationKey}", this);
        return null;
    }
}
