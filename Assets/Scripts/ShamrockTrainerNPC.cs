using System.Collections;
using TMPro;
using UnityEngine;

public class ShamrockTrainerNPC : MonoBehaviour
{
    private enum TrainerState
    {
        Practicing,
        Teaching,
        HitReaction
    }

    [Header("NPC")]
    [SerializeField] private Animator animator;
    [SerializeField] private float turnSpeed = 5f;

    [Header("Practice Animation")]
    [SerializeField] private float minimumAttackDelay = 2.5f;
    [SerializeField] private float maximumAttackDelay = 4.5f;

    [Header("Practice Target")]
    [SerializeField] private Transform practiceTarget;
    [SerializeField] private float requiredFacingAngle = 10f;
    [SerializeField] private float practiceAttackDuration = 1f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;

    [Header("Text Reveal")]
    [SerializeField] private bool revealWordByWord = false;
    [SerializeField] private float letterDelay = 0.03f;
    [SerializeField] private float wordDelay = 0.15f;

    private Coroutine typingRoutine;


    [TextArea]
    [SerializeField]
    private string instructionMessage =
        "Use Left Click or Right Trigger to shoot a shamrock at me!";

    [TextArea]
    [SerializeField]
    private string successMessage =
        "Great shot! Shamrocks can slow down your enemies.";

    [SerializeField] private float successMessageDuration = 3f;

    private Transform player;
    private TrainerState currentState;
    private float nextPracticeAttackTime;
    private Coroutine hitRoutine;
    private bool practiceAttackInProgress;
    private Coroutine practiceAttackRoutine;


    private static readonly int AttackTrigger =
        Animator.StringToHash("attack");

    private static readonly int DamagedTrigger =
        Animator.StringToHash("damaged");

    private static readonly int IdleState =
        Animator.StringToHash("Base Layer.idle");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        currentState = TrainerState.Practicing;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        ScheduleNextPracticeAttack();
    }

    private void Update()
    {
        switch (currentState)
        {
            case TrainerState.Practicing:
                UpdatePractice();
                break;

            case TrainerState.Teaching:
                FacePlayer();
                break;

            case TrainerState.HitReaction:
                FacePlayer();
                break;
        }
    }

    private void UpdatePractice()
    {
        // Never practice while the player is inside the tutorial zone.
        if (currentState != TrainerState.Practicing || player != null)
        {
            return;
        }

        if (practiceTarget == null || animator == null)
        {
            return;
        }

        FaceTarget(practiceTarget.position);

        if (practiceAttackInProgress)
        {
            return;
        }

        if (Time.time >= nextPracticeAttackTime)
        {
            practiceAttackRoutine =
                StartCoroutine(PracticeAttackRoutine());
        }
    }

    private IEnumerator PracticeAttackRoutine()
    {
        practiceAttackInProgress = true;

        // Check again before attacking.
        if (currentState != TrainerState.Practicing || player != null)
        {
            practiceAttackInProgress = false;
            practiceAttackRoutine = null;
            yield break;
        }

        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(AttackTrigger);

        yield return new WaitForSeconds(practiceAttackDuration);

        // Only schedule another attack if the player is still outside.
        if (currentState == TrainerState.Practicing && player == null)
        {
            ScheduleNextPracticeAttack();
        }

        practiceAttackInProgress = false;
        practiceAttackRoutine = null;
    }

    private void StopPracticing()
    {
        if (practiceAttackRoutine != null)
        {
            StopCoroutine(practiceAttackRoutine);
            practiceAttackRoutine = null;
        }

        practiceAttackInProgress = false;
        nextPracticeAttackTime = float.PositiveInfinity;

        if (animator != null)
        {
            animator.ResetTrigger(AttackTrigger);

            animator.CrossFadeInFixedTime(
                IdleState,
                0.05f,
                0
            );
        }
    }

    private void ScheduleNextPracticeAttack()
    {
        nextPracticeAttackTime = Time.time + Random.Range(
            minimumAttackDelay,
            maximumAttackDelay
        );
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        player = other.transform.root;

        // Change the state before stopping the attack.
        currentState = TrainerState.Teaching;

        StopPracticing();

        ShowMessage(instructionMessage);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        player = null;
        currentState = TrainerState.Practicing;

        HideMessage();

        ScheduleNextPracticeAttack();
    }

    public void ReceiveShamrockHit()
    {
        Debug.Log("Trainer received shamrock hit.");

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(ShowSuccessRoutine());
    }

    private IEnumerator ShowSuccessRoutine()
    {
        currentState = TrainerState.HitReaction;

        StopPracticing();

        if (animator != null)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.SetTrigger(DamagedTrigger);
        }

        ShowMessage(successMessage);

        yield return new WaitForSeconds(successMessageDuration);

        if (player != null)
        {
            currentState = TrainerState.Teaching;
            ShowMessage(instructionMessage);
        }
        else
        {
            currentState = TrainerState.Practicing;
            HideMessage();
            ScheduleNextPracticeAttack();
        }

        hitRoutine = null;
    }

    private IEnumerator HitReactionRoutine()
    {
        currentState = TrainerState.HitReaction;

        animator.ResetTrigger(AttackTrigger);
        animator.SetTrigger(DamagedTrigger);

        ShowMessage(successMessage);

        yield return new WaitForSeconds(successMessageDuration);

        if (player != null)
        {
            currentState = TrainerState.Teaching;
            ShowMessage(instructionMessage);
        }
        else
        {
            currentState = TrainerState.Practicing;
            HideMessage();
            ScheduleNextPracticeAttack();
        }

        hitRoutine = null;
    }

    private void ShowMessage(string message)
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialText == null)
        {
            return;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(RevealTextRoutine(message));
    }

    private IEnumerator RevealTextRoutine(string message)
    {
        tutorialText.text = message;

        // Generate TextMeshPro's character and word information.
        tutorialText.ForceMeshUpdate();

        if (revealWordByWord)
        {
            tutorialText.maxVisibleCharacters = int.MaxValue;
            tutorialText.maxVisibleWords = 0;

            int totalWords = tutorialText.textInfo.wordCount;

            for (int i = 1; i <= totalWords; i++)
            {
                tutorialText.maxVisibleWords = i;
                yield return new WaitForSeconds(wordDelay);
            }
        }
        else
        {
            tutorialText.maxVisibleWords = int.MaxValue;
            tutorialText.maxVisibleCharacters = 0;

            int totalCharacters =
                tutorialText.textInfo.characterCount;

            for (int i = 1; i <= totalCharacters; i++)
            {
                tutorialText.maxVisibleCharacters = i;

                char currentCharacter =
                    tutorialText.textInfo.characterInfo[i - 1].character;

                // Do not pause for spaces.
                if (!char.IsWhiteSpace(currentCharacter))
                {
                    yield return new WaitForSeconds(letterDelay);
                }
            }
        }

        typingRoutine = null;
    }

    private void HideMessage()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (tutorialText != null)
        {
            tutorialText.maxVisibleCharacters = int.MaxValue;
            tutorialText.maxVisibleWords = int.MaxValue;
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}