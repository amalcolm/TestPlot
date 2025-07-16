using System;
using System.Reflection;

namespace CTG_Comms
{
    public static class Change
    {
        static public T To<T>(CTGframe baseFrame) where T : CTGframe, new()
        {
            var cFrame = new T();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

            var currentType = typeof(CTGframe);
            while (currentType != null)
            {
                foreach (var prop in currentType.GetProperties(flags))
                    if (prop.CanWrite)
                        prop.SetValue(cFrame, prop.GetValue(baseFrame));

                foreach (var field in currentType.GetFields(flags))
                    field.SetValue(cFrame, field.GetValue(baseFrame));  // fields after properties as setting properties can set fields.

                currentType = currentType.BaseType;
            }

            return cFrame;
        }

        static public CTGframe? SetType(this CTGframe baseFrame) => From(baseFrame);

        static public CTGframe? From(CTGframe baseFrame)
        {
            if (baseFrame?.IsMalformed != false || baseFrame.ReadyToSet == false) return baseFrame;

            return baseFrame.Kind switch
            {
                'C' => Change.To< C_Frame>(baseFrame),
                'T' => Change.To< Test_Frame>(baseFrame),
                _ => throw new Exception(TestIO.ERROR_FRAME_NOT_RECOGNISED)
            };
            

        }
    }
}
