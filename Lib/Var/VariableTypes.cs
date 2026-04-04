using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class AnimatorVar : MMVar<global::UnityEngine.Animator>
    {
        public AnimatorVar() : base("Animator") {}
    }

    [System.Serializable]
    public class AnimatorListVar : MMListVar<global::UnityEngine.Animator>
    {
        public AnimatorListVar() : base("Animator") {}
    }

    [System.Serializable]
    public class CameraVar : MMVar<global::UnityEngine.Camera>
    {
        public CameraVar() : base("Camera") {}
    }

    [System.Serializable]
    public class CameraListVar : MMListVar<global::UnityEngine.Camera>
    {
        public CameraListVar() : base("Camera") {}
    }

    [System.Serializable]
    public class GameObjectVar : MMVar<global::UnityEngine.GameObject>
    {
        public GameObjectVar() : base("GameObject") {}
    }

    [System.Serializable]
    public class GameObjectListVar : MMListVar<global::UnityEngine.GameObject>
    {
        public GameObjectListVar() : base("GameObject") {}
    }

    [System.Serializable]
    public class JoystickUIVar : MMVar<global::SP.JoystickUI>
    {
        public JoystickUIVar() : base("JoystickUI") {}
    }

    [System.Serializable]
    public class JoystickUIListVar : MMListVar<global::SP.JoystickUI>
    {
        public JoystickUIListVar() : base("JoystickUI") {}
    }

    [System.Serializable]
    public class ProgressBarVar : MMVar<global::SP.ProgressBar>
    {
        public ProgressBarVar() : base("ProgressBar") {}
    }

    [System.Serializable]
    public class ProgressBarListVar : MMListVar<global::SP.ProgressBar>
    {
        public ProgressBarListVar() : base("ProgressBar") {}
    }

    [System.Serializable]
    public class StockVar : MMVar<global::SP.Stock>
    {
        public StockVar() : base("Stock") {}
    }

    [System.Serializable]
    public class StockListVar : MMListVar<global::SP.Stock>
    {
        public StockListVar() : base("Stock") {}
    }

    [System.Serializable]
    public class TransformVar : MMVar<global::UnityEngine.Transform>
    {
        public TransformVar() : base("Transform") {}
    }

    [System.Serializable]
    public class TransformListVar : MMListVar<global::UnityEngine.Transform>
    {
        public TransformListVar() : base("Transform") {}
    }

    [System.Serializable]
    public class TriggerVar : MMVar<global::SP.Trigger>
    {
        public TriggerVar() : base("Trigger") {}
    }

    [System.Serializable]
    public class TriggerListVar : MMListVar<global::SP.Trigger>
    {
        public TriggerListVar() : base("Trigger") {}
    }
}
