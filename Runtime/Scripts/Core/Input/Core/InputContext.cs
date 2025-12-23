 
namespace OSK
{
    public enum InputContext
    {
        Gameplay,
        UI,
        Cutscene
    }
    public static class InputContextManager
    {
        public static InputContext Current { get; private set; } = InputContext.Gameplay;

        public static void Set(InputContext context)
        {
            Current = context;
        }

        public static bool Allow(string actionId)
        {
            switch (Current)
            {
                case InputContext.Cutscene:
                    return false;

                case InputContext.UI:
                    return actionId.StartsWith("UI_");

                case InputContext.Gameplay:
                default:
                    return true;
            }
        }
    }
} 