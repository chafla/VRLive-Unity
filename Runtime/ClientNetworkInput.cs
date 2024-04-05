namespace VRLive.Runtime
{
    public interface ClientNetworkInput
    {
        public void UpdatePorts(ClientPortMap clientPorts, ServerPortMap serverPorts);
    }
}