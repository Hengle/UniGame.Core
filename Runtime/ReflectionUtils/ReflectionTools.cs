﻿namespace UniModules.UniCore.Runtime.ReflectionUtils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using DataStructure;
    using UnityEngine;
    using Utils;
    using Object = UnityEngine.Object;

    public static class ReflectionTools
    {
        private static Type _stringType = typeof(string);

        public static MemorizeItem<Type, IReadOnlyList<FieldInfo>> InstanceFields = MemorizeTool.
            Memorize<Type, IReadOnlyList<FieldInfo>>(x => {
                var fields = new List<FieldInfo>();
                if(x == null) return fields;
                fields.AddRange(x.GetFields(bindingFlags));
                return fields;
            });

        
        private static MemorizeItem<Type, List<Type>> assignableTypesCache = MemorizeTool.Memorize<Type, List<Type>>(x => {
            var items = x.GetAssignableTypesNonCached().
                ToList();
            return items;
        });
        
        private static MemorizeItem<(Type source,Type attribute), List<Type>> assignableAttributesTypesCache = 
            MemorizeTool.Memorize<(Type source,Type attribute), List<Type>>(x => 
            {
                var items = x.source.GetAssignableWithAttributeNonCached(x.attribute).
                    ToList();
                return items;
            });
        
        public const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public static DoubleKeyDictionary<Type,string,FieldInfo> fieldInfos = new DoubleKeyDictionary<Type,string,FieldInfo>();

        public static void Clear()
        {
            fieldInfos.Clear();
        }

        public static IReadOnlyList<FieldInfo> GetInstanceFields(this Type type)
        {
            return InstanceFields.GetValue(type);
        }

        public static bool IsReallyAssignableFrom(this Type type, Type otherType)
        {
            if (type.IsAssignableFrom(otherType))
                return true;
            if (otherType.IsAssignableFrom(type))
                return true;

            try
            {
                var v    = Expression.Variable(otherType);
                var expr = Expression.Convert(v, type);
                return expr.Method != null && expr.Method.Name != "op_Implicit";
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
        
        public static FieldInfo GetFieldInfoCached(this object target,string name) => GetFieldInfoCached(target.GetType(),name);
        
        public static FieldInfo GetFieldInfoCached<T>(string name) => GetFieldInfoCached(typeof(T),name);
        
        public static FieldInfo GetFieldInfoCached(this Type type,string name)
        {
            var info = fieldInfos.Get(type, name);
            if (info != null) return info;
            info = type.GetField(name,bindingFlags);

            if (info == null) return null;
            
            fieldInfos.Add(type,name,info);
            return info;
        }
        
        public static void SearchInFieldsRecursively<T>(object target, Object parent, Action<Object, T> onFoundAction, HashSet<object> validatedObjects, Func<T, T> resourceAction = null)
        {
            if (target == null || !validatedObjects.Add(target)) return;

            var targetType = target.GetType();
            var fields = targetType.GetFields();
            foreach (var fieldInfo in fields)
            {

                SearchInObject<T>(target, parent, fieldInfo, onFoundAction, validatedObjects, resourceAction);

            }
        }

        private static void SearchInObject<T>(object target, Object parent, FieldInfo fieldInfo, Action<Object, T> onFoundAction, HashSet<object> validatedObjects, Func<T, T> resourceAction)
        {

            try
            {

                if (target == null) return;

                var searchType = typeof(T);
                var item = fieldInfo.GetValue(target);

                if (Validate(item, searchType) == false)
                    return;

                T resultItem;
                if (ProcessItem<T>(target, fieldInfo, item, out resultItem, resourceAction))
                {
                    if (onFoundAction != null) onFoundAction(parent, resultItem);
                    return;
                }

                var collection = item as ICollection;
                if (collection != null)
                {
                    validatedObjects.Add(collection);
                    SearchInCollection(target, parent, collection, onFoundAction, validatedObjects, resourceAction);
                    return;
                }

                var assetItem = item as Object;
                parent = assetItem == null ? parent : assetItem;

                SearchInFieldsRecursively(item, parent, onFoundAction, validatedObjects);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static void SearchInCollection<T>(object target, Object parent, ICollection collection, Action<Object, T> onFoundAction, HashSet<object> validatedObjects, Func<T, T> resourceAction)
        {

            if (collection.Count > 0)
            {
                var searchingType = typeof(T);
                foreach (var collectionItem in collection)
                {
                    if (collectionItem == null || Validate(collectionItem.GetType(), searchingType) == false)
                        continue;
                    T resultItem;

                    if (ProcessItem<T>(target, null, collectionItem, out resultItem, resourceAction))
                    {
                        if (onFoundAction != null) onFoundAction(parent, resultItem);
                        continue;
                    }

                    var assetItem = collectionItem as Object;
                    parent = assetItem == null ? parent : assetItem;
                    SearchInFieldsRecursively(collectionItem, parent, onFoundAction, validatedObjects);
                }
            }
        }

        private static bool ProcessItem<T>(object target, FieldInfo fieldInfo, object item, out T result, Func<T, T> resourceAction)
        {

            var resultItem = default(T);
            var searchingType = typeof(T);

            result = resultItem;

            if (item == null || searchingType.IsInstanceOfType(item) == false) return false;

            result = (T)item;
            if (resourceAction != null)
            {
                result = resourceAction(result);
                if (fieldInfo != null)
                    fieldInfo.SetValue(target, result);
            }

            return true;

        }

        public static List<Type> GetDerivedTypes(this Type aType) {
            
            var  appDomain = AppDomain.CurrentDomain;
            var result = new List<Type>();
            var assemblies = appDomain.GetAssemblies();
            
            for(var i = 0; i<assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var types = assembly.GetTypes();
                for (var j = 0; j < types.Length; j++) {
                    var type = types[j];
                    if (type.IsSubclassOf(aType))
                        result.Add(type);
                }
            }
            return result;
        }

        
        public static List<Type> GetAssignableWithAttribute<TAttribute>(this Type baseType)
            where TAttribute : Attribute
        {
            return baseType.GetAssignableWithAttribute(typeof(TAttribute));
        }
        
        public static List<Type> GetAssignableWithAttribute(this Type baseType, Type attribute) {
            return assignableAttributesTypesCache.GetValue((baseType, attribute));
        }
        
        public static List<Type> GetAssignableWithAttributeNonCached(this Type baseType, Type attribute)
        {
            var items = baseType.GetAssignableTypes().
                Where(node => node.HasAttribute(attribute)).
                ToList();
            return items;
        }
        
        /// <summary>
        /// Get all classes deriving from baseType via reflection
        /// </summary>
        public static List<Type> GetAssignableTypes(this Type baseType) {
            return assignableTypesCache.GetValue(baseType);
        }
        
        /// <summary>
        /// Get all classes deriving from baseType via reflection
        /// </summary>
        public static List<Type> GetAssignableTypesNonCached(this Type baseType)
        {
            var types      = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var asmTypes = assembly.GetTypes();
                    var items    = asmTypes.Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t));
                    types.AddRange(items);
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning(e);
                };
            }
            
            return types;
        }

        public static bool HasDefaultConstructor(this Type target)
        {
            var constructor = target.GetConstructor(Type.EmptyTypes);
            return constructor != null;
        }
        
        public static bool Validate(object item, Type searchType)
        {
            if (item == null)
                return false;

            if (searchType.IsInstanceOfType(item)) return true;

            var type = item.GetType();
            return Validate(type, searchType);
        }

        public static bool Validate(Type type, Type searchType)
        {
            if (type == null) return false;
            if (type.IsValueType)
                return false;
            if (type == _stringType && searchType != _stringType)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// utility method for returning the first matching custom attribute (or <c>null</c>) of the specified member.
        /// </summary>
        public static T GetCustomAttribute<T>(this Type type, bool inherit = true)
        {
            var array = type.GetCustomAttributes(typeof (T), inherit);
            return array.Length != 0 ? (T) array[0] : default (T);
        }
        
        /// <summary>
        /// is type has target attribute
        /// </summary>
        public static bool HasAttribute<T>(this Type type, bool inherit = true) where T : Attribute
        {
            return type.HasAttribute(typeof(T),inherit);
        }
        
        public static bool HasAttribute(this Type type,Type attribute, bool inherit = true) 
        {
            var array = type.GetCustomAttributes(attribute, inherit);
            return array.Length != 0 ;
        }
        
        public static void FindResources<TData>(List<Object> assets, Action<Object, TData> onFoundAction, HashSet<object> excludedItems = null, Func<TData, TData> resourceAction = null) where TData : class
        {
            GUI.changed = true;
            var cache = excludedItems == null ? new HashSet<object>() : excludedItems;
            try
            {
                foreach (var asset in assets)
                {
                    FindResource<TData>(asset, onFoundAction, cache, resourceAction);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asset">source asset</param>
        /// <param name="onFoundAction"></param>
        /// <param name="cache">exclude items map filter</param>
        /// <param name="assetAction">allow change searching field value</param>
        /// <returns></returns>
        public static void FindResource<T>(Object asset, Action<Object, T> onFoundAction, HashSet<object> cache = null,
                                                Func<T, T> assetAction = null)
        {
            GUI.changed = true;
            var resourceCache = cache == null ? new HashSet<object>() : cache;
            if (asset == null) return;
            try
            {
                var seachingType = typeof(T);
                if (seachingType.IsInstanceOfType(asset))
                {
                    if (onFoundAction != null)
                        onFoundAction(asset, (T)(object)asset);
                    return;
                }
                SearchInFieldsRecursively(asset, asset, onFoundAction, resourceCache, assetAction);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static List<Type> FindAllChildrenTypes<T>()
        {
            var types = Assembly.GetAssembly(typeof(T)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)));
            return types.ToList();
        }

        public static List<Type> FindAllImplementations(Type targetType)
        {
            var type = targetType;
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));
            return types.ToList();
        }

        
        
    }


}

