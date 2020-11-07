using UnityEngine;

namespace KartGame.KartSystems
{
    public abstract class BaseInput : MonoBehaviour
    {
        public abstract Vector2 RetrieveMovementInput();
        public abstract bool RetrieveNitroInput();
    }
}
