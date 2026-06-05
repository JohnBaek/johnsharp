using AspectCore.DynamicProxy;
using JohnIsDev.Core.Features.Extensions;
using Microsoft.Extensions.Logging;

namespace JohnIsDev.Core.Features.Aop;

/// <summary>
/// Remove
/// </summary>
public class RemoveLeadingNewLineAspect : AbstractInterceptorAttribute
{
    
     
    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger<RemoveLeadingNewLineAspect> _logger;

    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public RemoveLeadingNewLineAspect(ILogger<RemoveLeadingNewLineAspect> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Intercept
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async override Task Invoke(AspectContext context, AspectDelegate next)
    {
        try
        {
            // Get Parameters
            object[] parameters = context.Parameters;

            // Process all
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is string str && str.StartsWith("\n"))
                {
                    parameters[i] = str.TrimStart('\n');
                }
            }
        }
        catch (Exception e)
        {
            e.LogError(logger: _logger);
        }

        await next(context);
    }
}