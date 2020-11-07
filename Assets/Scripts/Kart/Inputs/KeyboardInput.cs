using UnityEngine;

namespace KartGame.KartSystems
{
    public class KeyboardInput : BaseInput
    {
        public override Vector2 GenerateInput()
        {
            return new Vector2
            {
                x = Input.GetAxis("Horizontal"),
                y = Input.GetAxis("Vertical")
            };
        }
    }
}