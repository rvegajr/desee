using FastMember;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;


namespace desee.entity
{
    public static class DatabaseExtensions
    {
        static Regex underscore = new Regex(@"(^|_)(.)");
        static string convertName(string s)
        {
            return underscore.Replace(s.ToLower(), m => m.Groups[0].ToString().ToUpper().Replace("_", ""));
        }

        static Dictionary<string, Dictionary<string, Member>> IndexMembers = new Dictionary<string, Dictionary<string, Member>>();
        static T ToObject<T>(this IDataRecord r) where T : new()
        {
            var indexMembers = new Dictionary<string, Member>();
            T obj = new T();
            var accessor = TypeAccessor.Create(typeof(T));
            var members = accessor.GetMembers();
            if (!IndexMembers.ContainsKey(typeof(T).Name))
            {
                foreach (var item in members) indexMembers.Add(item.Name, item);
                IndexMembers.Add(typeof(T).Name, indexMembers);
            }
            indexMembers = IndexMembers[typeof(T).Name];

            for (int i = 0; i < r.FieldCount; i++)
            {
                var name = r.GetName(i);
                if (r.GetName(i).Equals("RSRC_ID"))
                {
                    var k = 10;
                }
                if (indexMembers.ContainsKey(name))
                {
                    var PropertyType = indexMembers[name].Type;
                    if (r[i].GetType().Name.Equals("DBNull"))
                    {
                        accessor[obj, name] = null;
                    }
                    else if (PropertyType == r[i].GetType())
                    {
                        accessor[obj, name] = r[i];
                    }
                    else
                    {
                        if (PropertyType.GenericTypeArguments.Contains(r[i].GetType()))
                        {
                            accessor[obj, name] = r[i];
                        }
                        else
                        {
                            var c = TypeDescriptor.GetConverter(r[i]);
                            if (PropertyType.IsGenericType) PropertyType = PropertyType.GenericTypeArguments[0];
                            if (c.CanConvertTo(PropertyType))
                            {
                                accessor[obj, name] = c.ConvertTo(r[i], PropertyType);
                            }
                            else
                            {
                                try
                                {
                                    if (r[i].GetType().Name.Equals("Int64"))
                                        accessor[obj, name] = System.Int64.Parse(r[i].ToString());
                                    if ((indexMembers[name].Type.Name.Contains("Decimal")) || (indexMembers[name].Type.GenericTypeArguments.First().Name.Contains("Decimal")))
                                        accessor[obj, name] = r[i].ToDecimal();
                                    else
                                        accessor[obj, name] = System.Convert.ChangeType(r[i], indexMembers[name].Type);
                                }
                                catch (System.Exception ex)
                                {
                                    throw new System.Exception(string.Format("Could not conver field {0} of type {1} to {2}", name, r[i].GetType().Name, indexMembers[name].Type.Name), ex);
                                }
                            }

                        }
                    }

                }
            }
            return obj;
        }

        static string DRToJson(this IDataRecord r)
        {
            JObject returnJson = new JObject();
            for (int i = 0; i < r.FieldCount; i++)
            {
                var name = r.GetName(i);
                var c = TypeDescriptor.GetConverter(r[i]);
                if (c.CanConvertTo(r[i].GetType()))
                    returnJson.Add(new JProperty(name, r[i]));
            }
            return returnJson.ToString();
        }


        public static string ToJson(this IDataReader r)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            var rsCount = 0;
            do
            {
                var recCount = 0;
                sb.Append(((rsCount > 0) ? ",[" : "["));
                while (r.Read())
                {
                    if (recCount > 0) sb.Append(",");
                    sb.Append(r.DRToJson());
                    recCount++;
                }
                sb.Append("]");
                rsCount++;
            } while (r.NextResult());
            sb.Append("]");
            return sb.ToString();
        }

        public static IEnumerable<T> GetObjects<T>(this IDbCommand c) where T : new()
        {
            using (IDataReader r = c.ExecuteReader())
            {
                while (r.Read())
                {
                    yield return r.ToObject<T>();
                }
            }
        }

        public static IEnumerable<T> GetObjects<T>(this IDataReader r) where T : new()
        {
            while (r.Read())
            {
                yield return r.ToObject<T>();
            }
        }

        public static IEnumerable<T> GetObjectsNextRS<T>(this IDataReader r) where T : new()
        {
            if (r.NextResult())
            {
                while (r.Read())
                {
                    yield return r.ToObject<T>();
                }
            }
        }
        public static IEnumerable<T> GetObjects<T>(this DataTable dt) where T : new()
        {
            foreach (DataRow r in dt.Rows)
            {
                yield return r.ToObject<T>();
            }
        }
        static T ToObject<T>(this DataRow dr) where T : new()
        {
            var indexMembers = new Dictionary<string, Member>();
            T obj = new T();
            var accessor = TypeAccessor.Create(typeof(T));
            var members = accessor.GetMembers();
            foreach (var item in members) indexMembers.Add(item.Name, item);
            foreach (DataColumn c in dr.Table.Columns)
            {
                var name = c.ColumnName;
                if (name.Equals("MOIC"))
                { //TEST HERE :)
                    var k = 10;
                }
                if (indexMembers.ContainsKey(name))
                {
                    var PropertyType = indexMembers[name].Type;
                    if (dr[name].GetType().Name.Equals("DBNull"))
                    {
                        accessor[obj, name] = null;
                    }
                    else if (PropertyType == dr[name].GetType())
                        accessor[obj, name] = dr[name];
                    else
                    {
                        if (PropertyType.GenericTypeArguments.Contains(dr[name].GetType()))
                        {
                            accessor[obj, name] = dr[name];
                        }
                        else
                        {
                            var conv = TypeDescriptor.GetConverter(dr[name]);
                            if (conv.CanConvertTo(PropertyType))
                                accessor[obj, name] = conv.ConvertTo(dr[name], PropertyType);
                        }
                    }
                }
            }
            return obj;
        }
    }

    public class QueryResult
    {
        public int Inserts { get; set; } = 0;
        public int Updates { get; set; } = 0;
        public int Deletes { get; set; } = 0;

    }

    public static class ObjectExtentions
    {

        //Thanks https://stackoverflow.com/questions/6007159/cast-a-double-variable-to-decimal
        public static decimal ToDecimal(this double @double) =>
            @double > Convert.ToDouble(decimal.MaxValue) ? decimal.MaxValue : Convert.ToDecimal(@double);
        public static decimal ToDecimal(this object @double) =>
            Convert.ToDouble(@double) > Convert.ToDouble(decimal.MaxValue) ? decimal.MaxValue : Convert.ToDecimal(@double);
        public static double ToDouble(this decimal @decimal) =>
            @decimal > Convert.ToDecimal(decimal.MaxValue) ? double.MaxValue : Convert.ToDouble(@decimal);
        public static double ToDouble(this object @decimal) =>
            Convert.ToDecimal(@decimal) > Convert.ToDecimal(decimal.MaxValue) ? double.MaxValue : Convert.ToDouble(@decimal);
    }
}

