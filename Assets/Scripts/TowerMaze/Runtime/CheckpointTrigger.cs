using UnityEngine;

namespace TowerMaze
{
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private bool triggerOnlyOnce = true;
        [SerializeField] private string playerTag = "Player";

        private bool consumed;

        private void Awake()
        {
            gameManager ??= FindObjectOfType<GameManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed && triggerOnlyOnce)
            {
                return;
            }

            if (!other.CompareTag(playerTag))
            {
                return;
            }

            gameManager?.RegisterCheckpoint(transform.position, transform.rotation);
            consumed = true;
        }
    }
}
