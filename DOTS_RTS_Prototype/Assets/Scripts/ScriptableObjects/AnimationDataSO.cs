using UnityEngine;

[CreateAssetMenu(fileName = "AnimationDataSO", menuName = "AnimationData/AnimationDataSO")]
public class AnimationDataSO : ScriptableObject
{
    public enum AnimationType
    {
        None,
        SoldierIdle,
        SoldierWalk,
        ZombieIdle,
        ZombieWalk,
        SoldierAim,
        SoldierShoot,
        ZombieAttack

    }
    public AnimationType animationType;
    public Mesh[] meshArray;
    public float frameFrequency;
}
