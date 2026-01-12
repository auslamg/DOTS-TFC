using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDataRegistrySO", menuName = "AnimationData/AnimationDataListSO")]
public class AnimationDataRegistrySO : ScriptableObject
{
    public List<AnimationDataSO> animationDataSOList;
    public AnimationDataSO GetAnimationDataSO(AnimationKey animationKey)
    {
        foreach (AnimationDataSO so in animationDataSOList)
        {
            if (so.animationKey == animationKey)
            {
                return so;
            }
        }
        Debug.LogError("Could not find AnimationData ScriptableObject for Animation type " + animationKey);
        return null;
    }
}
