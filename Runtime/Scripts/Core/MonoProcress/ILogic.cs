namespace OSK
{
    public interface IAwake
    {
        void OnAwake();
    }

    public interface IOnEnable
    {
        void OnEnable();
    }

    public interface IOnDisable
    {
        void OnDisable();
    }

    public interface IStart
    {
        void OnStart();
    }

    public interface IUpdate
    {
        void Tick(float deltaTime);
    }

    public interface IFixedUpdate
    {
        void FixedTick(float fixedDeltaTime);
    }

    public interface ILateUpdate
    {
        void LateTick(float deltaTime);
    }

    public interface IDestroy
    {
        void OnDestroy();
    }
}