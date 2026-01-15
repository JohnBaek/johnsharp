using System.Collections;
using System.Reflection;
using Newtonsoft.Json;

namespace JohnIsDev.Core.Extensions;

/// <summary>
/// Object 확장
/// </summary>
public static class ObjectExtensions 
{
    /// <summary>
    /// 
    /// </summary>
    private static readonly Lazy<UltraMapper.Mapper> Mapper = new(() => new UltraMapper.Mapper());

    
    /// <summary>
    /// T 데이터를 T로 클로닝 한다.
    /// </summary>
    /// <param name="source">원본 데이터</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? ToClone<T>(this T? source) where T : class
    {
        // 원본데이터가 유효하지 않을경우 
        if (source == null) 
            return null;
        
        // 질렬화 한다.
        string serialized = JsonConvert.SerializeObject(source);
        
        // 역직렬화 해서 반환한다.
        return JsonConvert.DeserializeObject<T>(serialized);
    }
    
    /// <summary>
    /// T 데이터를 T로 클로닝 한다.
    /// </summary>
    /// <param name="source">원본 데이터</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T ToCloneToNoneNullable<T>(this T? source) where T : class, new()
    {
        T? cloned = ToClone(source);
        if (cloned == null)
            return new T();
        
        return cloned;
    }
    
    
    /// <summary>
    /// Copies property values from the source object to a new instance of type T.
    /// </summary>
    /// <param name="source">The source object containing the data to copy.</param>
    /// <typeparam name="T">The type of the object to create and populate.</typeparam>
    /// <returns>A new instance of type T with properties populated from the source object.</returns>
    /// <exception cref="Exception">Thrown if an error occurs during property copying or object creation.</exception>
    public static T FromCopyValue<T>(this object source) where T : class
    {
        T? destination = Activator.CreateInstance<T>();
        try
        {
            // 소스 데이터로부터 프로퍼티를 가져온다.
            PropertyInfo[] sourceProperties = source.GetType().GetProperties();

            // 목적지 데이터로부터 프로퍼티를 가져온다.
            PropertyInfo[] destinationProperties = destination.GetType().GetProperties();
    
            // 모든 소스데이터에 대해 처리한다.
            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                // 모든 목적지 데이터에 대해 처리한다.
                foreach (PropertyInfo destinationProperty in destinationProperties)
                {
                    // Is Name and Type is Not identical
                    if (sourceProperty.Name != destinationProperty.Name || sourceProperty.PropertyType != destinationProperty.PropertyType) 
                        // Next 
                        continue;
                
                    // Update value. sourceProperty to destination
                    destinationProperty.SetValue(destination, sourceProperty.GetValue(source));
                    break;
                }
            }
        
            // 수정된 destination을 반환한다.
            return destination; 
        }
        catch (Exception) 
        {
            throw;
        }
    }
    
    
    /// <summary>
    /// Copies property values from the source object to a new instance of type T with deep copy support.
    /// </summary>
    /// <param name="source">The source object containing the data to copy.</param>
    /// <typeparam name="T">The type of the object to create and populate.</typeparam>
    /// <returns>A new instance of type T with properties populated from the source object.</returns>
    public static T FromCopyValueDeep<T>(this object source) where T : class
    {
        T destination = Activator.CreateInstance<T>();
        CopyProperties(source, destination);
        return destination;
    }

    /// <summary>
    /// Copies the properties from the source object to the destination object.
    /// </summary>
    /// <param name="source">The source object to copy properties from.</param>
    /// <param name="destination">The destination object to copy properties to.</param>
    private static void CopyProperties(object source, object destination)
    {
        PropertyInfo[] sourceProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] destinationProperties = destination.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo sourceProperty in sourceProperties)
        {
            if (!sourceProperty.CanRead) continue;

            PropertyInfo? destinationProperty = destinationProperties
                .FirstOrDefault(p => p.Name == sourceProperty.Name && p.CanWrite);

            if (destinationProperty == null) 
                continue;

            object? sourceValue = sourceProperty.GetValue(source);
            if (sourceValue == null)
            {
                destinationProperty.SetValue(destination, null);
                continue;
            }

            // Handle different types of properties
            if (IsSimpleType(sourceProperty.PropertyType))
            {
                // Simple types (primitives, string, DateTime, etc.)
                if (destinationProperty.PropertyType == sourceProperty.PropertyType)
                {
                    destinationProperty.SetValue(destination, sourceValue);
                }
            }
            else if (IsEnumerable(sourceProperty.PropertyType))
            {
                // Handle collections and arrays
                HandleEnumerableProperty(sourceValue, destinationProperty, destination);
            }
            else
            {
                // Complex objects - recursive copy
                HandleComplexProperty(sourceValue, destinationProperty, destination);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified type is a simple type.
    /// Simple types include primitives, enums, string, DateTime, DateTimeOffset, TimeSpan, Guid, decimal,
    /// and nullable versions of these types.
    /// </summary>
    /// <param name="type">The type to be evaluated.</param>
    /// <returns>True if the type is a simple type; otherwise, false.</returns>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum || 
               type == typeof(string) || 
               type == typeof(DateTime) || 
               type == typeof(DateTimeOffset) || 
               type == typeof(TimeSpan) || 
               type == typeof(Guid) || 
               type == typeof(decimal) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                IsSimpleType(Nullable.GetUnderlyingType(type) ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Determines whether the specified type is enumerable, excluding strings.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type implements IEnumerable and is not a string; otherwise, false.</returns>
    private static bool IsEnumerable(Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Processes enumerable property values from the source object and assigns them to the specified destination property.
    /// </summary>
    /// <param name="sourceValue">The enumerable value from the source object.</param>
    /// <param name="destinationProperty">The property information of the destination where the enumerable value should be assigned.</param>
    /// <param name="destination">The object containing the destination property to be updated.</param>
    private static void HandleEnumerableProperty(object sourceValue, PropertyInfo destinationProperty,
        object destination)
    {
        if (sourceValue is IEnumerable sourceEnumerable)
        {
            Type destinationType = destinationProperty.PropertyType;
            
            // Handle arrays
            if (destinationType.IsArray)
            {
                Type? elementType = destinationType.GetElementType();
                if (elementType == null)
                    return;
                
                var sourceList = sourceEnumerable.Cast<object>().ToList();
                Array destinationArray = Array.CreateInstance(elementType, sourceList.Count);
                
                for (int i = 0; i < sourceList.Count; i++)
                {
                    object copiedElement = CopyElement(sourceList[i], elementType);
                    destinationArray.SetValue(copiedElement, i);
                }
                
                destinationProperty.SetValue(destination, destinationArray);
            }
            // Handle generic collections (List<T>, IEnumerable<T>, etc.)
            else if (destinationType.IsGenericType)
            {
                Type[] genericArgs = destinationType.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    Type elementType = genericArgs[0];
                    Type listType = typeof(List<>).MakeGenericType(elementType);
                    var destinationList = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");
                    
                    foreach (object item in sourceEnumerable)
                    {
                        object copiedElement = CopyElement(item, elementType);
                        addMethod?.Invoke(destinationList, [copiedElement]);
                    }
                    
                    destinationProperty.SetValue(destination, destinationList);
                }
            }
        }
    }

    /// <summary>
    /// Handles the copying of complex property values from the source object to a destination object,
    /// ensuring accurate property mapping for complex types.
    /// </summary>
    /// <param name="sourceValue">The value of the property from the source object to be copied.</param>
    /// <param name="destinationProperty">The property metadata on the destination object target.</param>
    /// <param name="destination">The object to which the complex property value will be assigned.</param>
    private static void HandleComplexProperty(object sourceValue, PropertyInfo destinationProperty, object destination)
    {
        try
        {
            object destinationValue = Activator.CreateInstance(destinationProperty.PropertyType) ?? throw new InvalidOperationException();
            CopyProperties(sourceValue, destinationValue);
            destinationProperty.SetValue(destination, destinationValue);
        }
        catch
        {
            // If we can't create the destination type, skip this property
        }
    }

    /// <summary>
    /// 복잡한 객체의 속성을 복사하거나 단순 타입 데이터를 반환합니다.
    /// </summary>
    /// <param name="sourceElement">복사할 원본 요소</param>
    /// <param name="targetType">복사된 데이터의 대상 타입</param>
    /// <returns>복사된 데이터 객체 또는 단순 타입 데이터</returns>
    private static object CopyElement(object sourceElement, Type targetType)
    {
        // if (sourceElement == null)
        //     return null;

        if (IsSimpleType(targetType))
        {
            return sourceElement;
        }
        else
        {
            object destinationElement = Activator.CreateInstance(targetType) ?? throw new InvalidOperationException();
            CopyProperties(sourceElement, destinationElement);
            return destinationElement;
        }
    }

    

    // /// <summary>
    // /// Copies property values from the source object to a new instance of type T.
    // /// </summary>
    // /// <param name="source">The source object containing the data to copy.</param>
    // /// <typeparam name="T">The type of the object to create and populate.</typeparam>
    // /// <returns>A new instance of type T with properties populated from the source object.</returns>
    // /// <exception cref="Exception">Thrown if an error occurs during property copying or object creation.</exception>
    // public static T FromCopyValue<T>(this object source) where T : class
    // {
    //     T? destination = Activator.CreateInstance<T>();
    //     try
    //     {
    //         // 소스 데이터로부터 프로퍼티를 가져온다.
    //         PropertyInfo[] sourceProperties = source.GetType().GetProperties();
    //
    //         // 목적지 데이터로부터 프로퍼티를 가져온다.
    //         PropertyInfo[] destinationProperties = destination.GetType().GetProperties();
    //
    //         // 모든 소스데이터에 대해 처리한다.
    //         foreach (PropertyInfo sourceProperty in sourceProperties)
    //         {
    //             // 모든 목적지 데이터에 대해 처리한다.
    //             foreach (PropertyInfo destinationProperty in destinationProperties)
    //             {
    //                 // Is Name and Type is Not identical
    //                 if (sourceProperty.Name != destinationProperty.Name || sourceProperty.PropertyType != destinationProperty.PropertyType) 
    //                     // Next 
    //                     continue;
    //             
    //                 // Update value. sourceProperty to destination
    //                 destinationProperty.SetValue(destination, sourceProperty.GetValue(source));
    //                 break;
    //             }
    //         }
    //     
    //         // 수정된 destination을 반환한다.
    //         return destination; 
    //     }
    //     catch (Exception) 
    //     {
    //         throw;
    //     }
    // }

    /// <summary>
    /// Creates a deep copy of the source object to a new instance of type T using safe mapping.
    /// </summary>
    /// <param name="source">The source object containing the data to copy.</param>
    /// <typeparam name="T">The target type to create and map data to.</typeparam>
    /// <returns>A new instance of type T with properties deeply copied from the source object.</returns>
    public static T FromCopyValueDeepSafe<T>(this object source) where T : class, new()
        => Mapper.Value.Map<T>(source);


    /// <summary>
    /// 모든 타입 완벽 지원하는 Universal Deep Copy (생성자 무관)
    /// </summary>
    public static T FromCopyValueUniversal<T>(this object source)
    {
        if (source == null) return default!;

        try
        {
            string json = JsonConvert.SerializeObject(source, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore // 순환 참조 방지
            });
            return JsonConvert.DeserializeObject<T>(json)!;
        }
        catch
        {
            return default!;
        }
    }

}