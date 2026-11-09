using BYSResults;
using Grpc.Core;

namespace Day23Demo.Server.Extensions;

// This one extension IS the bridge between Module 1 and Module 2. The service
// keeps reasoning in Result<T>/Error (the Day-6 pattern); this single place
// translates a failed Result into the gRPC failure channel (RpcException +
// Status). Same error-handling philosophy, different transport. Centralizing the
// mapping means every RPC reports failures identically and the translation table
// lives in ONE place instead of being re-decided inside each method.
public static class BYSResultExtensions
{
    public static RpcException ToRpcException<T>(this Result<T> result)
    {
        // Map our domain Error.Code (a string WE control) onto the gRPC StatusCode
        // the client filters on with `catch (RpcException ex) when (ex.StatusCode == ...)`.
        // These code strings are the contract: the server's Error("NotFound", …) and
        // the client's StatusCode.NotFound handler have to agree on them.
        var statusCode = result.FirstError?.Code switch
        {
            "NotFound" => StatusCode.NotFound,
            "Validation" => StatusCode.InvalidArgument,
            "Authorization" => StatusCode.PermissionDenied,
            // Default arm: an unmapped/unknown code becomes Internal — the gRPC
            // "something broke, no detail" status. Never let an unrecognized failure
            // surface to the client as something more specific than it really is.
            _ => StatusCode.Internal
        };
        // Status.Detail carries the human-readable message across the wire (the client
        // reads ex.Status.Detail). The ?? guards the shouldn't-happen case of a failed
        // Result with no error attached, so Detail is never null.
        return new RpcException(new Status(statusCode,
            result.FirstError?.Message ?? "Unknown error"));
    }
}
