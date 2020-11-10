using UnityEngine;

public interface IInput
{
    Vector2 RetrieveMovementInput();
    bool RetrieveNitroInput();
}

public abstract class BaseInput : MonoBehaviour, IInput
{
    public abstract Vector2 RetrieveMovementInput();
    public abstract bool RetrieveNitroInput();
}