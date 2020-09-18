using FastMember;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace desee.EntityObjects.Extentions
{

    internal static class GenericExtensions
    {

        static Dictionary<string, Dictionary<string, Member>> IndexMembers = new Dictionary<string, Dictionary<string, Member>>();
        public static bool In<T>(this T item, params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            return items.Contains(item);
        }
        internal static T As<S, T>(this S item, Dictionary<string, Member> sourceIndexMembers, Dictionary<string, Member> targetIndexMembers, TypeAccessor sourceAccessor, TypeAccessor targetAccessor) where T : new()
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
                                        if (targetPropertyType.Name.Equals("Int64"))
                                            targetAccessor[targetItem, targetFieldName] = long.Parse(sourceValue.ToString());
                                        if ((targetPropertyType.Name.Contains("Decimal")) || (targetIndexMembers[targetFieldName].Type.GenericTypeArguments.First().Name.Contains("Decimal")))
                                            targetAccessor[targetItem, targetFieldName] = sourceValue.ToDecimal();
                                        else
                                            targetAccessor[targetItem, targetFieldName] = System.Convert.ChangeType(sourceValue, targetPropertyType);
                                    }
                                    catch (System.Exception ex)
                                    {
                                        throw new System.Exception(string.Format("Could not conver field {0} of type {1} to {2}", targetFieldName, sourceIndexMembers[targetFieldName].Type.Name, targetPropertyType.Name), ex);
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

        internal static Dictionary<string, Member> Members<T>(this T item)
        {
            if (IndexMembers.ContainsKey(typeof(T).Name)) return IndexMembers[typeof(T).Name];
            var targetAccessor = TypeAccessor.Create(typeof(T));
            var targetIndexMembers = new Dictionary<string, Member>();
            if (!IndexMembers.ContainsKey(typeof(T).Name))
            {
                var members = targetAccessor.GetMembers();
                foreach (var _item in members) targetIndexMembers.Add(_item.Name, _item);
                IndexMembers.Add(typeof(T).Name, targetIndexMembers);
            }
            targetIndexMembers = IndexMembers[typeof(T).Name];
            return targetIndexMembers;
        }

        internal static Dictionary<string, Member> Members<T>(this Type type)
        {
            if (IndexMembers.ContainsKey(type.Name)) return IndexMembers[typeof(T).Name];
            var targetAccessor = TypeAccessor.Create(type);
            var targetIndexMembers = new Dictionary<string, Member>();
            if (!IndexMembers.ContainsKey(type.Name))
            {
                var members = targetAccessor.GetMembers();
                foreach (var _item in members) targetIndexMembers.Add(_item.Name, _item);
                IndexMembers.Add(type.Name, targetIndexMembers);
            }
            targetIndexMembers = IndexMembers[type.Name];
            return targetIndexMembers;
        }

        internal static T As<S, T>(this S item) where T : new()
        {
            return item.As<S, T>(typeof(S).Members(), typeof(T).Members(), TypeAccessor.Create(typeof(S)), TypeAccessor.Create(typeof(T)));
        }

        /// <summary>
        /// Will Convert a list of objects to another list, but has the special trait of searching for a Specified field which is reuqired for web service calls and will automatically set the target object to true
        /// so the client does not have to worry abnout a setting it
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList"></param>
        /// <returns></returns>
        internal static T[] AsArray<S, T>(this S[] sourceList) where T : new()
            /// <summary>
            /// Will Convert a list of objects to another list, but has the special trait of searching for a Specified field which is reuqired for web service calls and will automatically set the target object to true
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
