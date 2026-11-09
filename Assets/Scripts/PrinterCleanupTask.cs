using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PrinterCleanupTask : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject[] loosePapers;
    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private GameObject completedStack;

    [Header("Interaction")]
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 1.2f;

    [Header("Paper Audio")]
    [SerializeField] private AudioSource paperAudioSource;
    [SerializeField] private AudioClip paperSound;

    [Range(0f, 1f)]
    [SerializeField] private float paperVolume = 0.5f;

    private bool[] collected;
    private int collectedCount;
    private bool completed;
    private bool initialized;
    private int nearbyPaperIndex = -1;

    private void Start()
    {
        if (player == null ||
            deliveryPoint == null ||
            completedStack == null ||
            loosePapers == null ||
            loosePapers.Length == 0)
        {
            Debug.LogError(
                "PrinterCleanupTask: Assign Player, Loose Papers, " +
                "Delivery Point, and Completed Stack.",
                this
            );

            enabled = false;
            return;
        }

        HashSet<GameObject> uniquePapers = new HashSet<GameObject>();

        foreach (GameObject paper in loosePapers)
        {
            if (paper == null ||
                paper == completedStack ||
                !uniquePapers.Add(paper))
            {
                Debug.LogError(
                    "PrinterCleanupTask: Each Loose Papers entry must " +
                    "contain a different paper object, not the final stack.",
                    this
                );

                enabled = false;
                return;
            }
        }

        collected = new bool[loosePapers.Length];
        completedStack.SetActive(false);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || completed) return;

        nearbyPaperIndex = FindNearestPaper();

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.tKey.wasPressedThisFrame)
        {
            return;
        }

        if (collectedCount < loosePapers.Length)
        {
            if (nearbyPaperIndex >= 0)
            {
                CollectPaper(nearbyPaperIndex);
            }

            return;
        }

        if (HorizontalDistance(deliveryPoint.position) <=
            interactionDistance)
        {
            completed = true;
            completedStack.SetActive(true);
            PlayPaperSound();
        }
    }

    private int FindNearestPaper()
    {
        int nearestIndex = -1;
        float nearestDistance = interactionDistance;

        for (int i = 0; i < loosePapers.Length; i++)
        {
            if (collected[i] ||
                loosePapers[i] == null ||
                !loosePapers[i].activeInHierarchy)
            {
                continue;
            }

            float distance =
                HorizontalDistance(loosePapers[i].transform.position);

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private float HorizontalDistance(Vector3 position)
    {
        Vector3 difference = player.position - position;
        difference.y = 0f;

        return difference.magnitude;
    }

    private void CollectPaper(int index)
    {
        if (collected[index]) return;

        collected[index] = true;
        collectedCount++;

        loosePapers[index].SetActive(false);
        PlayPaperSound();

        nearbyPaperIndex = -1;
    }

    private void PlayPaperSound()
    {
        if (paperAudioSource != null && paperSound != null)
        {
            paperAudioSource.PlayOneShot(paperSound, paperVolume);
        }
    }

    private void OnGUI()
    {
        if (!initialized) return;

        Matrix4x4 previousMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(
                Screen.width / 1280f,
                Screen.height / 720f,
                1f
            )
        );

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            wordWrap = true
        };

        string message;

        if (completed)
        {
            message = "PRINTER CLEANUP\n" +
                      "Complete! All papers neatly stacked.";
        }
        else if (collectedCount == loosePapers.Length)
        {
            message = "PRINTER CLEANUP\n" +
                      "All papers collected. Take them to the printer desk.";

            if (HorizontalDistance(deliveryPoint.position) <=
                interactionDistance)
            {
                message += "\nT: Place the paper stack";
            }
        }
        else
        {
            message = "PRINTER CLEANUP\n" +
                      $"Papers collected: {collectedCount}/{loosePapers.Length}";

            if (nearbyPaperIndex >= 0)
            {
                message += "\nT: Collect paper";
            }
        }

        GUILayout.BeginArea(
            new Rect(20, 160, 440, 140),
            GUI.skin.box
        );

        GUILayout.Label(message, textStyle);
        GUILayout.EndArea();

        GUI.matrix = previousMatrix;
    }
}