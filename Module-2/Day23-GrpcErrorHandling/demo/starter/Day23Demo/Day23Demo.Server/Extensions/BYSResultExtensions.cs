using BYSResults;
using Grpc.Core;

namespace Day23Demo.Server.Extensions;

public static class BYSResultExtensions
{
    public static RpcException ToRpcException<T>(this Result<T> result)
    {
        var statusCode = result.FirstError?.Code switch
        {
            "NotFound" => StatusCode.NotFound,
            "Validation" => StatusCode.InvalidArgument,
            "Authorization" => StatusCode.PermissionDenied,
            _ => StatusCode.Internal
        };
        return new RpcException(new Status(statusCode,
            result.FirstError?.Message ?? "Unknown error"));
    }
}
