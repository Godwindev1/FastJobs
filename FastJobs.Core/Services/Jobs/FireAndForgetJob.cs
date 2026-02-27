
using System.Linq.Expressions;
using System.Reflection;

namespace FastJobs;

//Idea Here is To Store The Type FireAndForgetJob, With Constructor Argument Expression<Action> 
//WHen dequeued An Ready to Run THe Internal method Call Expression is Reflected And Called. There Will be no Return Types, 
//And parameters Are Passed By Structures or Json  
public class FireAndForgetJobs : IBackGroundJob
{
    private Expression<Action> _expression;

    public FireAndForgetJobs(Expression<Action> expression)
    {
        _expression = expression; 
    }

    public async Task ExecuteAsync(CancellationToken token)
    {    
            MethodCallExpression? methodCall = (MethodCallExpression)_expression.Body;

            string FunctionName = methodCall.Method.Name;
            Type TypeName = methodCall.Method.DeclaringType;

            if(methodCall.Method.IsStatic)
            {
                Expression.Lambda(methodCall).Compile().DynamicInvoke();
                return ;
            }
            else
            {
                var Value = Activator.CreateInstance(TypeName);   
                methodCall.Method.Invoke(Value, null);
            }
        
    }

}