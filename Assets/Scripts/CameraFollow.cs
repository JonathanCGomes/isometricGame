using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform; // Referencia ao jogador
    public float smoothSpeed = 5f;

    [SerializeField]
    private Vector3 offset; // Diferenca entre a posicao da camera e do jogador

    private void Start()
    {
        offset = transform.position - playerTransform.position;
    }

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, playerTransform.position + offset, smoothSpeed * Time.deltaTime);
    }
}
