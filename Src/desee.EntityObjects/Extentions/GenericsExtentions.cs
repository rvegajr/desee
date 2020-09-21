using FastMember;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("desee.Tests")]

namespace desee.EntityObjects.Extentions
{
    public static class GenericExtensions
    {
        public static String AsSHA256Hash(this string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (var hash = System.Security.Cryptography.SHA256.Create())            
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
        }

        /// <summary>
        /// Will generate a SHA256 hash on any C# object based on what is passed through fieldList. This is useful as you can image any object in a concise matter and then use that key to compare it from a cached image later
        /// </summary>
        /// <param name="item">Item to generate hash on</param>
        /// <param name="fieldList">A string array that lets you specify wild cards or '-' to remove blocks of fields.  Lets assume a class that contains properties KeyFieldA, KeyFieldB, DataFieldA, DataFieldB, DataFieldC
        ///   For Example:  ["-Key*"] - This says include all fields but ignore all fields that begin with Key - The Hash will be generated from - DataFieldA, DataFieldB, DataFieldC</param>
        ///                 ["Key*", "DataFieldA"] - Include all fields that begin with Key and the field DataFieldA - The Hash will be generated from - KeyFieldA, KeyFieldB, DataFieldA</param>
        ///                 ["*Field*", "-Data*"] - Include all fields that have the text 'Field' in the middle but ignoring all fields that begin with Data - The Hash will be generated from - KeyFieldA, KeyFieldB</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>String Hash made off the fields you specify</returns>
        public static HashImage AsHashImage<T>(this T item, string[] fieldList)  where T : new() {
            var hashImage = new HashImage();
            var targetAccessor = TypeAccessor.Create(typeof(T));
            T targetItem = new T();
            var _targetIndexMembers = item.Members(fieldList);

            var valueDict = new Dictionary<string, object>();
            foreach (var targetFieldName in _targetIndexMembers.Keys)
            {
                try {
                    object targetValue = targetAccessor[item, targetFieldName];
                    if ((targetValue == null) || (targetValue.Equals("DBNull"))) targetValue = null;
                    valueDict.Add(targetFieldName, targetValue);
                }
                catch (Exception exField)
                {
                    var message = string.Format("GenericExtensions.AsHashImage: Unable to set {0}.  {1}", targetFieldName, exField.Message);
                    throw new Exception(message, exField);
                }
            }
            hashImage.Json = JsonConvert.SerializeObject(valueDict);
            return hashImage;
        }

        public static HashKeyImage AsHashKeyImage<T>(this T item, string[] keyFieldList, string[] fieldList)  where T : new() {
            var hashKeyImage = new HashKeyImage();
            hashKeyImage.Key = item.AsHashImage(keyFieldList);
            hashKeyImage.Image = item.AsHashImage(fieldList);
            return hashKeyImage;
        }

        public static HashKeyImage AsHashKeyImage<T>(this T item, string keyFieldListCSV, string fieldListCSV)  where T : new() {
            var hashKeyImage = new HashKeyImage();
            var keyFieldList = keyFieldListCSV.Split(',');
            var fieldList = fieldListCSV.Split(',');
            hashKeyImage.Key = item.AsHashImage(keyFieldList);
            hashKeyImage.Image = item.AsHashImage(fieldList);
            return hashKeyImage;
        }

        internal static Dictionary<string, TypeMemberIndex> IndexMembers = new Dictionary<string, TypeMemberIndex>();
        internal static bool In<T>(this T item, params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            return items.Contains(item);
        }

        internal static DifferenceItems Difference<T>(this T itemSource, T itemToCompare, string[] fieldList) where T : new() {
            var differences = new DifferenceItems();
            var itemSourceAccessor = TypeAccessor.Create(typeof(T));
            var itemToCompareAccessor = TypeAccessor.Create(typeof(T));

            var itemSourceIndexMembers = itemSource.Members(fieldList);
            var itemToCompareIndexMembers = itemToCompare.Members(fieldList);

            var valueDict = new Dictionary<string, object>();
            foreach (var targetFieldName in itemSourceIndexMembers.Keys)
            {
                try {
                    object itemSourceValue = itemSourceAccessor[itemSource, targetFieldName];
                    object itemToCompareValue = itemToCompareAccessor[itemToCompare, targetFieldName];
                    if (itemSourceValue != itemToCompareValue) {
                        differences.Add(targetFieldName, new DifferenceItemValue() { OldValue=itemSourceValue, NewValue=itemToCompareValue } );
                    }
                }
                catch (Exception exField)
                {
                    var message = string.Format("GenericExtensions.AsHashImage: Unable to set {0}.  {1}", targetFieldName, exField.Message);
                    throw new Exception(message, exField);
                }
            }

            return differences;
        }

