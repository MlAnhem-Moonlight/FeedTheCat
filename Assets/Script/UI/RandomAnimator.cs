using UnityEngine;
using System.Collections;

public class RandomAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Tên các State trong Animator")]
    [SerializeField] private string[] animationStates;

    private void OnEnable()
    {
        PlayRandomAnimation();
    }

    /// <summary>
    /// Phát ngẫu nhiên một animation.
    /// </summary>
    public void PlayRandomAnimation()
    {
        if (animator == null || animationStates == null || animationStates.Length == 0)
            return;

        int index = Random.Range(0, animationStates.Length);
        animator.Play(animationStates[index], 0, 0f);
    }

    /// <summary>
    /// Phát một animation ngẫu nhiên nhưng không trùng animation hiện tại.
    /// </summary>
    public void PlayRandomAnimationNoRepeat()
    {
        if (animator == null || animationStates == null || animationStates.Length <= 1)
        {
            PlayRandomAnimation();
            return;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        int currentIndex = -1;
        for (int i = 0; i < animationStates.Length; i++)
        {
            if (currentState.IsName(animationStates[i]))
            {
                currentIndex = i;
                break;
            }
        }

        int newIndex;
        do
        {
            newIndex = Random.Range(0, animationStates.Length);
        }
        while (newIndex == currentIndex);

        animator.Play(animationStates[newIndex], 0, 0f);
    }

    public void PlayRandomLoop()
    {
        StopAllCoroutines();
        StartCoroutine(RandomLoop());
    }

    private IEnumerator RandomLoop()
    {
        while (true)
        {
            PlayRandomAnimationNoRepeat();

            yield return null; // Chờ Animator cập nhật state

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(state.length);
        }
    }
}