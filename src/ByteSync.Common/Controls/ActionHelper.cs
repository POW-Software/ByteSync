// using ByteSyncCommon.Business.Actions;
// using PowSoftware.Common.Helpers;
//
// namespace ByteSyncCommon.Controls;
//
// public static class ActionHelper
// {
//     public static bool IsCopyContent(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.CopyContentOnly, ActionOperatorTypes.CopyContentAndDate);
//     }
//     
//     public static bool IsCopyContentOnly(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.CopyContentOnly);
//     }
//     
//     public static bool IsCopyContentAndDate(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.CopyContentAndDate);
//     }
//     
//     public static bool IsCopyDate(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.CopyDate);
//     }
//     
//     public static bool IsDelete(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.Delete);
//     }
//     
//     public static bool IsCreate(this AbstractAction action)
//     {
//         return action.Operator.In(ActionOperatorTypes.Create);
//     }
// }