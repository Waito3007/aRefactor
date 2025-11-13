using System.ComponentModel;

namespace aRefactor.Extension;

public static class Extension
{
    public static string GetDescriptionOfEnum(this Enum enumModel)
    {
        var enumType = enumModel.GetType();
        var memberInfos = enumType.GetMember(enumModel.ToString());
        var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
        var valueAttributes =
            enumValueMemberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        var description = ((DescriptionAttribute)valueAttributes[0]).Description;
        return description;
    }
}