using RTP;
using UnityEngine;

namespace VRLive.Runtime.Player
{
    public class AudienceManager : RemotePlayerController
    {
        // protected void OnNewListenerData(object obj, VRTPPacket pkt)
        // {
        //     // we're the audience -- we don't really care if there's any audio here.
        //
        //     PlayerMotionController player;
        //     if (players.TryGetValue(pkt.UserID, out player))
        //     {
        //         player.OnListenerData(this, pkt);
        //     }
        //     
        // }

        public override void CreateNewPlayer(int userId, UserType usrType)
        {
            if (usrType != UserType.Audience)
            {
                Debug.Log($"User {userId} was not an audience member ({usrType})! Ignoring.");
                return;
            }

            if (players.ContainsKey(userId))
            {
                Debug.LogWarning($"User {userId} seems to have reconnected!");
                return;
            }
            else
            {
                Debug.LogWarning($"Adding new audience member {userId}");
            }
            
            var newObj = Instantiate(baseModel);
            newObj.transform.position = Vector3.zero;
            var comp = newObj.GetComponent<AudienceMotionController>() ?? newObj.AddComponent<AudienceMotionController>();
            comp.parent = this;
            comp.userId = userId;
            players.Add(userId, comp);
        }

        public override void RemovePlayer(int userId, UserType usrType)
        {
            PlayerMotionController controller;
            players.Remove(userId, out controller);
            Destroy(controller.gameObject);
            // throw new System.NotImplementedException();
        }
    }
}