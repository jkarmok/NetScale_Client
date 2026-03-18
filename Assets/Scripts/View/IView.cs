using Game.Scene;
using UnityEngine;

namespace View
{
    public interface IView
    {
        public void SpawnSetup(byte viewId,  int spatialOwnerId, int proxyId,
            TransformState transformState, Vector3 position, Quaternion rotation, bool owned);
    }
}