using UnityEngine;

namespace KartGame.KartSystems
{
    public class KeyboardInput : BaseInput
    {
        public override Vector2 RetrieveMovementInput()
        {
            return new Vector2
            {
                x = Input.GetAxis("Horizontal"),
                y = Input.GetAxis("Vertical")
            };
        }

        public override bool RetrieveNitroInput()
        {
            return Input.GetKey(KeyCode.T);
        }
    }
}