using EVMC4U;
using RTP;
using UnityEngine;
using VRM;

namespace VRLive.Runtime.Player
{
    public class PerformerManager : RemotePlayerManagerBase
    {

        // keep tabs on this so we can make use of better audio timing
        public BackingTrackManager backingTrackManager;

        /// <summary>
        /// The VRM avatar that we plan to use as our base.
        /// </summary>
        private VRMMeta avatar;

        // The osc "server" responsible for sending the bone data to the vrm model
        // public VRTPOscServer oscServer;

        public RTPAudioListenerComponentized audioListener;
        
        
        public override void OnEnable()
        {
            base.OnEnable();

            listener.label = "remote performer manager";

            // var existingAvatar = baseModel.GetComponent<VRMMeta>();

            if (!baseModel.GetComponent<VRMMeta>())
            {
                Debug.LogError("Performer manager's model should be in VRM format.");
                return;
            }


            // we already have an RTP listener active so we can just start using this one
            // oscServer = gameObject.AddComponent<VRTPOscServer>();
            // oscServer.mocapDataIn = listener.MocapDataIn;  // link the two queues together, the server just needs the messages
            audioListener = gameObject.AddComponent<RTPAudioListenerComponentized>();
            audioListener.Listener = listener;
        }
        
        public override void CreateNewPlayer(int userId, UserType usrType)
        {
            if (usrType != UserType.Performer)
            {
                Debug.Log($"User {userId} was not a performer ({usrType})! Ignoring.");
                return;
            }

            if (players.ContainsKey(userId))
            {
                Debug.LogWarning($"User {userId} seems to have reconnected!");
                return;
            }
            else
            {
                Debug.LogWarning($"Adding new performer {userId}");
            }
            
            var newObj = Instantiate(baseModel);
            // newObj.transform.position = Vector3.zero;
            var comp = newObj.GetComponent<PerformerMotionController>() ?? newObj.AddComponent<PerformerMotionController>();
            newObj.SetActive(true);
            comp.parent = this;
            comp.userId = userId;
            if (spawnPoint)
            {
                spawnPoint.MoveTo(newObj);
            }
            
            players.Add(userId, comp);
        }

        public override void RemovePlayer(int userId, UserType usrType)
        {
            throw new System.NotImplementedException();
        }
    }
}