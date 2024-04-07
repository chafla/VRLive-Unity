using UnityEngine;

namespace VRLive.Runtime.Utils
{
    /// <summary>
    /// A simple spawn point.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        public void MoveTo(GameObject obj)
        {
            var spawnPos = gameObject.transform.localPosition;
            obj.transform.position = new Vector3(spawnPos.x, 0.5f, spawnPos.z);
        }
    }
}