        internal static T As<S, T>(this S item, TypeMemberIndex sourceIndexMembers, TypeMemberIndex targetIndexMembers, TypeAccessor sourceAccessor, TypeAccessor targetAccessor) where T : new()
        {
            T targetItem = new T();
            foreach (var targetFieldName in targetIndexMembers.Keys)
            {
                try
                {
                    if (sourceIndexMembers.ContainsKey(targetFieldName))
                    {
                        var sourcePropertyType = sourceIndexMembers[targetFieldName].Type;
                        sourcePropertyType = ((sourcePropertyType.Name.Contains("Nullable")) ? sourcePropertyType.GenericTypeArguments.First() : sourcePropertyType);

                        var targetPropertyType = targetIndexMembers[targetFieldName].Type;
                        var isTargetNullable = targetPropertyType.Name.Contains("Nullable");
                        targetPropertyType = ((isTargetNullable) ? targetPropertyType.GenericTypeArguments.First() : targetPropertyType);
                        object sourceValue = sourceAccessor[item, targetFieldName];
                        var bForceSpecified = false;


                        if (sourceValue != null)
                        {
                            if ((targetPropertyType.Name.Equals("DateTime")) && (sourceValue.Equals(DateTime.Parse("1/1/0001 12:00:00 AM")))) sourceValue = null;
                            else if ((targetPropertyType.Name.Equals("DateTime")) && (sourceValue.Equals(DateTime.MaxValue)))
                            {
                                sourceValue = null;
                                bForceSpecified = true;
                            }
                            else if ((sourcePropertyType.IsEnum) && (targetPropertyType.Name.Contains("String")))
                                sourceValue = sourceValue?.ToString();
                            else if ((sourcePropertyType.Name.Contains("String")) && (targetPropertyType.IsEnum))
                                sourceValue = Enum.Parse(targetPropertyType, sourceValue?.ToString());
                        }

                        if ((sourceValue == null) || (sourceValue.Equals("DBNull")))
                        {
                            if ((targetPropertyType.Name.Equals("DateTime") && (!isTargetNullable)))
                            {
                                targetAccessor[targetItem, targetFieldName] = DateTime.Parse("1/1/0001 12:00:00 AM");
                            }
                            else
                            {
                                if (isTargetNullable) targetAccessor[targetItem, targetFieldName] = null;
                            }
                        }
                        else if ((targetPropertyType.FullName == sourcePropertyType.FullName) || (targetPropertyType.Name.Equals(sourceValue.GetType().Name)))
                            targetAccessor[targetItem, targetFieldName] = sourceValue;
                        else
                        {
                            if (targetPropertyType.GenericTypeArguments.Contains(targetPropertyType))
                            {
                                targetAccessor[targetItem, targetFieldName] = sourceValue;
                            }
                            else
                            {
                                var c = TypeDescriptor.GetConverter(targetPropertyType);
                                if (c.CanConvertTo(targetPropertyType))
                                {
                                    targetAccessor[targetItem, targetFieldName] = c.ConvertTo(sourceValue, targetPropertyType);
                                }
                                else
                                {
                                    try
                                    {
                                        targetAccessor[targetItem, targetFieldName] = System.Convert.ChangeType(sourceValue, targetPropertyType);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        throw new System.Exception(string.Format("Could not convert field {0} of type {1} to {2}", targetFieldName, sourceIndexMembers[targetFieldName].Type.Name, targetPropertyType.Name), ex);
                                    }
                                }

                            }
                        }
                        if (targetIndexMembers.ContainsKey(targetFieldName + "Specified"))
                        {
                            try
                            {
                                targetAccessor[targetItem, targetFieldName + "Specified"] = ((sourceValue != null) || bForceSpecified);
                            }
                            catch (Exception ex)
                            {
                                var message = string.Format("Unable to set {0}Specified as {1}.  {2}", targetFieldName + "Specified", targetPropertyType.Name, ex.Message);
                                throw new Exception(message, ex);
                            }

                        }
                    }
                }
                catch (Exception exField)
                {
                    var message = string.Format("Unable to set {0}.  {1}", targetFieldName, exField.Message);
                    throw new Exception(message, exField);
                }

            }
            return targetItem;
        }
        internal static TypeMemberIndex Members<T>(this T item)
        {
            return item.Members(new List<string>().ToArray());
        }

