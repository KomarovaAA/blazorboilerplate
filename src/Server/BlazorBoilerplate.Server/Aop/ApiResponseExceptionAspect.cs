using System;
using System.Reflection;
using System.Threading.Tasks;
using AspectInjector.Broker;
using BlazorBoilerplate.Infrastructure.Server.Models;
using BlazorBoilerplate.Server.Factories;
using BlazorBoilerplate.Shared.Localizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace BlazorBoilerplate.Server.Aop;

[Aspect(Scope.PerInstance, Factory = typeof(AopServicesFactory))]
public class ApiResponseExceptionAspect
{
    private readonly MethodInfo _asyncHandler =
        typeof(ApiResponseExceptionAspect).GetMethod(nameof(WrapAsync), BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly ILogger<ApiResponseExceptionAspect> _logger;

    private readonly MethodInfo _syncHandler =
        typeof(ApiResponseExceptionAspect).GetMethod(nameof(WrapSync), BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IStringLocalizer<Global> L;

    public ApiResponseExceptionAspect(ILogger<ApiResponseExceptionAspect> logger, IStringLocalizer<Global> l)
    {
        _logger = logger;
        L = l;
    }

    [Advice(Kind.Around, Targets = Target.Public | Target.Method)]
    public object Handle(
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Arguments)] object[] args,
        [Argument(Source.Name)] string name,
        [Argument(Source.ReturnType)] Type retType
    )
    {
        if (typeof(Task<ApiResponse>).IsAssignableFrom(retType))
            return _asyncHandler.Invoke(this, new object[] { target, args, name });
        if (typeof(ApiResponse).IsAssignableFrom(retType))
            return _syncHandler.Invoke(this, new object[] { target, args, name });

        return target(args);
    }

    private ApiResponse GetApiResponseFor(string method, Exception ex)
    {
        var code = Status500InternalServerError;
        var msg = ex.GetBaseException().Message + "\n\n" + ex.GetBaseException().StackTrace;

        var isDomainException = ex is DomainException;
        var isUnauthorizedAccessException = ex is UnauthorizedAccessException;

        if (isDomainException)
        {
            code = Status400BadRequest;
            msg = ((DomainException)ex).Description;
        }

        if (isUnauthorizedAccessException)
        {
            code = Status401Unauthorized;
            msg = ex.Message;
        }

        _logger.LogError($"{method}: {msg}");

        return new ApiResponse(code, isDomainException || isUnauthorizedAccessException ? msg : L["Operation Failed"]);
    }

    private ApiResponse WrapSync(Func<object[], object> target, object[] args, string name)
    {
        try
        {
            return (ApiResponse)target(args);
        }
        catch (Exception ex)
        {
            return GetApiResponseFor(name, ex);
        }
    }

    private async Task<ApiResponse> WrapAsync(Func<object[], object> target, object[] args, string name)
    {
        try
        {
            return await (Task<ApiResponse>)target(args);
        }
        catch (Exception ex)
        {
            return GetApiResponseFor(name, ex);
        }
    }
}