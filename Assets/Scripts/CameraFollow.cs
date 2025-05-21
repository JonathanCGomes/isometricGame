using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform playerTransform; // Referencia ao jogador
    [SerializeField]
    private Vector3 offset;           // Diferenca entre a posicao da camera e do jogador

    private void Start()
    {
        offset = transform.position - playerTransform.position;
    }

    private void LateUpdate()
    {
        transform.position = playerTransform.position + offset;
    }
}