    /// <summary>
    /// Returns 
    /// </summary>
    /// <param name="item">Item to generate hash on</param>
    /// <param name="filters">A string array that lets you specify wild cards or '-' to remove blocks of fields.  Lets assume a class that contains properties KeyFieldA, KeyFieldB, DataFieldA, DataFieldB, DataFieldC
    ///   For Example:  ["-Key*"] - This says include all fields but ignore all fields that begin with Key - The Hash will be generated from - DataFieldA, DataFieldB, DataFieldC</param>
    ///                 ["Key*", "DataFieldA"] - Include all fields that begin with Key and the field DataFieldA - The Hash will be generated from - KeyFieldA, KeyFieldB, DataFieldA</param>
    ///                 ["*Field*", "-Data*"] - Include all fields that have the text 'Field' in the middle but ignoring all fields that begin with Data - The Hash will be generated from - KeyFieldA, KeyFieldB</param>
    /// <typeparam name="T">List of members filtered by filter</typeparam>
    /// <returns></returns>
        internal static TypeMemberIndex Members<T>(this T item, string[] filters)
        {
            var targetIndexMembers = new TypeMemberIndex();
            if (!IndexMembers.ContainsKey(typeof(T).Name)) {
                var targetAccessor = TypeAccessor.Create(typeof(T));
                if (!IndexMembers.ContainsKey(typeof(T).Name))
                {
                    var members = targetAccessor.GetMembers();
                    foreach (var _item in members) targetIndexMembers.Add(_item.Name, _item);
                    IndexMembers.Add(typeof(T).Name, targetIndexMembers);
                }
            }
            targetIndexMembers = IndexMembers[typeof(T).Name];

            if (filters.Length>0) {
                var targetIndexMembersFiltered = new TypeMemberIndex();
                /* If we start with an not operator, then we will assume we want every column */
                var allColumnOperator = filters.First().StartsWith("-");
                var keepField = allColumnOperator;

                foreach (var _targetFieldName in targetIndexMembers.Keys) {
                    var targetFieldName = _targetFieldName;
                    if (allColumnOperator) {
                        keepField = (targetFieldName.IsIn(filters));
                    } else {
                        if (targetFieldName.IsIn(filters)) keepField=true;
                    }
                    if (keepField) targetIndexMembersFiltered.Add(targetFieldName, targetIndexMembers[targetFieldName]);
                }
                targetIndexMembers = targetIndexMembersFiltered;
            }

            return targetIndexMembers;
        }

        internal static T As<S, T>(this S item) where T : new()
        {
            return item.As<S, T>(typeof(S).Members(), typeof(T).Members(), TypeAccessor.Create(typeof(S)), TypeAccessor.Create(typeof(T)));
        }

        /// <summary>
        /// Will Convert a list of objects to another list, but has the special trait of searching for a Specified field which is required for web service calls and will automatically set the target object to true
        /// so the client does not have to worry about a setting it
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList"></param>
        /// <returns></returns>
        internal static T[] AsArray<S, T>(this S[] sourceList) where T : new()
            /// <summary>
            /// Will Convert a list of objects to another list, but has the special trait of searching for a Specified field which is required for web service calls and will automatically set the target object to true
            /// so the client does not have to worry abnout a setting it
            /// </summary>
            /// <typeparam name="S"></typeparam>
            /// <typeparam name="T"></typeparam>
            /// <param name="sourceList"></param>
            /// <returns></returns>
        {
            return sourceList.ToList().AsArray<S, T>();
        }

        internal static T[] AsArray<S, T>(this List<S> sourceList) where T : new()
        {
            var returnList = new List<T>();
            foreach (var item in sourceList)
                returnList.Add(item.As<S, T>(typeof(S).Members(), typeof(T).Members(), TypeAccessor.Create(typeof(S)), TypeAccessor.Create(typeof(T))));
            return returnList.ToArray();
        }
    }
}
