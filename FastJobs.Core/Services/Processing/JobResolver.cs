using System.Reflection;
using Newtonsoft.Json;


namespace FastJobs;


internal static class JobResolver
{
    internal static List<Type> ResolveParamentersTypes()
    {
      return new List<Type> {};   
    }

    internal static List<object> ResolveParameters()
    {
        return new List<object> {};
    }

    internal static IBackGroundJob ResolveFireAndForgetJob(Job  job)
    {
        string DeclaringTypeName = job.MethodDeclaringTypeName;
        string MethodName = job.MethodName;
        string ConcreteJobType = job.TypeName;
        string ParameterTypesJson = job.ParameterTypeNamesJson;
        string Arguments = job.ArgumentsJson;

        Type declaringType = Type.GetType(DeclaringTypeName);
        object instance = Activator.CreateInstance(declaringType);
        
        string[] typeNames = JsonConvert.DeserializeObject<string[]>(ParameterTypesJson);

        Type[] parameterTypes = typeNames
            .Select(Type.GetType)
            .ToArray();

        object[] arguments = JsonConvert.DeserializeObject<object[]>(Arguments);
        MethodInfo method = declaringType.GetMethod(MethodName, parameterTypes);

        //No Returning Of Results Yet
        return  new FireAndForgetJobs(() => method.Invoke(instance, arguments)  ); 

    }


}