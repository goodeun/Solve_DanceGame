using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdPooling : MonoBehaviour
{
    [SerializeField] private GameObject Pool_Prefab;
    [SerializeField] private int Pool_Count = 100;
    [SerializeField] private AtmosphereController atmosphereController;
    [SerializeField] private BoxCollider seatingCollider;
    [SerializeField, Min(0f)] private float positionJitter = 0.35f;
    [SerializeField] private Transform mainFloor;

    private GameObject[] Objects;
    private int Pivot = 0;

    private void Awake()
    {
        atmosphereController ??= GetComponent<AtmosphereController>();
        seatingCollider ??= FindSeatingCollider();
        if (mainFloor == null)
            mainFloor = FindFirstObjectByType<MainFloorController>()?.transform;
        if (Pool_Prefab == null)
        {
            Debug.LogWarning("CrowdPooling needs Audience assigned as Pool Prefab.", this);
            return;
        }

        Objects = new GameObject[Pool_Count];

        for (int i = 0; i < Objects.Length; i++)
        {
            Objects[i] = Instantiate(
                Pool_Prefab,
                transform
            );

            Objects[i].name = $"{Pool_Prefab.name}_{i}";
            Objects[i].transform.position = GetPositionOnSeatingCollider(i);
            FaceMainFloor(Objects[i].transform);

            foreach (Rigidbody body in Objects[i].GetComponentsInChildren<Rigidbody>())
                body.isKinematic = true;

            Objects[i].SetActive(true);
        }

        StartCoroutine(ApplyAnimationOffsets());
    }

    private IEnumerator ApplyAnimationOffsets()
    {
        yield return null;

        for (int i = 0; i < Objects.Length; i++)
        {
            float offset = Mathf.PerlinNoise(i * 0.53f, 0.41f);
            foreach (Animator animator in Objects[i].GetComponentsInChildren<Animator>())
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != 0)
                    animator.Play(state.fullPathHash, 0, offset);
            }
        }
    }

    private void LateUpdate()
    {
        if (mainFloor == null || Objects == null)
            return;

        foreach (GameObject audience in Objects)
        {
            if (audience != null && audience.activeSelf)
                FaceMainFloor(audience.transform);
        }
    }

    private BoxCollider FindSeatingCollider()
    {
        Transform seat = transform.parent;
        if (seat == null)
            return null;

        BoxCollider[] colliders = seat.GetComponentsInChildren<BoxCollider>(true);
        return colliders.Length > 0 ? colliders[0] : null;
    }

    private Vector3 GetPositionOnSeatingCollider(int index)
    {
        if (seatingCollider == null)
            return transform.position;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(Pool_Count));
        int rows = Mathf.CeilToInt((float)Pool_Count / columns);
        int column = index % columns;
        int row = index / columns;

        float x = ((column + 0.5f) / columns - 0.5f) * seatingCollider.size.x;
        float z = ((row + 0.5f) / rows - 0.5f) * seatingCollider.size.z;
        x += (Mathf.PerlinNoise(index * 0.71f, 0.17f) - 0.5f) * positionJitter * 2f;
        z += (Mathf.PerlinNoise(index * 0.31f, 0.83f) - 0.5f) * positionJitter * 2f;
        Vector3 localPosition = seatingCollider.center + new Vector3(x, seatingCollider.size.y * 0.5f, z);

        return seatingCollider.transform.TransformPoint(localPosition);
    }

    private void FaceMainFloor(Transform audience)
    {
        Vector3 direction = mainFloor.position - audience.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            audience.rotation = Quaternion.LookRotation(direction);
    }

    public GameObject GetObject()
    {
        if (Objects == null) return null;
        for (int i = 0; i < Objects.Length; i++)
        {
            if (!Objects[Pivot].activeSelf)
            {
                GameObject Monster = Objects[Pivot];

                Pivot++;

                if (Pivot >= Objects.Length)
                    Pivot = 0;

                Monster.GetComponent<AudienceController>()?.SetAtmosphereController(atmosphereController);

                return Monster;
            }

            Pivot++;

            if (Pivot >= Objects.Length)
                Pivot = 0;
        }

        return null;
    }
}
