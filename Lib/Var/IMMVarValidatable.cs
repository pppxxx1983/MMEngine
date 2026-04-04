using System;

namespace SP
{
    public interface IMMVarValidatable
    {
        bool TryValidate(Type expectedValueType, bool expectsList, out string error);
        bool ValidateAndLog(Type expectedValueType, bool expectsList, UnityEngine.Object context = null);
    }
}
