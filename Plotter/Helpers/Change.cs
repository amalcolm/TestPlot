using System;
using System.Reflection;

namespace Plotter
{
    public static class Change
    {
        static public T To<T>(MyFrame baseFrame) where T : MyFrame, new()
        {
            var cFrame = new T();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

            var currentType = typeof(MyFrame);
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

        static public MyFrame? SetType(this MyFrame baseFrame) => From(baseFrame);

        static public MyFrame? From(MyFrame baseFrame)
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
