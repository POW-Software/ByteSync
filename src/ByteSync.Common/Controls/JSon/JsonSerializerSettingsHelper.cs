// using Newtonsoft.Json;
//
// namespace ByteSync.Common.Controls.JSon;
//
// public class JsonSerializerSettingsHelper
// {
//     public static JsonSerializerSettings BuildSettings(bool writablePropertiesOnly, bool useUtcDateTimes, 
//         bool addTypeNames)
//     {
//         JsonSerializerSettings settings = new JsonSerializerSettings();
//
//         if (writablePropertiesOnly)
//         {
//             settings.ContractResolver = new WritablePropertiesOnlyResolver();
//         }
//         
//         if (useUtcDateTimes)
//         {
//             settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
//         }
//
//         if (addTypeNames)
//         {
//             settings.TypeNameHandling = TypeNameHandling.Objects;
//         }
//         
//         return settings;
//     }
// }